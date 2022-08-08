namespace LoadingScreenModRevisited
{
    using System.IO;
    using System.Xml.Serialization;
    using AlgernonCommons.XML;

    [XmlRoot("LoadingScreenModRevisited")]
    public class LSMRSettings : SettingsXMLBase
    {
        /// <summary>
        /// Gets the settings file name.
        /// </summary>
        [XmlIgnore]
        private static readonly string SettingsFileName = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, "LoadingScreenModRevisited.xml");

        /// <summary>
        /// Gets or sets a value indicating whether duplicate asset warnings should be displayed on the loading screen scroll.
        /// </summary>
        [XmlIgnore]
        internal static bool ShowDuplicates { get; set; }  = false;

        /// <summary>
        /// Gets or sets the loading image mode.
        /// </summary>
        [XmlElement("BackgroundImageMode")]
        public ImageMode XMLImageMode { get => BackgroundImage.ImageMode; set => BackgroundImage.ImageMode = value; }


        /// <summary>
        /// Gets or sets the local loading image directory.
        /// </summary>
        [XmlElement("LocalImageDir")]
        public string XMLLocalImageDir { get => BackgroundImage.imageDir; set => BackgroundImage.imageDir = value; }


        /// <summary>
        /// Gets or sets a value indicating whether duplicate asset warnings should be displayed on the loading screen scroll.
        /// </summary>
        [XmlElement("ShowDuplicateWarnings")]
        public bool XMLShowDuplicates { get => ShowDuplicates; set => ShowDuplicates = value; }


        /// <summary>
        /// Loads settings from file.
        /// </summary>
        internal static void Load() => XMLFileUtils.Load<LSMRSettings>(SettingsFileName);

        /// <summary>
        /// Saves settings to file.
        /// </summary>
        internal static void Save() => XMLFileUtils.Save<LSMRSettings>(SettingsFileName);
    }
}