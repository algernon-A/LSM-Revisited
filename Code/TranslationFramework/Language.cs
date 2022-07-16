using System.Collections.Generic;


namespace LoadingScreenModRevisited
{
    /// <summary>
    /// Translation language class.
    /// </summary>
    public class Language
    {
        // Translation file keywords - readable name.
        public static readonly string NameKey = "NAME";


        // Dictionary of translations for this language.
        public Dictionary<string, string> translationDictionary = new Dictionary<string, string>();

        // Language unique name.
        public string uniqueName = null;

        // Language human-readable name.
        public string readableName = null;
    }
}