// <copyright file="ImageOptions.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using ICities;
    using UnityEngine;

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
        private readonly UICheckBox _fitImageCheck;
        private readonly UICheckBox _cropImageCheck;
        private readonly UICheckBox _stretchImageCheck;
        private readonly UILabel _textSizeLabel;
        private readonly UILabel _alphaLabel;

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

            // Image source options.
            UIHelperBase sourceGroup = helper.AddGroup(Translations.Translate("IMAGE_SOURCE"));

            // Add curated imgur image check.
            _defaultCheck = sourceGroup.AddCheckbox(
                Translations.Translate("DEFAULT_BACKGROUND"),
                BackgroundImage.CurrentImageMode == BackgroundImage.ImageMode.Standard,
                DefaultCheckChanged) as UICheckBox;

            // Add curated imgur image check.
            _imgurCuratedCheck = sourceGroup.AddCheckbox(
                Translations.Translate("IMGUR_CURATED"),
                BackgroundImage.CurrentImageMode == BackgroundImage.ImageMode.ImgurCurated,
                CuratedCheckChanged) as UICheckBox;
            _imgurCuratedCheck.tooltip = Translations.Translate("IMGUR_CURATED_TIP");

            // Add random imgur image check.
            _imgurRandomCheck = sourceGroup.AddCheckbox(
                Translations.Translate("IMGUR_TOP"),
                BackgroundImage.CurrentImageMode == BackgroundImage.ImageMode.ImgurRandom,
                RandomCheckChanged) as UICheckBox;
            _imgurRandomCheck.tooltip = Translations.Translate("IMGUR_TOP_TIP");

            // Random local image check.
            _localRandomCheck = sourceGroup.AddCheckbox(
                Translations.Translate("LOCAL_IMAGE"),
                BackgroundImage.CurrentImageMode == BackgroundImage.ImageMode.LocalRandom,
                LocalCheckChanged) as UICheckBox;
            _localRandomCheck.tooltip = Translations.Translate("LOCAL_IMAGE_TIP");

            TextField(sourceGroup as UIHelper, BackgroundImage.ImageDirectory, (text) =>
            {
                if (text != BackgroundImage.ImageDirectory)
                {
                    BackgroundImage.ImageDirectory = text;
                }
            });

            // Scaling options.
            UIHelperBase scaleGroup = helper.AddGroup(Translations.Translate("IMAGE_SCALE"));

            // Scale to fit.
            _fitImageCheck = scaleGroup.AddCheckbox(
                Translations.Translate("IMAGE_FIT"),
                BackgroundImage.ImageScaling == ScaleMode.ScaleToFit,
                FitCheckChanged) as UICheckBox;

            // Fit and crop.
            _cropImageCheck = scaleGroup.AddCheckbox(
                Translations.Translate("IMAGE_CROP"),
                BackgroundImage.ImageScaling == ScaleMode.ScaleAndCrop,
                CropCheckChanged) as UICheckBox;

            // Stretch to fill.
            _stretchImageCheck = scaleGroup.AddCheckbox(
                Translations.Translate("IMAGE_STRETCH"),
                BackgroundImage.ImageScaling == ScaleMode.StretchToFill,
                StretchCheckChanged) as UICheckBox;

            // Scaling options.
            UIHelperBase textGroup = helper.AddGroup(Translations.Translate("IMAGE_TEXT"));

            // Text font size.
            UISlider textSizeSlider = textGroup.AddSlider(
                Translations.Translate("TEXT_SIZE"),
                LoadingScreen.MinimumTextSize,
                LoadingScreen.MaximumTextSize,
                1f,
                LoadingScreen.TextSize,
                (value) =>
                {
                    LoadingScreen.TextSize = (int)value.RoundToNearest(1f);
                    _textSizeLabel.text = LoadingScreen.TextSize.ToString();
                }) as UISlider;
            _textSizeLabel = UILabels.AddLabel(textSizeSlider, textSizeSlider.width + 5f, 0f, LoadingScreen.TextSize.ToString());

            // Overlay transparency.
            UISlider alphaSlider = textGroup.AddSlider(
                Translations.Translate("OVERLAY_ALPHA"),
                0f,
                1f,
                0.1f,
                LoadingScreen.OverlayAlpha,
                (value) =>
                {
                    LoadingScreen.OverlayAlpha = value;
                    _alphaLabel.text = AlphaText;
                }) as UISlider;
            _alphaLabel = UILabels.AddLabel(alphaSlider, alphaSlider.width + 5f, 0f, AlphaText);
        }

        /// <summary>
        /// Gets the text display for the current overlay alpha value.
        /// </summary>
        private string AlphaText => (LoadingScreen.OverlayAlpha * 100f).ToString("N0") + '%';

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

            // Update all mode check states.
            UpdateModeChecks();
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

            // Update all mode check states.
            UpdateModeChecks();
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

            // Update all mode check states.
            UpdateModeChecks();
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
            UpdateModeChecks();
        }

        /// <summary>
        /// Scaling check change handler.
        /// </summary>
        /// <param name="isChecked">New checked status.</param>
        private void FitCheckChanged(bool isChecked)
        {
            // Don't do anything if events are ignored.
            if (ignoreEvents)
            {
                return;
            }

            // Only update if this is being checked.
            if (isChecked)
            {
                BackgroundImage.ImageScaling = ScaleMode.ScaleToFit;
            }

            // Update all scale check states.
            UpdateScaleChecks();
        }

        /// <summary>
        /// Scaling check change handler.
        /// </summary>
        /// <param name="isChecked">New checked status.</param>
        private void CropCheckChanged(bool isChecked)
        {
            // Don't do anything if events are ignored.
            if (ignoreEvents)
            {
                return;
            }

            // Only update if this is being checked.
            if (isChecked)
            {
                BackgroundImage.ImageScaling = ScaleMode.ScaleAndCrop;
            }

            // Update all scale check states.
            UpdateScaleChecks();
        }

        /// <summary>
        /// Scaling check change handler.
        /// </summary>
        /// <param name="isChecked">New checked status.</param>
        private void StretchCheckChanged(bool isChecked)
        {
            // Don't do anything if events are ignored.
            if (ignoreEvents)
            {
                return;
            }

            // Only update if this is being checked.
            if (isChecked)
            {
                BackgroundImage.ImageScaling = ScaleMode.StretchToFill;
            }

            // Update all scale check states.
            UpdateScaleChecks();
        }

        /// <summary>
        /// Updates image mode checkbox states to match current settings.
        /// </summary>
        private void UpdateModeChecks()
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

        /// <summary>
        /// Updates image scaling states to match current settings.
        /// </summary>
        private void UpdateScaleChecks()
        {
            // Suspend events while updating.
            ignoreEvents = true;

            // Set check states.
            _fitImageCheck.isChecked = BackgroundImage.ImageScaling == ScaleMode.ScaleToFit;
            _cropImageCheck.isChecked = BackgroundImage.ImageScaling == ScaleMode.ScaleAndCrop;
            _stretchImageCheck.isChecked = BackgroundImage.ImageScaling == ScaleMode.StretchToFill;

            // Resume event handling.
            ignoreEvents = false;
        }
    }
}