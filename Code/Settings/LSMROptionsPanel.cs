using ColossalFramework.UI;
using ICities;
using System;
using UnityEngine;


namespace LoadingScreenModRevisited
{
    /// <summary>
    /// RON options panel.
    /// </summary>
    internal class LSMROptionsPanel : UIPanel
    {
        // Layout constants.
        private const float Margin = 5f;
        private const float GroupMargin = 40f;


        /// <summary>
        /// Constructor.
        /// </summary>
        internal LSMROptionsPanel()
        {
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Vertical;

            // If we have an invalid application version, then just display the message and nothing else.
            if (!BuildConfig.applicationVersion.StartsWith("1.14"))
            {
                autoLayout = false;
                AddLabel(this, Margin, GroupMargin, Translations.Translate("MAJOR_GAME_UPDATE"));
                AddLabel(this, Margin, GroupMargin * 2f, Translations.Translate("INCOMPATIBLE_VERSION"));
                return;
            }
            UIHelper helper = new UIHelper(this);

            // Language selection.
            UIDropDown languageDropDown = helper.AddDropdown(Translations.Translate("LANGUAGE"), Translations.LanguageList, Translations.Index, (index) =>
            {
                Translations.Index = index;
                OptionsPanelManager.LocaleChanged();
            }) as UIDropDown;

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

            // Reporting options.
            UIHelper reportingGroup = AddGroup(helper, Translations.Translate("REPORTING"));
            UICheckBox checkCheck = null;
            UICheckBox reportCheck = reportingGroup.AddCheckbox(Translations.Translate("SAVE_REPORTS_IN_DIRECTORY"), LoadingScreenMod.Settings.settings.reportAssets, (isChecked) =>
            {
                LoadingScreenMod.Settings.settings.reportAssets = isChecked;
                LoadingScreenMod.Settings.settings.checkAssets &= isChecked;
                checkCheck.isChecked = LoadingScreenMod.Settings.settings.checkAssets;
            }) as UICheckBox;
            reportCheck.tooltip = Translations.Translate("SAVE_REPORTS_OF_ASSETS");

            TextField(reportingGroup, LoadingScreenMod.Settings.settings.reportDir, (text) =>
            {
                if (text != LoadingScreenMod.Settings.settings.reportDir)
                {
                    LoadingScreenMod.Settings.settings.reportDir = text;
                }
            });
            checkCheck = reportingGroup.AddCheckbox(Translations.Translate("CHECK_FOR_ERRORS"), LoadingScreenMod.Settings.settings.checkAssets, (isChecked) =>
            {
                LoadingScreenMod.Settings.settings.checkAssets = isChecked;
                LoadingScreenMod.Settings.settings.reportAssets |= isChecked;
                reportCheck.isChecked = LoadingScreenMod.Settings.settings.reportAssets;
            }) as UICheckBox;
            UICheckBox hideCheck = assetGroup.AddCheckbox(Translations.Translate("DO_NOT_REPORT_THESE"), LoadingScreenMod.Settings.settings.hideAssets, (isChecked) => { LoadingScreenMod.Settings.settings.hideAssets = isChecked; }) as UICheckBox;
            UIButton openHideFileButton = assetGroup.AddButton(Translations.Translate("OPEN_FILE"), LoadingScreenMod.Settings.settings.OnAssetsButton) as UIButton;
            openHideFileButton.tooltip = Translations.Translate("CLICK_TO_OPEN") + ' ' + LoadingScreenMod.Settings.HiddenAssetsFile;

            // Prefab skipping options.
            UIHelper skippingGroup = AddGroup(helper, Translations.Translate("PREFAB_SKIPPING"), Translations.Translate("PREFAB_MEANS"));
            UICheckBox skipCheck = skippingGroup.AddCheckbox(Translations.Translate("SKIP_THESE"), LoadingScreenMod.Settings.settings.skipPrefabs, (isChecked) => { LoadingScreenMod.Settings.settings.skipPrefabs = isChecked; }) as UICheckBox;
            TextField(skippingGroup, LoadingScreenMod.Settings.settings.skipFile, LoadingScreenMod.Settings.settings.OnSkipFileChanged);

            // Recovery options.
            UIHelper recoveryGroup = AddGroup(helper, Translations.Translate("SAFE_MODE"));
            UILabel recoveryLabel = ((recoveryGroup as UIHelper)?.self as UIPanel)?.Find<UILabel>("Label");
            if (recoveryLabel != null)
            {
                recoveryLabel.tooltip = Translations.Translate("AUTOMATICALLY_DISABLED");
            }
            recoveryGroup.AddCheckbox(Translations.Translate("REMOVE_VEHICLE_AGENTS"), LoadingScreenMod.Settings.settings.removeVehicles, (isChecked) => { LoadingScreenMod.Settings.settings.removeVehicles = isChecked; });
            recoveryGroup.AddCheckbox(Translations.Translate("REMOVE_CITIZEN_AGENTS"), LoadingScreenMod.Settings.settings.removeCitizenInstances, (isChecked) => { LoadingScreenMod.Settings.settings.removeCitizenInstances = isChecked; });
            recoveryGroup.AddCheckbox(Translations.Translate("TRY_TO_RECOVER"), LoadingScreenMod.Settings.settings.recover, (isChecked) => { LoadingScreenMod.Settings.settings.recover = isChecked; });
        }


