// <copyright file="GeneralOptions.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Options panel for setting basic mod options.
    /// </summary>
    internal class GeneralOptions : OptionsPanelTab
    {
        // Panel components.
        private readonly UITextField _skipFileTextField;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralOptions"/> class.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to.</param>
        /// <param name="tabIndex">Index number of tab.</param>
        internal GeneralOptions(UITabstrip tabStrip, int tabIndex)
        {
            Logging.Message("creating general options panel");

            // Add tab and helper.
            UIPanel panel = UITabstrips.AddTextTab(tabStrip, Translations.Translate("OPTIONS_GENERAL"), tabIndex, out UIButton _, autoLayout: true);
            panel.autoLayoutDirection = LayoutDirection.Vertical;

            // Add controls.
            UIHelper helper = new UIHelper(panel);

            UIHelper languageGroup = AddGroup(helper, Translations.Translate("CHOOSE_LANGUAGE"));

            // Language selection.
            UIDropDown languageDropDown = languageGroup.AddDropdown(" ", Translations.LanguageList, Translations.Index, (index) =>
            {
                Translations.Index = index;
                OptionsPanelManager<OptionsPanel>.LocaleChanged();
            }) as UIDropDown;
            languageDropDown.width += 200f;

            // Remove language dropdown label.
            UIComponent languageParent = languageDropDown.parent;
            if (languageParent.Find("Label") is UILabel label)
            {
                languageParent.height -= label.height;
                label.height = 0;
                label.Hide();
            }

            // Asset loading options.
            UIHelper assetGroup = AddGroup(helper, Translations.Translate("LOADING_OPTIONS_FOR_ASSETS"));
            UICheckBox loadEnabledCheck = assetGroup.AddCheckbox(Translations.Translate("LOAD_ENABLED_ASSETS"), LSMRSettings.LoadEnabled, (isChecked) =>
            {
                LSMRSettings.LoadEnabled = isChecked;
                LevelLoader.Reset();
            }) as UICheckBox;
            loadEnabledCheck.tooltip = Translations.Translate("LOAD_ENABLED_IN_CM");
            UICheckBox loadUsedCheck = assetGroup.AddCheckbox(Translations.Translate("LOAD_USED_ASSETS"), LSMRSettings.LoadUsed, (isChecked) =>
            {
                LSMRSettings.LoadUsed = isChecked;
                LevelLoader.Reset();
            }) as UICheckBox;
            loadUsedCheck.tooltip = Translations.Translate("LOAD_USED_IN_YOUR_CITY");

            string replaceDuplicates = Translations.Translate("REPLACE_DUPLICATES");
            UICheckBox shareTexturesCheck = assetGroup.AddCheckbox(Translations.Translate("SHARE_TEXTURES"), LSMRSettings.ShareTextures, (isChecked) => { LSMRSettings.ShareTextures = isChecked; }) as UICheckBox;
            shareTexturesCheck.tooltip = replaceDuplicates;
            UICheckBox shareMaterialsCheck = assetGroup.AddCheckbox(Translations.Translate("SHARE_MATERIALS"), LSMRSettings.ShareMaterials, (isChecked) => { LSMRSettings.ShareMaterials = isChecked; }) as UICheckBox;
            shareMaterialsCheck.tooltip = replaceDuplicates;
            UICheckBox shareMeshesCheck = assetGroup.AddCheckbox(Translations.Translate("SHARE_MESHES"), LSMRSettings.ShareMeshes, (isChecked) => { LSMRSettings.ShareMeshes = isChecked; }) as UICheckBox;
            shareMeshesCheck.tooltip = replaceDuplicates;
            UICheckBox optimizeThumbsCheck = assetGroup.AddCheckbox(Translations.Translate("OPTIMIZE_THUMBNAILS"), LSMRSettings.OptimizeThumbs, (isChecked) => { LSMRSettings.OptimizeThumbs = isChecked; }) as UICheckBox;
            optimizeThumbsCheck.tooltip = Translations.Translate("OPTIMIZE_TEXTURES");

            // Prefab skipping options.
            UIHelper skippingGroup = AddGroup(helper, Translations.Translate("PREFAB_SKIPPING"), Translations.Translate("PREFAB_MEANS"));
            UICheckBox skipCheck = skippingGroup.AddCheckbox(Translations.Translate("SKIP_THESE"), LSMRSettings.SkipPrefabs, (isChecked) => { LSMRSettings.SkipPrefabs = isChecked; }) as UICheckBox;
            _skipFileTextField = TextField(skippingGroup, LSMRSettings.SkipFile, (value) => LSMRSettings.SkipFile = value);
            UIButton openSkipFileButton = skippingGroup.AddButton(Translations.Translate("OPEN_FILE"), OpenSkipFile) as UIButton;
            openSkipFileButton.tooltip = Translations.Translate("CLICK_TO_OPEN");
            UIButton resetSkipLocationButton = openSkipFileButton.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsButtonTemplate")) as UIButton;
            resetSkipLocationButton.relativePosition = new Vector2(openSkipFileButton.width + 10f, 0f);
            resetSkipLocationButton.text = Translations.Translate("RESET_FILE_LOCATION");
            resetSkipLocationButton.eventClicked += (c, p) =>
            {
                LSMRSettings.SkipFile = LoadingScreenMod.Settings.DefaultSkipFile;
                _skipFileTextField.text = LSMRSettings.SkipFile;
            };

            // Recovery options.
            string tooltipString = Translations.Translate("AUTOMATICALLY_DISABLED");
            UIHelper recoveryGroup = AddGroup(helper, Translations.Translate("SAFE_MODE"), tooltipString);
            UILabel recoveryLabel = ((recoveryGroup as UIHelper)?.self as UIPanel)?.Find<UILabel>("Label");
            if (recoveryLabel != null)
            {
                recoveryLabel.tooltip = tooltipString;
            }

            UICheckBox removeVehiclesCheck = recoveryGroup.AddCheckbox(Translations.Translate("REMOVE_VEHICLE_AGENTS"), LSMRSettings.RemoveVehicles, (isChecked) => { LSMRSettings.RemoveVehicles = isChecked; }) as UICheckBox;
            UICheckBox removeCitizensCheck = recoveryGroup.AddCheckbox(Translations.Translate("REMOVE_CITIZEN_AGENTS"), LSMRSettings.RemoveCitizenInstances, (isChecked) => { LSMRSettings.RemoveCitizenInstances = isChecked; }) as UICheckBox;
            removeVehiclesCheck.tooltip = tooltipString;
            removeCitizensCheck.tooltip = tooltipString;

            UIHelper simulationGroup = AddGroup(helper, Translations.Translate("TRY_TO_RECOVER"));
            UILabel simulationLabel = ((recoveryGroup as UIHelper)?.self as UIPanel)?.Find<UILabel>("Label");
            if (simulationLabel != null)
            {
                simulationLabel.tooltip = tooltipString;
            }

            UICheckBox recoverNetCheck = simulationGroup.AddCheckbox(Translations.Translate("RECOVER_NETS"), LSMRSettings.RecoverMissingNets, (isChecked) => { LSMRSettings.RecoverMissingNets = isChecked; }) as UICheckBox;
            recoverNetCheck.tooltip = tooltipString;
        }

        /// <summary>
        /// Opens the current skip prefabs file.
        /// </summary>
        private void OpenSkipFile()
        {
            try
            {
                // If string is null, reset it.
                if (LSMRSettings.SkipFile.IsNullOrWhiteSpace())
                {
                    LSMRSettings.SkipFile = LoadingScreenMod.Settings.DefaultSkipFile;

                    // Update textfield.
                    _skipFileTextField.text = LSMRSettings.SkipFile;
                }

                // Create directory if it doesn't already exist.
                string directoryName = Path.GetDirectoryName(LSMRSettings.SkipFile);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                // Create file if it doesn't already exist.
                if (!File.Exists(LSMRSettings.SkipFile))
                {
                    using (StreamWriter writer = new StreamWriter(LSMRSettings.SkipFile))
                    {
                        writer.Write("# Loading Screen Mod skip file");
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception creating skip file ", LSMRSettings.SkipFile);
            }

            // Open the file.
            try
            {
                Process.Start(LSMRSettings.SkipFile);
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception opening skip file ", LSMRSettings.SkipFile);
            }
        }
    }
}