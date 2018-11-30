using CommandLine;

namespace LocaleStringResourceUpdate.Models
{
    internal class ArgumentsModel
    {
        [Option('l', "languageId", Default = 0, Required = true, HelpText = "Language identifier")]
        public int LanguageId { get; set; }

        [Option('n', "languageName", Default = null, HelpText = "Language name")]
        public string LanguageName { get; set; }

        public string LanguageIdString => this.LanguageId.ToString();

        [Option("createXml", Default = false, HelpText = "If true update XML file")]
        public bool CreateXml { get; set; }

        [Option("createSql", Default = false, HelpText = "If true update SQL file")]
        public bool CreateSql { get; set; }

        [Option("inputFile", Default = null, HelpText = "Input localization file")]
        public string LocalizationFilePath { get; set; }

        [Option("sqlFile", Default = null, HelpText = "Input/output SQL file")]
        public string SqlFilePath { get; set; }

        [Option("xmlFile", Default = null, HelpText = "Input/output XML file")]
        public string XmlFilePath { get; set; }
    }
}
