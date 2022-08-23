using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using AlgernonCommons.Translation;
using ColossalFramework.IO;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace LoadingScreenMod
{
    public class Settings
    {
        private const string FILENAME = "LoadingScreenMod.xml";

        internal const string SUPPORTED = "1.14";

        private const int VERSION = 10;

        public int version = 10;

        public bool loadEnabled = true;

        public bool loadUsed = true;

        public bool shareTextures = true;

        public bool shareMaterials = true;

        public bool shareMeshes = true;

        public bool optimizeThumbs = true;

        public bool reportAssets;

        public bool checkAssets;

        public string reportDir = string.Empty;

        public bool skipPrefabs;

        public string skipFile = string.Empty;

        public bool hideAssets;

        public bool useReportDate = true;

        private DateTime skipFileTimestamp = DateTime.MinValue;

        internal bool enableDisable;

        internal bool removeVehicles;

        internal bool removeCitizenInstances;

        internal bool recover;

        private static Settings singleton;

        internal UIHelperBase helper;

        internal bool SkipPrefabs
        {
            get
            {
                if (skipPrefabs && SkipMatcher != null)
                {
                    return ExceptMatcher != null;
                }
                return false;
            }
        }


        internal static Matcher SkipMatcher { get; private set; }

        internal static Matcher ExceptMatcher { get; private set; }

        internal static string DefaultSavePath => Path.Combine(Path.Combine(DataLocation.localApplicationData, "Report"), "LoadingScreenMod");

        private static string DefaultSkipFile => Path.Combine(Path.Combine(DataLocation.localApplicationData, "SkippedPrefabs"), "skip.txt");

        internal static string HiddenAssetsFile => LoadingScreenModRevisited.LSMRSettings.HiddenAssetsFile;

        public static Settings settings
        {
            get
            {
                if (singleton == null)
                {
                    singleton = Load();
                }
                return singleton;
            }
        }

        private Settings()
        {
        }

        private static Settings Load()
        {
            Settings settings = null;
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Settings));
                using (StreamReader textReader = new StreamReader("LoadingScreenMod.xml"))
                {
                    settings = (Settings)xmlSerializer.Deserialize(textReader);
                }
            }
            catch (Exception)
            {
                return null;
            }
            if (string.IsNullOrEmpty(settings.reportDir = settings.reportDir?.Trim()))
            {
                settings.reportDir = DefaultSavePath;
            }
            if (string.IsNullOrEmpty(settings.skipFile = settings.skipFile?.Trim()))
            {
                settings.skipFile = DefaultSkipFile;
            }
            settings.checkAssets &= settings.reportAssets;
            settings.version = 10;
            return settings;
        }

        internal static DateTime LoadSkipFile()
        {
            try
            {
                if (LoadingScreenModRevisited.LSMRSettings.SkipPrefabs)
                {
                    bool flag = File.Exists(LoadingScreenModRevisited.LSMRSettings.SkipFile);
                    DateTime lastWriteTimeUtc;
                    if (flag && LoadingScreenModRevisited.LSMRSettings.SkipFileTimestamp != (lastWriteTimeUtc = File.GetLastWriteTimeUtc(LoadingScreenModRevisited.LSMRSettings.SkipFile)))
                    {
                        Matcher[] array = Matcher.Load(LoadingScreenModRevisited.LSMRSettings.SkipFile);
                        SkipMatcher = array[0];
                        ExceptMatcher = array[1];
                        LoadingScreenModRevisited.LSMRSettings.SkipFileTimestamp = lastWriteTimeUtc;
                    }
                    else if (!flag)
                    {
                        Util.DebugPrint("Skip file", LoadingScreenModRevisited.LSMRSettings.SkipFile, "does not exist");
                    }
                }
            }
            catch (Exception exception)
            {
                Util.DebugPrint("Settings.LoadSkipFile");
                UnityEngine.Debug.LogException(exception);
                LoadingScreenModRevisited.LSMRSettings.SkipFileTimestamp = DateTime.MinValue;
            }
            if (!LoadingScreenModRevisited.LSMRSettings.SkipPrefabs)
            {
                return DateTime.MinValue;
            }
            return LoadingScreenModRevisited.LSMRSettings.SkipFileTimestamp;
        }

        internal static void LoadHiddenAssets(HashSet<string> hidden)
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(HiddenAssetsFile))
                {
                    string text;
                    while ((text = streamReader.ReadLine()) != null)
                    {
                        string text2 = text.Trim();
                        if (!string.IsNullOrEmpty(text2) && !text2.StartsWith("#"))
                        {
                            hidden.Add(text2);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Util.DebugPrint("Cannot read from " + HiddenAssetsFile);
            }
        }

        internal static void SaveHiddenAssets(HashSet<string> hidden, string[] missing, string[] duplicates)
        {
            if (hidden.Count == 0 && missing.Length == 0 && duplicates.Length == 0)
            {
                CreateHiddenAssetsFile();
                return;
            }
            string hiddenAssetsFile = HiddenAssetsFile;
            Util.DebugPrint("Saving hidden assets to", hiddenAssetsFile);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(hiddenAssetsFile));
                using (StreamWriter streamWriter = new StreamWriter(hiddenAssetsFile))
                {
                    streamWriter.WriteLine(Translations.Translate("AS_YOU_KNOW"));
                    streamWriter.WriteLine(Translations.Translate("USING_THIS_FILE"));
                    if (hidden.Count > 0)
                    {
                        WriteLines(streamWriter, Translations.Translate("THESE_ARE_NOT_REPORTED"), hidden.ToArray(), tag: false);
                    }
                    string[] array = missing.Where((string s) => !hidden.Contains(s)).ToArray();
                    string[] array2 = duplicates.Where((string s) => !hidden.Contains(s)).ToArray();
                    if (array.Length != 0 || array2.Length != 0)
                    {
                        streamWriter.WriteLine();
                        streamWriter.WriteLine(Translations.Translate("LSM_REPORTED_THESE"));
                        WriteLines(streamWriter, Translations.Translate("REPORTED_MISSING"), array, tag: true);
                        WriteLines(streamWriter, Translations.Translate("REPORTED_DUPLICATE"), array2, tag: true);
                    }
                }
            }
            catch (Exception)
            {
                Util.DebugPrint("Cannot write to " + hiddenAssetsFile);
            }
        }

        private static void WriteLines(StreamWriter w, string header, string[] lines, bool tag)
        {
            if (lines.Length == 0)
            {
                return;
            }
            w.WriteLine();
            w.WriteLine(header);
            Array.Sort(lines);
            foreach (string text in lines)
            {
                if (tag)
                {
                    w.WriteLine("#" + text);
                }
                else
                {
                    w.WriteLine(text);
                }
            }
        }

        private static void CreateHiddenAssetsFile()
        {
            try
            {
                string hiddenAssetsFile = HiddenAssetsFile;
                Directory.CreateDirectory(Path.GetDirectoryName(hiddenAssetsFile));
                using (StreamWriter streamWriter = new StreamWriter(hiddenAssetsFile))
                {
                    streamWriter.WriteLine(Translations.Translate("GO_AHEAD"));
                }
            }
            catch (Exception)
            {
                Util.DebugPrint("Cannot write to " + HiddenAssetsFile);
            }
        }
    }
}
