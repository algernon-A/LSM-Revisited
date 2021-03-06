using System;
using System.Text;
using System.Reflection;
using ColossalFramework.Packaging;
using HarmonyLib;
using CitiesHarmony.API;


namespace LoadingScreenModRevisited
{
    /// <summary>
    /// Class to manage the mod's Harmony patches.
    /// </summary>
    public static class Patcher
    {
        // Unique harmony identifier.
        private const string harmonyID = "com.github.algernon-A.csl.lsmr";

        // Flags.
        internal static bool Patched => _patched;
        private static bool _patched = false;
        private static bool _customAnimLoaderPatched = false;


        /// <summary>
        /// Apply all Harmony patches.
        /// </summary>
        public static void PatchAll()
        {
            // Don't do anything if already patched.
            if (!_patched)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage("deploying Harmony patches");

                    // Apply all annotated patches and update flag.
                    Harmony harmonyInstance = new Harmony(harmonyID);
                    harmonyInstance.PatchAll();

                    // Apply asset serializer reverse patches.
                    ApplyAssetSerializerReverses();

                    _patched = true;
                }
                else
                {
                    Logging.Error("Harmony not ready");
                }
            }
        }


        /// <summary>
        /// Remove all Harmony patches.
        /// </summary>
        public static void UnpatchAll()
        {
            // Only unapply if patches appplied.
            if (_patched)
            {
                Logging.KeyMessage("reverting Harmony patches");

                // Unapply patches, but only with our HarmonyID.
                Harmony harmonyInstance = new Harmony(harmonyID);
                harmonyInstance.UnpatchAll(harmonyID);
                _patched = false;
            }
        }


        /// <summary>
        /// Applies a Harmony prefix to the specified method.
        /// </summary>
        /// <param name="target">Target method</param>
        /// <param name="patch">Harmony Prefix patch</param>
        public static void PrefixMethod(MethodInfo target, MethodInfo patch)
        {
            Harmony harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(target, prefix: new HarmonyMethod(patch));

            Logging.Message("patched ", PrintMethod(target), " to ", PrintMethod(patch));
        }


        /// <summary>
        /// Applies a Harmony prefix to the given type and method name, with a patch of the same name from a different type.
        /// </summary>
        /// <param name="targetType">Target type to patch</param>
        /// <param name="patchType">Type containing patch method</param>
        /// <param name="methodName">Method name</param>
        public static void PrefixMethod(Type targetType, Type patchType, string methodName)
        {
            PrefixMethod(targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance),
                patchType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
        }


        /// <summary>
        /// Reverts a Harmony ptach to the specified method.
        /// </summary>
        /// <param name="target">Target method</param>
        /// <param name="patch">Patch to revert</param>
        public static void UnpatchMethod(MethodInfo target, MethodInfo patch)
        {
            Harmony harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Unpatch(target, patch);
        }


        /// <summary>
        /// Reverts a Harmony prefix to the given type and method name, with a patch of the same name from a different type.
        /// </summary>
        /// <param name="targetType">Target type to patch</param>
        /// <param name="patchType">Type containing patch method</param>
        /// <param name="methodName">Method name</param>
        public static void UnpatchMethod(Type targetType, Type patchType, string methodName)
        {
            UnpatchMethod(targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance),
                patchType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
        }


        /// <summary>
        /// Patches Custom Animation Loader to work with this mod.
        /// Actually, it applies CAL's LSM Postfix patch to this mod's AssetDeserializer.DeserializeGameObject method.
        /// Doing this work for CAL.
        /// </summary>
        public static void PatchCustomAnimationLoader()
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

                    Harmony harmonyInstance = new Harmony(harmonyID);
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
        /// Applies reverse patches to access methods of private type ColossalFramework.Packaging.AssetSerializer.
        /// </summary>
        private static void ApplyAssetSerializerReverses()
        {
            // No try...catch here, if something goes wrong we want to have the unmanaged exception (at least for now).
            // Any failure needs to be immediately obvious to the user.
            // TODO: More graceful disabling and user notification.

            // Reflect AssetSerializer target methods.
            Type assetSerializer = Type.GetType("ColossalFramework.Packaging.AssetSerializer,ColossalManaged");
            MethodInfo deserializeHeader = assetSerializer.GetMethod("DeserializeHeader", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Type).MakeByRefType(), typeof(PackageReader) }, null);
            MethodInfo deserializeHeaderName = assetSerializer.GetMethod("DeserializeHeader", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Type).MakeByRefType(), typeof(string).MakeByRefType(), typeof(PackageReader) }, null);

            // Reverse patch.
            Harmony harmonyInstance = new Harmony(harmonyID);
            ReversePatcher reversePatcher = harmonyInstance.CreateReversePatcher(deserializeHeader, new HarmonyMethod(typeof(AssetDeserializer).GetMethod(nameof(AssetDeserializer.DeserializeHeader), BindingFlags.Static | BindingFlags.NonPublic)));
            ReversePatcher reversePatcherName = harmonyInstance.CreateReversePatcher(deserializeHeaderName, new HarmonyMethod(typeof(AssetDeserializer).GetMethod(nameof(AssetDeserializer.DeserializeHeaderName), BindingFlags.Static | BindingFlags.NonPublic)));
            reversePatcher.Patch();
            reversePatcherName.Patch();
        }


        /// <summary>
        /// Coverts MethodInfo data to string.
        /// </summary>
        /// <param name="method">MethodInfo to log</param>
        /// <returns>MethodInfo data as human-readable string.</returns>
        private static string PrintMethod(MethodInfo method)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(method.DeclaringType);
            sb.Append(".");
            sb.Append(method.Name);
            sb.Append("(");
            bool firstParam = true;
            foreach (ParameterInfo param in method.GetParameters())
            {
                // Separate by comma and space for everything after the first parameter.
                if (firstParam)
                {
                    firstParam = false;
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(param.ParameterType.Name);
                sb.Append(" ");
                sb.Append(param.Name);
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
}