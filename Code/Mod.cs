// <copyright file="Mod.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using AlgernonCommons;
    using AlgernonCommons.Notifications;
    using AlgernonCommons.Patching;
    using AlgernonCommons.Translation;
    using ColossalFramework.UI;
    using ICities;

    /// <summary>
    /// The base mod class for instantiation by the game.
    /// </summary>
    public sealed class Mod : PatcherMod<OptionsPanel, Patcher>, IUserMod
    {
        private readonly string _compatibleVersion = "1.18";

        /// <summary>
        /// Gets the mod's base display name (name only).
        /// </summary>
        public override string BaseName => "Loading Screen Mod Revisited";

        /// <summary>
        /// Gets the mod's unique Harmony identfier.
        /// </summary>
        public override string HarmonyID => "com.github.algernon-A.csl.lsmr";

        /// <summary>
        /// Gets the mod's description for display in the content manager.
        /// </summary>
        public string Description => Translations.Translate("MOD_DESCRIPTION");

        /// <summary>
        /// Called by the game when the mod is enabled.
        /// </summary>
        public override void OnEnabled()
        {
            // Disable mod if version isn't compatible.
            if (!BuildConfig.applicationVersion.StartsWith(_compatibleVersion))
            {
                Logging.Error("invalid game version detected!");

                // Display error message.
                // First, check to see if UIView is ready.
                if (UIView.GetAView() != null)
                {
                    // It's ready - attach the hook now.
                    DisplayVersionError();
                }
                else
                {
                    // Otherwise, queue the hook for when the intro's finished loading.
                    LoadingManager.instance.m_introLoaded += DisplayVersionError;
                }

                // Don't do anything else - no options panel hook, no Harmony patching.
                return;
            }

            // All good - continue as normal.
            base.OnEnabled();
        }

        /// <summary>
        /// Saves settings file.
        /// </summary>
        public override void SaveSettings() => LSMRSettings.Save();

        /// <summary>
        /// Loads settings file.
        /// </summary>
        public override void LoadSettings() => LSMRSettings.Load();

        /// <summary>
        /// Displays a version incompatibility error.
        /// </summary>
        private void DisplayVersionError()
        {
            ListNotification versionErrorNotification = NotificationBase.ShowNotification<ListNotification>();
            versionErrorNotification.AddParas(Translations.Translate("WRONG_VERSION"), Translations.Translate("SHUT_DOWN"), BuildConfig.applicationVersion);
        }
    }
}
