
# NopCommerce - LocaleStringResource

.NET Core Console app that helps you insert new localization strings in NopCommerce DB.

Existing localization is saved in XML file and is used as base file. Each value in this XML has last modified attribute. This is used in SQL file where new strings are appended at the end of SQL file.\
New localization strings are inserted in simple txt file as Key=Value. App reads XML and new localizations. Then it creates updated XML file with new localizations included and updated SQL file.

[![Build status](https://ci.appveyor.com/api/projects/status/50uti03vlhr0gar9?svg=true)](https://ci.appveyor.com/project/jaka-logar/nopcommerce-localestringresource)
[![contributions welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg?style=flat)](https://github.com/jaka-logar/NopCommerce.LocaleStringResource/issues)

# Usage
First open file UpdateLocalization.template.bat and edit at least "path to app".\
Other parameters are:\
-- l -> language identifier\
-- createXml -> option if XML file should be created\
-- createSql -> option if SQL file should be created\
-- xmlFile -> path to XML file\
-- sqlFile -> path to SQL file\
-- inputFile -> path to txt file of new localization

Then open file NewLocalization.template.txt and append new localization strings as Key=Value.\
When you execute UpdateLocalization.template.bat it first reads input XML file. Then it appends new localization from txt file in XML file and creates SQL file.
