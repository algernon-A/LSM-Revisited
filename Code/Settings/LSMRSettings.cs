// <copyright file="LSMRSettings.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System.IO;
    using System.Xml.Serialization;
    using AlgernonCommons.XML;

    /// <summary>
    /// The Loading Screen Mod Revisited settings file.
    /// </summary>
    [XmlRoot("LoadingScreenModRevisited")]
    public class LSMRSettings : SettingsXMLBase
    {
        /// <summary>
        /// Gets the settings file name.
        /// </summary>
        [XmlIgnore]
        private static readonly string SettingsFileName = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, "LoadingScreenModRevisited.xml");

        /// <summary>
        /// Gets or sets the loading image mode.
        /// </summary>
        [XmlElement("BackgroundImageMode")]
        public BackgroundImage.ImageMode XMLImageMode { get => BackgroundImage.CurrentImageMode; set => BackgroundImage.CurrentImageMode = value; }

        /// <summary>
        /// Gets or sets the local loading image directory.
        /// </summary>
        [XmlElement("LocalImageDir")]
        public string XMLLocalImageDir { get => BackgroundImage.ImageDirectory; set => BackgroundImage.ImageDirectory = value; }

        /// <summary>
        /// Gets or sets a value indicating whether duplicate asset warnings should be displayed on the loading screen scroll.
        /// </summary>
        [XmlElement("ShowDuplicateWarnings")]
        public bool XMLShowDuplicates { get => ShowDuplicates; set => ShowDuplicates = value; }

        /// <summary>
        /// Gets or sets a value indicating whether duplicate asset warnings should be displayed on the loading screen scroll.
        /// </summary>
        [XmlIgnore]
        internal static bool ShowDuplicates { get; set; } = false;

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