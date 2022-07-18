using ColossalFramework.UI;


namespace LoadingScreenModRevisited
{
    /// <summary>
    /// Options panel for setting basic mod options.
    /// </summary>
    internal class GeneralOptions : OptionsPanelTab
    {
        /// <summary>
        /// Adds mod options tab to tabstrip.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to</param>
        /// <param name="tabIndex">Index number of tab</param>
        internal GeneralOptions(UITabstrip tabStrip, int tabIndex)
        {
            Logging.Message("creating general options panel");

            // Add tab and helper.
            UIPanel panel = OptionsPanel.AddTab(tabStrip, Translations.Translate("OPTIONS_GENERAL"), tabIndex, true);
            panel.autoLayoutDirection = LayoutDirection.Vertical;

            // Add controls.
            UIHelper helper = new UIHelper(panel);

            UIHelper languageGroup = AddGroup(helper, Translations.Translate("CHOOSE_LANGUAGE"));
            // Language selection.
            UIDropDown languageDropDown = languageGroup.AddDropdown(" ", Translations.LanguageList, Translations.Index, (index) =>
            {
                Translations.Index = index;
                OptionsPanel.LocaleChanged();
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
            UICheckBox loadEnabledCheck = assetGroup.AddCheckbox(Translations.Translate("LOAD_ENABLED_ASSETS"), LoadingScreenMod.Settings.settings.loadEnabled, (isChecked) => { LoadingScreenMod.Settings.settings.loadEnabled = isChecked; LevelLoader.Reset(); }) as UICheckBox;
            loadEnabledCheck.tooltip = Translations.Translate("LOAD_ENABLED_IN_CM");
            UICheckBox loadUsedCheck = assetGroup.AddCheckbox(Translations.Translate("LOAD_USED_ASSETS"), LoadingScreenMod.Settings.settings.loadUsed, (isChecked) => { LoadingScreenMod.Settings.settings.loadUsed = isChecked; LevelLoader.Reset(); }) as UICheckBox;
            loadUsedCheck.tooltip = Translations.Translate("LOAD_USED_IN_YOUR_CITY");

            string replaceDuplicates = Translations.Translate("REPLACE_DUPLICATES");
            UICheckBox shareTexturesCheck = assetGroup.AddCheckbox(Translations.Translate("SHARE_TEXTURES"), LoadingScreenMod.Settings.settings.shareTextures, (isChecked) => { LoadingScreenMod.Settings.settings.shareTextures = isChecked; }) as UICheckBox;
            shareTexturesCheck.tooltip = replaceDuplicates;
            UICheckBox shareMaterialsCheck = assetGroup.AddCheckbox(Translations.Translate("SHARE_MATERIALS"), LoadingScreenMod.Settings.settings.shareMaterials, (isChecked) => { LoadingScreenMod.Settings.settings.shareMaterials = isChecked; }) as UICheckBox;
            shareMaterialsCheck.tooltip = replaceDuplicates;
            UICheckBox shareMeshesCheck = assetGroup.AddCheckbox(Translations.Translate("SHARE_MESHES"), LoadingScreenMod.Settings.settings.shareMeshes, (isChecked) => { LoadingScreenMod.Settings.settings.shareMeshes = isChecked; }) as UICheckBox;
            shareMeshesCheck.tooltip = replaceDuplicates;
            UICheckBox optimizeThumbsCheck = assetGroup.AddCheckbox(Translations.Translate("OPTIMIZE_THUMBNAILS"), LoadingScreenMod.Settings.settings.optimizeThumbs, (isChecked) => { LoadingScreenMod.Settings.settings.optimizeThumbs = isChecked; }) as UICheckBox;
            optimizeThumbsCheck.tooltip = Translations.Translate("OPTIMIZE_TEXTURES");

            // Prefab skipping options.
            UIHelper skippingGroup = AddGroup(helper, Translations.Translate("PREFAB_SKIPPING"), Translations.Translate("PREFAB_MEANS"));
            UICheckBox skipCheck = skippingGroup.AddCheckbox(Translations.Translate("SKIP_THESE"), LoadingScreenMod.Settings.settings.skipPrefabs, (isChecked) => { LoadingScreenMod.Settings.settings.skipPrefabs = isChecked; }) as UICheckBox;
            TextField(skippingGroup, LoadingScreenMod.Settings.settings.skipFile, LoadingScreenMod.Settings.settings.OnSkipFileChanged);

            // Recovery options.
            UIHelper recoveryGroup = AddGroup(helper, Translations.Translate("SAFE_MODE"), Translations.Translate("AUTOMATICALLY_DISABLED"));
            UILabel recoveryLabel = ((recoveryGroup as UIHelper)?.self as UIPanel)?.Find<UILabel>("Label");
            if (recoveryLabel != null)
            {
                recoveryLabel.tooltip = Translations.Translate("AUTOMATICALLY_DISABLED");
            }
            recoveryGroup.AddCheckbox(Translations.Translate("REMOVE_VEHICLE_AGENTS"), LoadingScreenMod.Settings.settings.removeVehicles, (isChecked) => { LoadingScreenMod.Settings.settings.removeVehicles = isChecked; });
            recoveryGroup.AddCheckbox(Translations.Translate("REMOVE_CITIZEN_AGENTS"), LoadingScreenMod.Settings.settings.removeCitizenInstances, (isChecked) => { LoadingScreenMod.Settings.settings.removeCitizenInstances = isChecked; });
            recoveryGroup.AddCheckbox(Translations.Translate("TRY_TO_RECOVER"), LoadingScreenMod.Settings.settings.recover, (isChecked) => { LoadingScreenMod.Settings.settings.recover = isChecked; });
        }
    }
}