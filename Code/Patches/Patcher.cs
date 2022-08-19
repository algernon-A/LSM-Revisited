// <copyright file="Patcher.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.Reflection;
    using AlgernonCommons;
    using AlgernonCommons.Patching;
    using ColossalFramework.Packaging;
    using HarmonyLib;

    /// <summary>
    /// Class to manage the mod's Harmony patches.
    /// </summary>
    public class Patcher : PatcherBase
    {
        // Flags.
        private bool _customAnimLoaderPatched = false;

        /// <summary>
        /// Patches Custom Animation Loader to work with this mod.
        /// Actually, it applies CAL's LSM Postfix patch to this mod's AssetDeserializer.DeserializeGameObject method.
        /// Doing this work for CAL.
        /// </summary>
        public void PatchCustomAnimationLoader()
        {
            // Don't do anything if already patched.
            if (!_customAnimLoaderPatched)
            {
                // Attempt to reflect CAL's patch method.
                MethodInfo calPatch = Type.GetType("CustomAnimationLoader.Patches.PackageAssetPatch,CustomAnimationLoader")?.GetMethod("Postfix", BindingFlags.Public | BindingFlags.Static);
                if (calPatch != null)
                {
                    // Found CAL's patch method - apply it here.
                    Logging.KeyMessage("patching Custom Animation Loader");

                    Harmony harmonyInstance = new Harmony(HarmonyID);
                    harmonyInstance.Patch(typeof(AssetDeserializer).GetMethod("DeserializeGameObject", BindingFlags.Instance | BindingFlags.NonPublic), postfix: new HarmonyMethod(calPatch));

                    _customAnimLoaderPatched = true;
                }
                else
                {
                    Logging.Message("Custom Animation Loader not found");
                }
            }
            else
            {
                Logging.Message("Custom Animation Loader already patched");
            }
        }

        /// <summary>
        /// Peforms any additional actions (such as custom patching) after PatchAll is called.
        /// </summary>
        /// <param name="harmonyInstance">Haromny instance for patching.</param>
        protected override void OnPatchAll(Harmony harmonyInstance) => ApplyAssetSerializerReverses(harmonyInstance);

        /// <summary>
        /// Applies reverse patches to access methods of private type ColossalFramework.Packaging.AssetSerializer.
        /// </summary>
        /// <param name="harmonyInstance">Haromny instance for patching.</param>
        private void ApplyAssetSerializerReverses(Harmony harmonyInstance)
        {
            // No try...catch here, if something goes wrong we want to have the unmanaged exception (at least for now).
            // Any failure needs to be immediately obvious to the user.
            // TODO: More graceful disabling and user notification.

            // Reflect AssetSerializer target methods.
            Type assetSerializer = Type.GetType("ColossalFramework.Packaging.AssetSerializer,ColossalManaged");
            MethodInfo deserializeHeader = assetSerializer.GetMethod("DeserializeHeader", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Type).MakeByRefType(), typeof(PackageReader) }, null);
            MethodInfo deserializeHeaderName = assetSerializer.GetMethod("DeserializeHeader", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Type).MakeByRefType(), typeof(string).MakeByRefType(), typeof(PackageReader) }, null);

            // Reverse patch.
            ReversePatcher reversePatcher = harmonyInstance.CreateReversePatcher(deserializeHeader, new HarmonyMethod(typeof(AssetDeserializer).GetMethod(nameof(AssetDeserializer.DeserializeHeader), BindingFlags.Static | BindingFlags.NonPublic)));
            ReversePatcher reversePatcherName = harmonyInstance.CreateReversePatcher(deserializeHeaderName, new HarmonyMethod(typeof(AssetDeserializer).GetMethod(nameof(AssetDeserializer.DeserializeHeaderName), BindingFlags.Static | BindingFlags.NonPublic)));
            reversePatcher.Patch();
            reversePatcherName.Patch();
        }
    }
}