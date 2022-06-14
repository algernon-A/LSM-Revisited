using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LoadingScreenMod
{
	public class DetourUtility<T> : Instance<T>
	{
		internal const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		private readonly List<Detour> detours = new List<Detour>(2);

		protected void init(Type fromType, string fromMethod, Type toType, string toMethod, int args = -1)
		{
			try
			{
				MethodInfo method = GetMethod(fromType, fromMethod, args);
				MethodInfo method2 = GetMethod(toType, toMethod);
				if (method == null)
				{
					Util.DebugPrint(fromType, "reflection failed:", fromMethod);
				}
				else if (method2 == null)
				{
					Util.DebugPrint(toType, "reflection failed:", toMethod);
				}
				else
				{
					detours.Add(new Detour(method, method2));
				}
			}
			catch (Exception exception)
			{
				Util.DebugPrint("Reflection failed in", GetType());
				Debug.LogException(exception);
			}
		}

		protected void init(Type fromType, string fromMethod, string toMethod, int args = -1)
		{
			init(fromType, fromMethod, GetType(), toMethod, args);
		}

		protected void init(Type fromType, string fromMethod, int args = -1)
		{
			init(fromType, fromMethod, GetType(), fromMethod, args);
		}

		internal static MethodInfo GetMethod(Type type, string method, int args = -1)
		{
			if (args >= 0)
			{
				return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Single((MethodInfo m) => m.Name == method && m.GetParameters().Length == args);
			}
			return type.GetMethod(method, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		protected void init(Type fromType, string fromMethod, int args, int argIndex, Type argType)
		{
			try
			{
				MethodInfo method = GetMethod(fromType, fromMethod, args, argIndex, argType);
				MethodInfo method2 = GetMethod(GetType(), fromMethod);
				if (method == null)
				{
					Util.DebugPrint(fromType, "reflection failed:", fromMethod);
				}
				else if (method2 == null)
				{
					Util.DebugPrint(GetType(), "reflection failed:", fromMethod);
				}
				else
				{
					detours.Add(new Detour(method, method2));
				}
			}
			catch (Exception exception)
			{
				Util.DebugPrint("Reflection failed in", GetType());
				Debug.LogException(exception);
			}
		}

		private static MethodInfo GetMethod(Type type, string method, int args, int argIndex, Type argType)
		{
			return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Single((MethodInfo m) => m.Name == method && m.GetParameters().Length == args && m.GetParameters()[argIndex].ParameterType == argType);
		}

		internal void Deploy()
		{
			foreach (Detour detour in detours)
			{
				detour.Deploy();
			}
		}

		internal void Revert()
		{
			foreach (Detour detour in detours)
			{
				detour.Revert();
			}
		}

		internal virtual void Dispose()
		{
			Revert();
			detours.Clear();
			Instance<T>.instance = default(T);
		}
	}
}
