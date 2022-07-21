using System;
using System.IO;
using System.Xml.Serialization;


namespace LoadingScreenModRevisited
{
    [XmlRoot("LoadingScreenModRevisited")]
    public class LSMRSettingsFile
    {
        // Settings file name.
        [XmlIgnore]
        private static readonly string SettingsFileName = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, "LoadingScreenModRevisited.xml");

        // 'Show duplicate warnings' setting.
        [XmlIgnore]
        internal static bool showDuplicates = false;


        /// <summary>
        /// Language setting.
        /// </summary>
        [XmlElement("Language")]
        public string XMLLanguage { get => Translations.CurrentLanguage; set => Translations.CurrentLanguage = value; }


        /// <summary>
        /// Loading image mode.
        /// </summary>
        [XmlElement("BackgroundImageMode")]
        public ImageMode XMLImageMode { get => BackgroundImage.ImageMode; set => BackgroundImage.ImageMode = value; }


        /// <summary>
        /// Local loading image directory.
        /// </summary>
        [XmlElement("LocalImageDir")]
        public string XMLLocalImageDir { get => BackgroundImage.imageDir; set => BackgroundImage.imageDir = value; }


        /// <summary>
        /// Hide duplicate item warnings.
        /// </summary>
        [XmlElement("ShowDuplicateWarnings")]
        public bool XMLShowDuplicates { get => showDuplicates; set => showDuplicates = value; }


        /// <summary>
        /// Load settings from XML file.
        /// </summary>
        internal static void Load()
        {
            try
            {
                // Check to see if configuration file exists.
                if (File.Exists(SettingsFileName))
                {
                    // Read it.
                    using (StreamReader reader = new StreamReader(SettingsFileName))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(LSMRSettingsFile));
                        if (!(xmlSerializer.Deserialize(reader) is LSMRSettingsFile settingsFile))
                        {
                            Logging.Error("couldn't deserialize settings file");
                        }
                    }
                }
                else
                {
                    Logging.Message("no settings file found");
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception reading XML settings file");
            }
        }


        /// <summary>
        /// Save settings to XML file.
        /// </summary>
        internal static void Save()
        {
            try
            {
                // Save into user local settings.
                using (StreamWriter writer = new StreamWriter(SettingsFileName))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(LSMRSettingsFile));
                    xmlSerializer.Serialize(writer, new LSMRSettingsFile());
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception saving XML settings file");
            }
        }
    }
}