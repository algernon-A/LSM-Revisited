// <copyright file="Mod.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using AlgernonCommons.Patching;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using ICities;

    /// <summary>
    /// The base mod class for instantiation by the game.
    /// </summary>
    public sealed class Mod : PatcherMod, IUserMod
    {
        /// <summary>
        /// Gets the mod's base display name (name only).
        /// </summary>
        public override string BaseName => "LSM Revisited";

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
            base.OnEnabled();

            // Attaching options panel event hook - check to see if UIView is ready.
            if (UIView.GetAView() != null)
            {
                // It's ready - attach the hook now.
                OptionsPanelManager<OptionsPanel>.OptionsEventHook();
            }
            else
            {
                // Otherwise, queue the hook for when the intro's finished loading.
                LoadingManager.instance.m_introLoaded += OptionsPanelManager<OptionsPanel>.OptionsEventHook;
            }
        }

        /// <summary>
        /// Called by the game when the mod is disabled.
        /// </summary>
        public override void OnDisabled()
        {
            base.OnDisabled();

            // Remove legacy settings helper.
            LoadingScreenMod.Settings.settings.helper = null;
        }

        /// <summary>
        /// Called by the game when the mod options panel is setup.
        /// </summary>
        /// <param name="helper">UI helper instance.</param>
        public void OnSettingsUI(UIHelperBase helper)
        {
            // Create options panel.
            OptionsPanelManager<OptionsPanel>.Setup(helper);
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
        /// Apply Harmony patches.
        /// </summary>
        protected override void ApplyPatches() => Patcher.Instance.PatchAll();

        /// <summary>
        /// Remove Harmony patches.
        /// </summary>
        protected override void RemovePatches() => Patcher.Instance.UnpatchAll();
    }
}
