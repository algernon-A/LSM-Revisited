// <copyright file="LSMRSettings.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Xml.Serialization;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.XML;
    using ColossalFramework.IO;
    using UnityEngine;

    /// <summary>
    /// The Loading Screen Mod Revisited settings file.
    /// </summary>
    [XmlRoot("LoadingScreenModRevisited")]
    public class LSMRSettings : SettingsXMLBase
    {
        // Settings file name.
        [XmlIgnore]
        private static readonly string SettingsFileName = Path.Combine(DataLocation.localApplicationData, "LoadingScreenModRevisited.xml");

        // Current reports directory.
        private static string s_reportsDirectory = DefaultReportsDirectory;

        // Current skip file path.
        private static string s_skipFile = Path.Combine(Path.Combine(DataLocation.localApplicationData, "SkippedPrefabs"), "skip.txt");

        /// <summary>
        /// Gets or sets the loading image mode.
        /// </summary>
        [XmlElement("BackgroundImageMode")]
        public BackgroundImage.ImageMode XMLImageMode { get => BackgroundImage.CurrentImageMode; set => BackgroundImage.CurrentImageMode = value; }

        /// <summary>
        /// Gets or sets a value indicating whether attempts shoudl be made to recover from simulation errors on the next load.
        /// </summary>
        [XmlElement("BackgroundImageScaling")]
        public ScaleMode XMLImageScale { get => BackgroundImage.ImageScaling; set => BackgroundImage.ImageScaling = value; }

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
        /// Gets or sets a value indicating whether enabled assets should be loaded (regardless of usage).
        /// </summary>
        [XmlElement("loadEnabled")]
        public bool XMLLoadEnabled { get => LoadEnabled; set => LoadEnabled = value; }

        /// <summary>
        /// Gets or sets a value indicating whether assets used in this save should be loaded (regardless of enabled state).
        /// </summary>
        [XmlElement("loadUsed")]
        public bool XMLLoadUsed { get => LoadUsed; set => LoadUsed = value; }

        /// <summary>
        /// Gets or sets a value indicating whether prefab skipping is enabled.
        /// </summary>
        [XmlElement("skipPrefabs")]
        public bool XMLSkipPrefab { get => SkipPrefabs; set => SkipPrefabs = value; }

        /// <summary>
        /// Gets or sets a the skip file name.
        /// </summary>
        [XmlElement("skipFile")]
        public string XMLSkipFile { get => SkipFile; set => SkipFile = value; }

        /// <summary>
        /// Gets or sets a value indicating whether asset reporting suppression is in effect.
        /// </summary>
        [XmlElement("hideAssets")]
        public bool XMLHideAssets { get => HideAssets; set => HideAssets = value; }

        /// <summary>
        /// Gets or sets a value indicating whether asset texture sharing is enabled.
        /// </summary>
        [XmlElement("shareTextures")]
        public bool XMLShareTextures { get => ShareTextures; set => ShareTextures = value; }

        /// <summary>
        /// Gets or sets a value indicating whether asset material sharing is enabled.
        /// </summary>
        [XmlElement("shareMaterials")]
        public bool XMLShareMaterisals { get => ShareMaterials; set => ShareMaterials = value; }

        /// <summary>
        /// Gets or sets a value indicating whether asset mesh sharing is enabled.
        /// </summary>
        [XmlElement("shareMeshes")]
        public bool XMLShareMeshes { get => ShareMeshes; set => ShareMeshes = value; }

        /// <summary>
        /// Gets or sets a value indicating whether asset thumbnails should be optimized.
        /// </summary>
        [XmlElement("optimizeThumbs")]
        public bool XMLOptimizeThumbs { get => OptimizeThumbs; set => OptimizeThumbs = value; }

        /// <summary>
        /// Gets or sets a value indicating whether an asset report should be generated.
        /// </summary>
        [XmlElement("reportAssets")]
        public bool XMLReportAssets { get => ReportAssets; set => ReportAssets = value; }

        /// <summary>
        /// Gets or sets a value indicating whether assets should be checked for possible errors.
        /// </summary>
        [XmlElement("checkAssets")]
        public bool XMLCheckAssets { get => CheckAssets; set => CheckAssets = value; }

        /// <summary>
        /// Gets or sets a value indicating whether asset mesh sharing is enabled.
        /// </summary>
        [XmlElement("reportDir")]
        public string XMLReportDirectory { get => ReportDirectory; set => ReportDirectory = value; }

        /// <summary>
        /// Gets or sets a value indicating whether vehicle instances should be removed on the next load.
        /// </summary>
        [XmlElement("removeVehicles")]
        public bool XMLRemoveVehicles { get => RemoveVehicles; set => RemoveVehicles = value; }

        /// <summary>
        /// Gets or sets a value indicating whether citizen instances should be removed on the next load.
        /// </summary>
        [XmlElement("removeCitizenInstances")]
        public bool XMLRemoveCitizenInstances { get => RemoveCitizenInstances; set => RemoveCitizenInstances = value; }

        /// <summary>
        /// Gets or sets a value indicating whether attempts shoudl be made to recover from simulation errors on the next load.
        /// </summary>
        [XmlElement("recover")]
        public bool XMLTryRecover { get => TryRecover; set => TryRecover = value; }

        /// <summary>
        /// Gets or sets a value indicating whether duplicate asset warnings should be displayed on the loading screen scroll.
        /// </summary>
        [XmlIgnore]
        internal static bool ShowDuplicates { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether enabled assets should be loaded (regardless of usage).
        /// </summary>
        [XmlIgnore]
        internal static bool LoadEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether assets used in this save should be loaded (regardless of enabled state).
        /// </summary>
        [XmlIgnore]
        internal static bool LoadUsed { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether prefab skipping is enabled.
        /// </summary>
        [XmlIgnore]
        internal static bool SkipPrefabs { get; set; } = true;

        /// <summary>
        /// Gets or sets a the skip file name.
        /// </summary>
        [XmlIgnore]
        internal static string SkipFile
        {
            get => s_skipFile;
            set
            {
                // Trim any whitespace (untrusted input).
                s_skipFile = value?.Trim();

                // Reset timestamp to indicate unloaded fule.
                SkipFileTimestamp = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Gets or sets the timestamp for the last skip file update.
        /// </summary>
        [XmlIgnore]

        internal static DateTime SkipFileTimestamp { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Gets or sets a value indicating whether asset reporting suppression is in effect.
        /// </summary>
        [XmlIgnore]
        internal static bool HideAssets { get; set; } = false;

        /// <summary>
        /// Gets the hidden assets file path.
        /// </summary>
        [XmlIgnore]
        internal static string HiddenAssetsFile => Path.Combine(Path.Combine(DataLocation.localApplicationData, "HiddenAssets"), "hide.txt");

        /// <summary>
        /// Gets or sets a value indicating whether asset texture sharing is enabled.
        /// </summary>
        [XmlIgnore]
        internal static bool ShareTextures { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether asset material sharing is enabled.
        /// </summary>
        [XmlIgnore]
        internal static bool ShareMaterials { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether asset mesh sharing is enabled.
        /// </summary>
        [XmlIgnore]
        internal static bool ShareMeshes { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether asset thumbnails should be optimized.
        /// </summary>
        [XmlIgnore]
        internal static bool OptimizeThumbs { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether an asset report should be generated.
        /// </summary>
        [XmlIgnore]
        internal static bool ReportAssets { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether assets should be checked for possible errors.
        /// </summary>
        [XmlIgnore]
        internal static bool CheckAssets { get; set; } = true;

        /// <summary>
        /// Gets a value indicating whether assets should be recorded.
        /// </summary>
        [XmlIgnore]
        internal static bool RecordAssets => ReportAssets | HideAssets | EnableDisable;

        /// <summary>
        /// Gets a value indicating whether any instance removal settings are in effect.
        /// </summary>
        [XmlIgnore]
        internal static bool Removals => RemoveVehicles | RemoveCitizenInstances;

        /// <summary>
        /// Gets or sets a value indicating whether asset mesh sharing is enabled.
        /// </summary>
        [XmlIgnore]
        internal static string ReportDirectory
        {
            get => s_reportsDirectory;

            set
            {
                // Trim any whitespace (untrusted input).
                s_reportsDirectory = value?.Trim();

                // If string is invalid, reset to default.
                if (string.IsNullOrEmpty(s_reportsDirectory))
                {
                    s_reportsDirectory = DefaultReportsDirectory;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether vehicle instances should be removed on the next load.
        /// </summary>
        [XmlIgnore]
        internal static bool RemoveVehicles { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether citizen instances should be removed on the next load.
        /// </summary>
        [XmlIgnore]
        internal static bool RemoveCitizenInstances { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether attempts shoudl be made to recover from simulation errors on the next load.
        /// </summary>
        [XmlIgnore]
        internal static bool TryRecover { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether assets should be enabled and disabled to match usage of the current save.
        /// </summary>
        [XmlIgnore]
        internal static bool EnableDisable { get; set; } = false;

        /// <summary>
        /// Gets the default reports directory path.
        /// </summary>
        [XmlIgnore]
        private static string DefaultReportsDirectory => Path.Combine(Path.Combine(DataLocation.localApplicationData, "Report"), "LoadingScreenMod");

        /// <summary>
        /// Loads settings from file.
        /// </summary>
        internal static void Load()
        {
            // Load any existing LSMR settings file first.
            if (File.Exists(SettingsFileName))
            {
                XMLFileUtils.Load<LSMRSettings>(SettingsFileName);
            }
            else
            {
                // No LSMR settings file - attempt to load legacy LSM settings file.
                LoadingScreenMod.Settings lsmSettings = LoadingScreenMod.Settings.settings;
                if (lsmSettings != null)
                {
                    Logging.KeyMessage("found legacy LSM settings file");

                    LoadEnabled = lsmSettings.loadEnabled;
                    LoadUsed = lsmSettings.loadUsed;
                    ShareTextures = lsmSettings.shareTextures;
                    ShareMaterials = lsmSettings.shareMaterials;
                    ShareMeshes = lsmSettings.shareMeshes;
                    OptimizeThumbs = lsmSettings.optimizeThumbs;
                    ReportAssets = lsmSettings.reportAssets;
                    CheckAssets = lsmSettings.checkAssets;
                    ReportDirectory = lsmSettings.reportDir;
                    SkipPrefabs = lsmSettings.skipPrefabs;
                    SkipFile = lsmSettings.skipFile;
                    HideAssets = lsmSettings.hideAssets;
                }
            }
        }

        /// <summary>
        /// Saves settings to file.
        /// </summary>
        internal static void Save() => XMLFileUtils.Save<LSMRSettings>(SettingsFileName);

        /// <summary>
        /// Ensures that the reporting directory exists, creating it if necessary and returning the full filepath.
        /// If unable, the default location will be returned.
        /// </summary>
        /// <returns>Report directory location.</returns>
        internal static string EnsureReportDirectory()
        {
            try
            {
                if (!Directory.Exists(s_reportsDirectory))
                {
                    Directory.CreateDirectory(s_reportsDirectory);
                }

                return s_reportsDirectory;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception creating reporting directory ", s_reportsDirectory ?? "null");
            }

            return DefaultReportsDirectory;
        }

        /// <summary>
        /// Opens the hidden assets file.
        /// </summary>
        internal static void OpenHideFile()
        {
            // Check to see if the file already exists.
            if (!File.Exists(HiddenAssetsFile))
            {
                // No - try to create it.
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(HiddenAssetsFile));
                    using (StreamWriter streamWriter = new StreamWriter(HiddenAssetsFile))
                    {
                        streamWriter.WriteLine(Translations.Translate("GO_AHEAD"));
                    }
                }
                catch (Exception e)
                {
                    Logging.LogException(e, "exception creating hidden assets file ", HiddenAssetsFile ?? "null");
                }
            }

            // Open the file.
            try
            {
                Process.Start(HiddenAssetsFile);
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception opening hidden assets file ", HiddenAssetsFile ?? "null");
            }
        }
    }
}