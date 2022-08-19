using System;
using System.Reflection;
using AlgernonCommons.Patching;
using LoadingScreenModRevisited;
using UnityEngine;

namespace LoadingScreenMod
{
    internal class Detour
    {
        private readonly MethodInfo from;

        private readonly MethodInfo to;

        private bool deployed;

        internal Detour(MethodInfo from, MethodInfo to)
        {
            this.from = from;
            this.to = to;
        }

        internal void Deploy()
        {
            try
            {
                if (!deployed)
                {
                    PatcherManager<Patcher>.Instance.PrefixMethod(from, to);
                }
                deployed = true;
            }
            catch (Exception exception)
            {
                Util.DebugPrint("Detour of", from.Name, "->", to.Name, "failed");
                Debug.LogException(exception);
            }
        }

        internal void Revert()
        {
            try
            {
                if (deployed)
                {
                    PatcherManager<Patcher>.Instance.UnpatchMethod(from, to);
                }
                deployed = false;
            }
            catch (Exception exception)
            {
                Util.DebugPrint("Revert of", from.Name, "failed");
                Debug.LogException(exception);
            }
        }
    }
}
