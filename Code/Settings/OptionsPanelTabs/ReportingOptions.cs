// <copyright file="ReportingOptions.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// Options panel for setting reporting options.
    /// </summary>
    internal class ReportingOptions : OptionsPanelTab
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReportingOptions"/> class.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to.</param>
        /// <param name="tabIndex">Index number of tab.</param>
        internal ReportingOptions(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = UITabstrips.AddTextTab(tabStrip, Translations.Translate("OPTIONS_REPORTING"), tabIndex, out UIButton _, autoLayout: true);
            panel.autoLayoutDirection = LayoutDirection.Vertical;

            // Add controls.
            UIHelper helper = new UIHelper(panel);

            // Reporting options.
            UIHelper reportingGroup = AddGroup(helper, Translations.Translate("REPORTING"));

            UICheckBox hideDuplicatesCheck = reportingGroup.AddCheckbox(Translations.Translate("WARN_DUPLICATES"), LSMRSettings.ShowDuplicates, (isChecked) => LSMRSettings.ShowDuplicates = isChecked) as UICheckBox;
            hideDuplicatesCheck.tooltip = Translations.Translate("WARN_DUPLICATES_TIP") +
                System.Environment.NewLine + Translations.Translate("DUPLICATE_NAMES_EXPLAIN_1") +
                System.Environment.NewLine + Translations.Translate("DUPLICATE_NAMES_EXPLAIN_2");

            UICheckBox reportCheck = null;
            UICheckBox checkCheck = reportingGroup.AddCheckbox(Translations.Translate("CHECK_FOR_ERRORS"), LSMRSettings.CheckAssets, (isChecked) =>
            {
                LSMRSettings.CheckAssets = isChecked;
                LSMRSettings.ReportAssets |= isChecked;
                reportCheck.isChecked = LSMRSettings.ReportAssets;
            }) as UICheckBox;

            reportCheck = reportingGroup.AddCheckbox(Translations.Translate("SAVE_REPORTS_IN_DIRECTORY"), LSMRSettings.ReportAssets, (isChecked) =>
            {
                LSMRSettings.ReportAssets = isChecked;
                LSMRSettings.CheckAssets &= isChecked;
                checkCheck.isChecked = LSMRSettings.CheckAssets;
            }) as UICheckBox;
            reportCheck.tooltip = Translations.Translate("SAVE_REPORTS_OF_ASSETS");

            TextField(reportingGroup, LSMRSettings.ReportDirectory, (text) =>
            {
                if (text != LSMRSettings.ReportDirectory)
                {
                    LSMRSettings.ReportDirectory = text;
                }
            });

            UIButton openReportDirectoryButton = reportingGroup.AddButton(Translations.Translate("OPEN_DIRECTORY"), LSMRSettings.OpenReportDirectory) as UIButton;
            openReportDirectoryButton.tooltip = Translations.Translate("CLICK_TO_OPEN") + ' ' + LoadingScreenMod.Settings.HiddenAssetsFile;

            // Suprressing options.
            UIHelper suppressingGroup = AddGroup(helper, Translations.Translate("DO_NOT_REPORT"));

            UICheckBox hideCheck = suppressingGroup.AddCheckbox(Translations.Translate("DO_NOT_REPORT_THESE"), LSMRSettings.HideAssets, (isChecked) => { LSMRSettings.HideAssets = isChecked; }) as UICheckBox;
            hideCheck.tooltipBox = UIToolTips.WordWrapToolTip;
            hideCheck.tooltip = Translations.Translate("DO_NOT_REPORT_THESE_TOOLTIP");
            UIButton openHideFileButton = suppressingGroup.AddButton(Translations.Translate("OPEN_FILE"), LSMRSettings.OpenHideFile) as UIButton;
            openHideFileButton.tooltip = Translations.Translate("CLICK_TO_OPEN") + ' ' + LoadingScreenMod.Settings.HiddenAssetsFile;
        }
    }
}