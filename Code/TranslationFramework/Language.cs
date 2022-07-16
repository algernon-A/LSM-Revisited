﻿using System.Collections.Generic;


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

        // Language code.
        public string code = null;

        // Language human-readable display name.
        public string readableName = null;
    }
}