using ColossalFramework.UI;
using ICities;
using System;
using UnityEngine;


namespace LoadingScreenModRevisited
{
    /// <summary>
    /// Options panel for setting reporting options.
    /// </summary>
    internal class ReportingOptions : OptionsPanelTab
    {
        /// <summary>
        /// Adds reporting options tab to tabstrip.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to</param>
        /// <param name="tabIndex">Index number of tab</param>
        internal ReportingOptions(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = OptionsPanel.AddTab(tabStrip, Translations.Translate("OPTIONS_REPORTING"), tabIndex, true);
            panel.autoLayoutDirection = LayoutDirection.Vertical;

            // Add controls.
            UIHelper helper = new UIHelper(panel);

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
            UICheckBox hideCheck = reportingGroup.AddCheckbox(Translations.Translate("DO_NOT_REPORT_THESE"), LoadingScreenMod.Settings.settings.hideAssets, (isChecked) => { LoadingScreenMod.Settings.settings.hideAssets = isChecked; }) as UICheckBox;
            UIButton openHideFileButton = reportingGroup.AddButton(Translations.Translate("OPEN_FILE"), LoadingScreenMod.Settings.settings.OnAssetsButton) as UIButton;
            openHideFileButton.tooltip = Translations.Translate("CLICK_TO_OPEN") + ' ' + LoadingScreenMod.Settings.HiddenAssetsFile;
        }
    }
}