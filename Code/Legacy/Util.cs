using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LoadingScreenMod
{
	public static class Util
	{
		public static void DebugPrint(params object[] args)
		{
			Console.WriteLine("[LSM] " + " ".OnJoin(args));
		}

		public static string OnJoin(this string delim, IEnumerable<object> args)
		{
			return string.Join(delim, args.Select((object o) => o?.ToString() ?? "null").ToArray());
		}

		internal static void InvokeVoid(object instance, string method)
		{
			instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, null);
		}

		internal static object Invoke(object instance, string method)
		{
			return instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, null);
		}

		internal static void InvokeVoid(object instance, string method, params object[] args)
		{
			instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, args);
		}

		internal static object Invoke(object instance, string method, params object[] args)
		{
			return instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, args);
		}

		internal static object Get(object instance, string field)
		{
			return instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
		}

		internal static object GetStatic(Type type, string field)
		{
			return type.GetField(field, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		}

		internal static void Set(object instance, string field, object value)
		{
			instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
		}

		internal static void Set(object instance, string field, object value, BindingFlags flags)
		{
			instance.GetType().GetField(field, flags).SetValue(instance, value);
		}

		internal static string GetFileName(string fileBody, string extension, bool useReportDate)
		{
			if (useReportDate)
			{
				return Path.Combine(GetSavePath(), fileBody + string.Format("-{0:yyyy-MM-dd_HH-mm-ss}." + extension, DateTime.Now));
			}
			return Path.Combine(GetSavePath(), fileBody + "." + extension);
		}

		internal static string GetSavePath()
		{
			string text = Settings.settings.reportDir?.Trim();
			if (string.IsNullOrEmpty(text))
			{
				text = Settings.DefaultSavePath;
			}
			try
			{
				if (!Directory.Exists(text))
				{
					Directory.CreateDirectory(text);
				}
				return text;
			}
			catch (Exception)
			{
				DebugPrint("Cannot create directory:", text);
			}
			return Settings.DefaultSavePath;
		}

		public static List<T> ToList<T>(this T[] array, int count)
		{
			List<T> list = new List<T>(count + 8);
			for (int i = 0; i < count; i++)
			{
				list.Add(array[i]);
			}
			return list;
		}

		public static Dictionary<string, int> GetEnumMap(Type enumType)
		{
			Array values = Enum.GetValues(enumType);
			Dictionary<string, int> dictionary = new Dictionary<string, int>(values.Length);
			foreach (object item in values)
			{
				dictionary[item.ToString().ToUpperInvariant()] = (int)item;
			}
			return dictionary;
		}
	}
}
