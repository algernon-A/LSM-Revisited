﻿// <copyright file="ReportingOptions.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LoadingScreenModRevisited
{
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Options panel for setting reporting options.
    /// </summary>
    internal class ReportingOptions : OptionsPanelTab
    {
        // Panel components.
        private readonly UITextField _reportTextField;

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

            _reportTextField = TextField(reportingGroup, LSMRSettings.ReportDirectory, (text) =>
            {
                if (text != LSMRSettings.ReportDirectory)
                {
                    LSMRSettings.ReportDirectory = text;
                }
            });

            // Buttons.
            UIComponent reportingPanel = reportingGroup.self as UIComponent;
            UIPanel buttonPanel = reportingPanel.AddUIComponent<UIPanel>();
            buttonPanel.autoLayout = false;

            UIButton openReportDirectoryButton = buttonPanel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsButtonTemplate")) as UIButton;
            openReportDirectoryButton.relativePosition = Vector2.zero;
            openReportDirectoryButton.text = Translations.Translate("OPEN_DIRECTORY");
            openReportDirectoryButton.tooltip = Translations.Translate("CLICK_TO_OPEN") + ' ' + LoadingScreenMod.Settings.HiddenAssetsFile;
            openReportDirectoryButton.eventClicked += (c, p) => LSMRSettings.OpenReportDirectory();

            UIButton resetFileLocationButton = buttonPanel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsButtonTemplate")) as UIButton;
            resetFileLocationButton.relativePosition = new Vector2(openReportDirectoryButton.width + 10f, 0f);
            resetFileLocationButton.text = Translations.Translate("RESET_FILE_LOCATION");
            resetFileLocationButton.eventClicked += (c, p) =>
            {
                LSMRSettings.ReportDirectory = LSMRSettings.DefaultReportsDirectory;
                _reportTextField.text = LSMRSettings.ReportDirectory;
            };

            buttonPanel.height = 40f;

            // Suprressing options.
            UIHelper suppressingGroup = AddGroup(helper, Translations.Translate("DO_NOT_REPORT"));

            UICheckBox hideCheck = suppressingGroup.AddCheckbox(Translations.Translate("DO_NOT_REPORT_THESE"), LSMRSettings.HideAssets, (isChecked) => { LSMRSettings.HideAssets = isChecked; }) as UICheckBox;
            hideCheck.tooltipBox = UIToolTips.WordWrapToolTip;
            hideCheck.tooltip = Translations.Translate("DO_NOT_REPORT_THESE_TOOLTIP");
            UIButton openHideFileButton = suppressingGroup.AddButton(Translations.Translate("OPEN_FILE"), LSMRSettings.OpenHideFile) as UIButton;
            openHideFileButton.tooltip = Translations.Translate("CLICK_TO_OPEN") + ' ' + LoadingScreenMod.Settings.HiddenAssetsFile;

            // Debugging options.
            UIHelper debugGroup = AddGroup(helper, Translations.Translate("DETAIL_LOGGING"));
            UICheckBox detailedLoggingCheck = debugGroup.AddCheckbox(Translations.Translate("DETAIL_LOGGING"), Logging.DetailLogging, (isChecked) => Logging.DetailLogging = isChecked) as UICheckBox;
        }
    }
}