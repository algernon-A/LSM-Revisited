using ColossalFramework.UI;


namespace LoadingScreenModRevisited
{
    /// <summary>
    /// Options panel for setting background loading image options.
    /// </summary>
    internal class ImageOptions : OptionsPanelTab
    {
        // Panel components.
        private UICheckBox defaultCheck, imgurCuratedCheck, imgurRandomCheck, localRandomCheck;

        // Event processing.
        private bool ignoreEvents = false;


        /// <summary>
        /// Adds mod options tab to tabstrip.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to</param>
        /// <param name="tabIndex">Index number of tab</param>
        internal ImageOptions(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = OptionsPanel.AddTab(tabStrip, Translations.Translate("OPTIONS_IMAGE"), tabIndex, true);
            panel.autoLayoutDirection = LayoutDirection.Vertical;

            // Add controls.
            UIHelper helper = new UIHelper(panel);

            // Add curated imgur image check.
            defaultCheck = helper.AddCheckbox(Translations.Translate("DEFAULT_BACKGROUND"),
                BackgroundImage.ImageMode == ImageMode.Standard,
                DefaultCheckChanged) as UICheckBox;

            // Add curated imgur image check.
            imgurCuratedCheck = helper.AddCheckbox(Translations.Translate("IMGUR_CURATED"),
                BackgroundImage.ImageMode == ImageMode.ImgurCurated,
                CuratedCheckChanged) as UICheckBox;
            imgurCuratedCheck.tooltip = Translations.Translate("IMGUR_CURATED_TIP");

            // Add random imgur image check.
            imgurRandomCheck = helper.AddCheckbox(Translations.Translate("IMGUR_TOP"),
                BackgroundImage.ImageMode == ImageMode.ImgurRandom,
                RandomCheckChanged) as UICheckBox;
            imgurRandomCheck.tooltip = Translations.Translate("IMGUR_TOP_TIP");

            // Random local image check.
            localRandomCheck = helper.AddCheckbox(Translations.Translate("LOCAL_IMAGE"),
                BackgroundImage.ImageMode == ImageMode.LocalRandom,
                LocalCheckChanged) as UICheckBox;
            localRandomCheck.tooltip = Translations.Translate("LOCAL_IMAGE_TIP");

            TextField(helper, BackgroundImage.imageDir, (text) =>
            {
                if (text != BackgroundImage.imageDir)
                {
                    BackgroundImage.imageDir = text;
                }
            });
        }


        /// <summary>
        /// Imgur curated backgroung image check change handler.
        /// </summary>
        /// <param name="isChecked">New checked status</param>
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
                BackgroundImage.ImageMode = ImageMode.Standard;
            }

            // Update all check states.
            UpdateChecks();
        }


        /// <summary>
        /// Imgur curated background image check change handler.
        /// </summary>
        /// <param name="isChecked">New checked status</param>
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
                BackgroundImage.ImageMode = ImageMode.ImgurCurated;
            }

            // Update all check states.
            UpdateChecks();
        }


        /// <summary>
        /// Imgur random background image check change handler.
        /// </summary>
        /// <param name="isChecked">New checked status</param>
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
                BackgroundImage.ImageMode = ImageMode.ImgurRandom;
            }

            // Update all check states.
            UpdateChecks();
        }


        /// <summary>
        /// Imgur random background image check change handler.
        /// </summary>
        /// <param name="isChecked">New checked status</param>
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
                BackgroundImage.ImageMode = ImageMode.LocalRandom;
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
            defaultCheck.isChecked = BackgroundImage.ImageMode == ImageMode.Standard;
            imgurCuratedCheck.isChecked = BackgroundImage.ImageMode == ImageMode.ImgurCurated;
            imgurRandomCheck.isChecked = BackgroundImage.ImageMode == ImageMode.ImgurRandom;
            localRandomCheck.isChecked = BackgroundImage.ImageMode == ImageMode.LocalRandom;

            // Resume event handling.
            ignoreEvents = false;
        }
    }
}