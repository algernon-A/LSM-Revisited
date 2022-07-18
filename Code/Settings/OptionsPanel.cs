using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using System;
using UnityEngine;


namespace LoadingScreenModRevisited
{
    /// <summary>
    /// Class to handle the mod's options panel.
    /// </summary>
    internal static class OptionsPanel
    {
        // Parent UI panel reference.
        internal static UIScrollablePanel optionsPanel;
        private static UIPanel gameOptionsPanel;

        // Instance references.
        private static GameObject optionsGameObject;


        /// <summary>
        /// Options panel setup.
        /// </summary>
        /// <param name="helper">UIHelperBase parent</param>
        internal static void Setup(UIHelperBase helper)
        {
            // Set up tab strip and containers.
            optionsPanel = ((UIHelper)helper).self as UIScrollablePanel;
            optionsPanel.autoLayout = false;
        }


        /// <summary>
        /// Attaches an event hook to options panel visibility, to enable/disable mod hokey when the panel is open.
        /// </summary>
        internal static void OptionsEventHook()
        {
            // Get options panel instance.
            gameOptionsPanel = UIView.library.Get<UIPanel>("OptionsPanel");

            if (gameOptionsPanel == null)
            {
                Logging.Error("couldn't find OptionsPanel");
            }
            else
            {
                // Simple event hook to create/destroy GameObject based on appropriate visibility.
                gameOptionsPanel.eventVisibilityChanged += (control, isVisible) =>
                {
                    // Create/destroy based on whether or not we're now visible.
                    if (isVisible)
                    {
                        Create();
                    }
                    else
                    {
                        Close();
                    }
                };

                // Recreate panel on system locale change.
                LocaleManager.eventLocaleChanged += LocaleChanged;
            }
        }


        /// <summary>
        /// Refreshes the options panel (destroys and rebuilds) on a locale change when the options panel is open.
        /// </summary>
        internal static void LocaleChanged()
        {
            if (gameOptionsPanel != null && gameOptionsPanel.isVisible)
            {
                Close();
                Create();
            }
        }


        /// <summary>
        /// Adds a tab to a UI tabstrip.
        /// </summary>
        /// <param name="tabStrip">UIT tabstrip to add to</param>
        /// <param name="tabName">Name of this tab</param>
        /// <param name="tabIndex">Index number of this tab</param>
        /// <param name="autoLayout">Autolayout</param>
        /// <returns>UIHelper instance for the new tab panel</returns>
        internal static UIPanel AddTab(UITabstrip tabStrip, string tabName, int tabIndex, bool autoLayout)
        {
            // Create tab.
            UIButton tabButton = tabStrip.AddTab(tabName);

            // Sprites.
            tabButton.normalBgSprite = "SubBarButtonBase";
            tabButton.disabledBgSprite = "SubBarButtonBaseDisabled";
            tabButton.focusedBgSprite = "SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = "SubBarButtonBaseHovered";
            tabButton.pressedBgSprite = "SubBarButtonBasePressed";

            // Tooltip.
            tabButton.tooltip = tabName;

            tabStrip.selectedIndex = tabIndex;

            // Force width.
            tabButton.width = 200;

            // Get tab root panel.
            UIPanel rootPanel = tabStrip.tabContainer.components[tabIndex] as UIPanel;

            // Autolayout.
            rootPanel.autoLayout = autoLayout;

            if (autoLayout)
            {
                rootPanel.autoLayoutDirection = LayoutDirection.Vertical;
                rootPanel.autoLayoutPadding.top = 5;
                rootPanel.autoLayoutPadding.left = 10;
            }

            return rootPanel;
        }


        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        private static void Create()
        {
            try
            {
                // If no instance already set, create one.
                if (optionsGameObject == null)
                {
                    // Give it a unique name for easy finding with ModTools.
                    optionsGameObject = new GameObject("LSMROptionsPanel");

                    // Attach to game options panel.
                    optionsGameObject.transform.parent = optionsPanel.transform;

                    // Create a base panel attached to our game object, perfectly overlaying the game options panel.
                    UIPanel basePanel = optionsGameObject.AddComponent<UIPanel>();
                    basePanel.width = optionsPanel.width - 10f;
                    basePanel.height = 725f;
                    basePanel.clipChildren = false;

                    // Needed to ensure position is consistent if we regenerate after initial opening (e.g. on language change).
                    basePanel.relativePosition = new Vector2(10f, 10f);

                    // Add tabstrip.
                    UITabstrip tabStrip = basePanel.AddUIComponent<UITabstrip>();
                    tabStrip.relativePosition = new Vector3(0, 0);
                    tabStrip.width = basePanel.width;
                    tabStrip.height = basePanel.height;
                    tabStrip.clipChildren = false;

                    // Tab container (the panels underneath each tab).
                    UITabContainer tabContainer = basePanel.AddUIComponent<UITabContainer>();
                    tabContainer.relativePosition = new Vector3(0, 30f);
                    tabContainer.width = tabStrip.width;
                    tabContainer.height = tabStrip.height;
                    tabContainer.clipChildren = false;
                    tabStrip.tabPages = tabContainer;

                    // Add tabs and panels.
                    new GeneralOptions(tabStrip, 0);
                    new ReportingOptions(tabStrip, 1);
                    new ImageOptions(tabStrip, 2);
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception creating options panel");
            }
        }


        /// <summary>
        /// Closes the panel by destroying the object (removing any ongoing UI overhead).
        /// </summary>
        private static void Close()
        {
            // Save settings first.
            LoadingScreenMod.Settings.settings.Save();

            // We're no longer visible - destroy our game object.
            if (optionsGameObject != null)
            {
                GameObject.Destroy(optionsGameObject);
                optionsGameObject = null;
            }
        }
    }
}