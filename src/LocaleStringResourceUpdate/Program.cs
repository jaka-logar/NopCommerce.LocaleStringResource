using CommandLine;
using LocaleStringResourceUpdate.Models;
using System;
using  System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LocaleStringResourceUpdate
{
    class Program
    {
        private const string XmlRoot = "Resources";
        private const string XmlLanguage = "Language";
        //private const string XmlLanguageName = "Name";
        private const string XmlLanguageId = "Id";
        private const string XmlLocaleResource = "LocaleResource";
        private const string XmlLocaleResourceName = "Name";
        private const string XmlLocaleResourceValue = "Value";
        private const string XmlLocaleResourceLastModified = "LastModified";

        private static string DateFormat = "dd.MM.yyyy";

        static void Main(string[] args)
        {
            //ArgumentsModel arguments = new ArgumentsModel();
            Parser.Default.ParseArguments<ArgumentsModel>(args)
                .WithParsed<ArgumentsModel>(opts => ParsedArgumentsRun(opts))
                .WithNotParsed<ArgumentsModel>((errs) => HandleParseError(errs));
        }

        private static void HandleParseError(IEnumerable<Error> errors)
        {
            Console.WriteLine("Errors:");
            foreach (var error in errors)
            {
                Console.WriteLine(error);
            }

            Console.ReadKey();
        }

        private static void ParsedArgumentsRun(ArgumentsModel arguments)
        {
            if ((arguments.LanguageId <= 0) || string.IsNullOrWhiteSpace(arguments.XmlFilePath))
            {
                Console.WriteLine("LanguageId and XML file path are required!");
                Console.ReadKey();
                return;
            }

            // Key: languageId; Value: { Key: resourceKey; Value: resourceValue }
            Dictionary<string, Dictionary<string, LocaleResource>> localeResources = ReadXmlFile(arguments.XmlFilePath);

            Dictionary<string, LocaleResource> localeResourcesTemp;
            localeResources.TryGetValue(arguments.LanguageIdString, out localeResourcesTemp);

            localeResourcesTemp = ReadLocalizationFile(arguments.LocalizationFilePath, localeResourcesTemp, arguments.LanguageIdString);

            if (localeResources.ContainsKey(arguments.LanguageIdString))
            {
                // Update existing
                localeResources[arguments.LanguageIdString] = localeResourcesTemp;
            }
            else
            {
                // Add new language
                localeResources.Add(arguments.LanguageIdString, localeResourcesTemp);
            }

            if (arguments.CreateXml)
            {
                // Write XML file
                WriteXmlFile(arguments.XmlFilePath, localeResources);
            }

            if (arguments.CreateSql)
            {
                // Write SQL file
                WriteSqlFile(arguments.SqlFilePath, localeResources);
            }
        }

        /// <summary>
        ///     Read existing XML file
        /// </summary>
        /// <param name="xmlFilePath">File path to XML</param>
        /// <returns></returns>
        private static Dictionary<string, Dictionary<string, LocaleResource>> ReadXmlFile(string xmlFilePath)
        {
            Dictionary<string, Dictionary<string, LocaleResource>> localeResourcesDictionary = new Dictionary<string, Dictionary<string, LocaleResource>>();

            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(xmlFilePath);

                if (xmlDocument.DocumentElement != null)
                {
                    foreach (XmlNode languageNode in xmlDocument.DocumentElement.ChildNodes)
                    {
                        string languageId = languageNode.Attributes?[XmlLanguageId]?.InnerText;
                        //string languageName = languageNode.Attributes?[XmlLanguageName]?.InnerText;

                        if (string.IsNullOrWhiteSpace(languageId))
                        {
                            // Something went wrong
                            continue;
                        }

                        if (!localeResourcesDictionary.ContainsKey(languageId))
                        {
                            localeResourcesDictionary.Add(languageId, new Dictionary<string, LocaleResource>());
                        }

                        foreach (XmlNode resourceNode in languageNode.ChildNodes)
                        {
                            string resourceKey = resourceNode.Attributes?[XmlLocaleResourceName]?.InnerText;
                            string resourceLastModified = resourceNode.Attributes?[XmlLocaleResourceLastModified]?.InnerText;

                            XmlNode valueXmlNode = resourceNode.SelectSingleNode(XmlLocaleResourceValue);
                            string resourceValue = WebUtility.HtmlDecode(valueXmlNode?.InnerXml);

                            DateTime lastModified;
                            if (string.IsNullOrWhiteSpace(resourceKey) || !DateTime.TryParse(resourceLastModified, out lastModified))
                            {
                                // Something went wrong
                                continue;
                            }

                            if (!localeResourcesDictionary[languageId].ContainsKey(resourceKey))
                            {
                                LocaleResource resource = new LocaleResource
                                {
                                    Key = resourceKey,
                                    Value = resourceValue,
                                    LastModified = lastModified,
                                    LanguageId = languageId
                                };

                                localeResourcesDictionary[languageId].Add(resourceKey, resource);
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
            }

            return localeResourcesDictionary;
        }

        /// <summary>
        ///     Read new locale resources
        /// </summary>
        /// <param name="localizationFilePath">File path to locale resources</param>
        /// <param name="localeResources">Local resources dictionary</param>
        /// <param name="languageId">Language identifier string</param>
        /// <returns></returns>
        private static Dictionary<string, LocaleResource> ReadLocalizationFile(string localizationFilePath,
            Dictionary<string, LocaleResource> localeResources, string languageId)
        {
            localeResources = localeResources ?? new Dictionary<string, LocaleResource>();

            // Read the file and display it line by line.
            using (StreamReader file = new StreamReader(localizationFilePath))
            {
                string line;
                string key = null;
                StringBuilder value = new StringBuilder();

                while ((line = file.ReadLine()) != null)
                {
                    int currentIndex = 0;
                    bool keyFound = false;
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    {
                        // Skip line
                        continue;
                    }

                    // Find key, value
                    while (!keyFound)
                    {
                        currentIndex = line.IndexOf("=", currentIndex, StringComparison.InvariantCulture);
                        if (currentIndex < 0)
                        {
                            // Not found -> multi line value
                            value.AppendLine(line);
                            break;
                        }

                        if (line.ElementAt(currentIndex - 1).Equals('\\'))
                        {
                            //  \= (Escaped =)
                            currentIndex++;
                        }
                        else
                        {
                            value.Clear();

                            // Key and value found
                            key = line.Substring(0, currentIndex);
                            value.AppendLine(line.Substring(++currentIndex));

                            keyFound = true;
                        }
                    }

                    if (!string.IsNullOrEmpty(key))
                    {
                        key = key.Replace("\\=", "=").Replace("'", "''").Trim();
                        string val = value.Replace("\\=", "=").Replace("'", "''").ToString().Trim();

                        if (!localeResources.ContainsKey(key))
                        {
                            LocaleResource localeResource = new LocaleResource
                            {
                                Key = key.Trim(),
                                Value = WebUtility.HtmlDecode(val),
                                LastModified = DateTime.Now.Date,
                                LanguageId = languageId
                            };

                            localeResources.Add(key, localeResource);
                        }
                        else
                        {
                            localeResources[key].Value = WebUtility.HtmlDecode(val);
                            localeResources[key].LastModified = DateTime.Now.Date;
                        }
                    }
                }

                file.Close();
            }

            return localeResources;
        }

        /// <summary>
        ///     Generate XML file
        /// </summary>
        /// <param name="xmlFilePath">XML file path</param>
        /// <param name="localeResources">Locale resources dictionary</param>
        private static void WriteXmlFile(string xmlFilePath, Dictionary<string, Dictionary<string, LocaleResource>> localeResources)
        {
            if ((localeResources == null) || (localeResources.Count == 0))
            {
                // Nothing to write
                return;
            }

            XDocument document = new XDocument();

            XElement rootElement = new XElement(XmlRoot);
            document.Add(rootElement);

            foreach (string key in localeResources.Keys)
            {
                // Languages
                XElement languageElement = new XElement(XmlLanguage);
                languageElement.SetAttributeValue(XmlLanguageId, key);
                rootElement.Add(languageElement);

                // Resources
                IList<LocaleResource> resources = localeResources[key].Values.OrderBy(x => x.Key).ToList();
                foreach (LocaleResource localeResource in resources)
                {
                    XElement resourceElement = new XElement(XmlLocaleResource);
                    resourceElement.SetAttributeValue(XmlLocaleResourceName, localeResource.Key);
                    resourceElement.SetAttributeValue(XmlLocaleResourceLastModified, localeResource.LastModified.ToString(DateFormat));
                    XElement resourceValueElement = new XElement(XmlLocaleResourceValue);
                    resourceValueElement.Add(localeResource.Value);

                    resourceElement.Add(resourceValueElement);
                    languageElement.Add(resourceElement);
                }
            }

            // Save to file
            using (XmlTextWriter file = new XmlTextWriter(xmlFilePath, Encoding.UTF8))
            {
                file.Formatting = Formatting.Indented;

                document.WriteTo(file);
            }


            //XmlDocument xmlDocument = new XmlDocument();
            //xmlDocument.CreateNode()   

            //using (XmlWriter xmlWriter = XmlWriter.Create(xmlFilePath))
            //{
            //        xmlWriter.WriteStartDocument();
            //    xmlWriter.WriteStartElement(XmlRoot);


            //    xmlWriter.WriteEndDocument();
            //    xmlWriter.Close();
            //}
        }

        /// <summary>
        ///     Write SQL file
        /// </summary>
        /// <param name="sqlFilePath">SQL file path</param>
        /// <param name="localeResources">Local resources dictionary</param>
        private static void WriteSqlFile(string sqlFilePath, Dictionary<string, Dictionary<string, LocaleResource>> localeResources)
        {
            if (localeResources == null || localeResources.Count == 0)
            {
                // Nothing to write
                return;
            }

            List<LocaleResource> localeResourcesList = new List<LocaleResource>();
            foreach (Dictionary<string, LocaleResource> localeResourcesValue in localeResources.Values)
            {
                localeResourcesList.AddRange(localeResourcesValue.Values);
            }

            List<IGrouping<DateTime, LocaleResource>> groups = localeResourcesList.GroupBy(x => x.LastModified).OrderBy(x => x.Key).ToList();

            using (StreamWriter writer = new StreamWriter(sqlFilePath))
            {
                foreach (IGrouping<DateTime, LocaleResource> group in groups)
                {
                    writer.WriteLine($"-- {group.Key.ToString(DateFormat)}");

                    List<LocaleResource> resourceList = group.OrderBy(x => x.Key).ToList();
                    foreach (LocaleResource localeResource in resourceList)
                    {
                        writer.WriteLine("INSERT INTO [dbo].[LocaleStringResource]([LanguageId],[ResourceName],[ResourceValue])");
                        writer.WriteLine($"VALUES({localeResource.LanguageId}, '{localeResource.Key}', '{localeResource.Value}')");
                        writer.WriteLine("GO");
                        writer.WriteLine("");
                    }
                }
            }
        }
    }
}
