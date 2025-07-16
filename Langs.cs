using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace CurrentDocumentBluePrint
{
    class Langs
    {
        // creates and returns a new CultureInfo based on iso639_3
        public static CultureInfo Iso639_3ToCulture(string iso639_3)
        {
            string cultureString = Iso639_3ToCultureString(iso639_3);
            return new CultureInfo(cultureString);
        }

        // converts an Iso639-3 language code to a string which can be used to initialize a CultureInfo
        public static string Iso639_3ToCultureString(string iso639_3)
        {
            string result = "";
            if (iso639_3 != null)
            {
                // try to find in our dictionary of known languages first
                if (!DictIso639_3ToCultureString.TryGetValue(iso639_3, out result))
                {
                    // try to look up in a generic way
                    result = Iso639_3ToCultureString_Default(iso639_3);
                }
            }
            return result;
        }


        #region Private members
        // find a culture string match the ISO 3 letter code
        // this might not work properly for DMSConnectors, only use it as a fallback
        private static string Iso639_3ToCultureString_Default(string iso639_3)
        {
            // try to find a matching netural culture first (eg. "en", "de")
            CultureInfo[] neutralCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
            CultureInfo neutralMatch = neutralCultures.FirstOrDefault(cultInfo =>
                        cultInfo.ThreeLetterISOLanguageName.Equals(iso639_3, StringComparison.InvariantCultureIgnoreCase));

            if (neutralMatch != null)
            {
                return neutralMatch.Name;
            }

            // try to find a specific culture if no match (eg. "en-US", "de-DE")
            CultureInfo[] speficiCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            CultureInfo specificMatch = speficiCultures.FirstOrDefault(cultInfo =>
                    cultInfo.ThreeLetterISOLanguageName.Equals(iso639_3, StringComparison.InvariantCultureIgnoreCase));

            if (specificMatch != null)
            {
                return specificMatch.Name;
            }
            else
            {
                return "";  // return empty string if no match
            }
        }

        // list of languages supported by DMSConnector interface
        private static Dictionary<string, string> DictIso639_3ToCultureString = new Dictionary<string, string>()
        {
           // ISO   Culture
            {"eng",  "en"}, // English
            {"por",  "pt"}, // Portuguese
            {"nld",  "nl"}, // Dutch
            {"fra",  "fr"}, // French
            {"deu",  "de"}, // German
            {"ita",  "it"}, // Italian
            {"spa",  "es"}, // Spanish
            {"swe",  "sv"}, // Swedish
            {"dan",  "da"}, // Danish
            {"qct",  "zh-TW"}, // qct is a reserved ISO code, DMSConnector use it for Chinse (Traditional)
            {"jpn",  "ja"}, // Japanese
            {"kor",  "ko"}, 
            {"zho",  "zh-CN"}, 	// zho is the Chinese ISO code, DMSConnector use it for Chinse (Simplified)
            {"nor",  "no"}, 	// Norwegian (Bokmal)
            {"rus",  "ru"}, 
            {"pol",  "pl"}, 
            {"tur",  "tr"}, 
            {"fin",  "fi"},
        };

        #endregion Private members
    }
}
