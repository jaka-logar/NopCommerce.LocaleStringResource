using System;
using System.Collections.Generic;
using System.Text;

namespace LocaleStringResourceUpdate.Models
{
    internal class LocaleResource
    {
        public string Key { get; set; }

        public string Value { get; set; }

        public DateTime LastModified { get; set; }

        public string LanguageId { get; set; }

        public string LanguageName { get; set; }
    }
}