        /// <summary>
        /// Adds a plain text label to the specified UI panel.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="xPos">Relative x position)</param>
        /// <param name="yPos">Relative y position</param>
        /// <param name="text">Label text</param>
        /// <param name="width">Label width (-1 (default) for autosize)</param>
        /// <param name="width">Text scale (default 1.0)</param>
        /// <returns>New text label</returns>
        private UILabel AddLabel(UIComponent parent, float xPos, float yPos, string text, float width = -1f, float textScale = 1.0f)
        {
            // Add label.
            UILabel label = (UILabel)parent.AddUIComponent<UILabel>();

            // Set sizing options.
            if (width > 0f)
            {
                // Fixed width.
                label.autoSize = false;
                label.width = width;
                label.autoHeight = true;
                label.wordWrap = true;
            }
            else
            {
                // Autosize.
                label.autoSize = true;
                label.autoHeight = false;
                label.wordWrap = false;
            }

            // Text.
            label.textScale = textScale;
            label.text = text;

            // Position.
            label.relativePosition = new Vector2(xPos, yPos);

            return label;
        }


        /// <summary>
        /// Adds a textfield with no title label.
        /// </summary>
        /// <param name="group">UIHelper group</param>
        /// <param name="text">Initial text</param>
        /// <param name="action">Text changed action</param>
        private void TextField(UIHelper group, string text, OnTextChanged action)
        {
            try
            {
                // Need to add at least a space as a label, otherwise the field won't create.
                UITextField textField = group.AddTextfield(" ", text, action, null) as UITextField;

                // Increase width.
                textField.width *= 2.8f;

                // Find label.
                UIComponent parentPanel = textField.parent;
                UILabel uILabel = parentPanel?.Find<UILabel>("Label");
                if (uILabel != null)
                {
                    // Hide label and reduce containing panel height by label height.
                    float height = uILabel.height;
                    uILabel.height = 0f;
                    uILabel.Hide();
                    parentPanel.height -= height;
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "Exception creating options panel textfield");
            }
        }


        /// <summary>
        /// Adds a helper group with reduced bottom offsets.
        /// </summary>
        /// <param name="parent">Parent helper</param>
        /// <param name="title">Group title</param>
        /// <param name="tooltip">Group label tooltip</param>
        /// <returns></returns>
        private UIHelper AddGroup(UIHelperBase parent, string title, string tooltip = null)
        {
            // Add helper.
            UIHelper helper = parent.AddGroup(title) as UIHelper;

            // Reduce helper bottom offset.
            UIPanel helperPanel = helper.self as UIPanel;
            RectOffset rectOffset = helperPanel?.autoLayoutPadding;
            if (rectOffset != null)
            {
                rectOffset.bottom /= 2;
            }

            // Reduce helper parent bottom offset.
            UIPanel helperParent = helperPanel.parent as UIPanel;
            RectOffset parentRectOffset = helperParent?.autoLayoutPadding;
            if (parentRectOffset != null)
            {
                parentRectOffset.bottom /= 2;
            }

            // Add tooltip.
            if (!string.IsNullOrEmpty(tooltip))
            {
                UILabel helperLabel = helperParent?.Find<UILabel>("Label");
                if (helperLabel != null)
                {
                    helperLabel.tooltip = tooltip;
                }
            }

            return helper;
        }
    }
}