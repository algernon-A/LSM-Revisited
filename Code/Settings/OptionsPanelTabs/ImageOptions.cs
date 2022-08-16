// <copyright file="ImageOptions.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// Options panel for setting background loading image options.
    /// </summary>
    internal class ImageOptions : OptionsPanelTab
    {
        // Panel components.
        private readonly UICheckBox _defaultCheck;
        private readonly UICheckBox _imgurCuratedCheck;
        private readonly UICheckBox _imgurRandomCheck;
        private readonly UICheckBox _localRandomCheck;

        // Event processing.
        private bool ignoreEvents = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageOptions"/> class.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to.</param>
        /// <param name="tabIndex">Index number of tab.</param>
        internal ImageOptions(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = UITabstrips.AddTextTab(tabStrip, Translations.Translate("OPTIONS_IMAGE"), tabIndex, out UIButton _, autoLayout: true);
            panel.autoLayoutDirection = LayoutDirection.Vertical;

            // Add controls.
            UIHelper helper = new UIHelper(panel);

            // Add curated imgur image check.
            _defaultCheck = helper.AddCheckbox(
                Translations.Translate("DEFAULT_BACKGROUND"),
                BackgroundImage.CurrentImageMode == BackgroundImage.ImageMode.Standard,
                DefaultCheckChanged) as UICheckBox;

            // Add curated imgur image check.
            _imgurCuratedCheck = helper.AddCheckbox(
                Translations.Translate("IMGUR_CURATED"),
                BackgroundImage.CurrentImageMode == BackgroundImage.ImageMode.ImgurCurated,
                CuratedCheckChanged) as UICheckBox;
            _imgurCuratedCheck.tooltip = Translations.Translate("IMGUR_CURATED_TIP");

            // Add random imgur image check.
            _imgurRandomCheck = helper.AddCheckbox(
                Translations.Translate("IMGUR_TOP"),
                BackgroundImage.CurrentImageMode == BackgroundImage.ImageMode.ImgurRandom,
                RandomCheckChanged) as UICheckBox;
            _imgurRandomCheck.tooltip = Translations.Translate("IMGUR_TOP_TIP");

            // Random local image check.
            _localRandomCheck = helper.AddCheckbox(
                Translations.Translate("LOCAL_IMAGE"),
                BackgroundImage.CurrentImageMode == BackgroundImage.ImageMode.LocalRandom,
                LocalCheckChanged) as UICheckBox;
            _localRandomCheck.tooltip = Translations.Translate("LOCAL_IMAGE_TIP");

            TextField(helper, BackgroundImage.ImageDirectory, (text) =>
            {
                if (text != BackgroundImage.ImageDirectory)
                {
                    BackgroundImage.ImageDirectory = text;
                }
            });
        }

        /// <summary>
        /// Imgur curated backgroung image check change handler.
        /// </summary>
        /// <param name="isChecked">New checked status.</param>
        private void DefaultCheckChanged(bool isChecked)
        {
            // Don't do anything if events are ignored.
            if (ignoreEvents)
            {
                return;
            }

            // Only update if this is being checked.
            if (isChecked)
            {
                BackgroundImage.CurrentImageMode = BackgroundImage.ImageMode.Standard;
            }

            // Update all check states.
            UpdateChecks();
        }

        /// <summary>
        /// Imgur curated background image check change handler.
        /// </summary>
        /// <param name="isChecked">New checked status.</param>
        private void CuratedCheckChanged(bool isChecked)
        {
            // Don't do anything if events are ignored.
            if (ignoreEvents)
            {
                return;
            }

            // Only update if this is being checked.
            if (isChecked)
            {
                BackgroundImage.CurrentImageMode = BackgroundImage.ImageMode.ImgurCurated;
            }

            // Update all check states.
            UpdateChecks();
        }

        /// <summary>
        /// Imgur random background image check change handler.
        /// </summary>
        /// <param name="isChecked">New checked status.</param>
        private void RandomCheckChanged(bool isChecked)
        {
            // Don't do anything if events are ignored.
            if (ignoreEvents)
            {
                return;
            }

            // Only update if this is being checked.
            if (isChecked)
            {
                BackgroundImage.CurrentImageMode = BackgroundImage.ImageMode.ImgurRandom;
            }

            // Update all check states.
            UpdateChecks();
        }

        /// <summary>
        /// Imgur random background image check change handler.
        /// </summary>
        /// <param name="isChecked">New checked status.</param>
        private void LocalCheckChanged(bool isChecked)
        {
            // Don't do anything if events are ignored.
            if (ignoreEvents)
            {
                return;
            }

            // Only update if this is being checked.
            if (isChecked)
            {
                BackgroundImage.CurrentImageMode = BackgroundImage.ImageMode.LocalRandom;
            }

            // Update all check states.
            UpdateChecks();
        }

        /// <summary>
        /// Updates checkbox states to match current settings.
        /// </summary>
        private void UpdateChecks()
        {
            // Suspend events while updating.
            ignoreEvents = true;

            // Set check states.
            _defaultCheck.isChecked = BackgroundImage.CurrentImageMode == BackgroundImage.ImageMode.Standard;
            _imgurCuratedCheck.isChecked = BackgroundImage.CurrentImageMode == BackgroundImage.ImageMode.ImgurCurated;
            _imgurRandomCheck.isChecked = BackgroundImage.CurrentImageMode == BackgroundImage.ImageMode.ImgurRandom;
            _localRandomCheck.isChecked = BackgroundImage.CurrentImageMode == BackgroundImage.ImageMode.LocalRandom;

            // Resume event handling.
            ignoreEvents = false;
        }
    }
}