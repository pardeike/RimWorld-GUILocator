using HarmonyLib;
using RimWorld.Planet;
using System;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace GUILocator
{
	public class MethodWrapper
	{
		readonly Type type;
		readonly string name;

		public MethodWrapper(Type type, string name)
		{
			this.type = type;
			this.name = name;
		}

		public T GetDelegate<T>() where T : Delegate
		{
			var method = AccessTools.DeclaredMethod(type, name, typeof(T).GetGenericArguments());
			if (method == null) return default;
			return AccessTools.MethodDelegate<T>(method);
		}
	}

	public class Constructor
	{
		public static T Get<T>() where T : Delegate
		{
			var types = typeof(T).GetGenericArguments();
			var type = types.Last();
			var args = types.Take(types.Count() - 1).ToArray();
			var constructor = AccessTools.DeclaredConstructor(type, args);
			if (constructor == null) return default;
			var dynamic = new DynamicMethod(string.Empty, type, args, type, true);
			var il = dynamic.GetILGenerator();
			for (int i = 0; i < args.Length; i++)
				il.Emit(OpCodes.Ldarg, i);
			il.Emit(OpCodes.Newobj, constructor);
			il.Emit(OpCodes.Ret);
			return (T)dynamic.CreateDelegate(typeof(T));
		}
	}

	public static class Misc
	{
		static readonly Func<string, Action, MenuOptionPriority, Action, Thing, float, Func<Rect, bool>, WorldObject, FloatMenuOption> floatMenuOption1
			 = Constructor.Get<Func<string, Action, MenuOptionPriority, Action, Thing, float, Func<Rect, bool>, WorldObject, FloatMenuOption>>();
		static readonly Func<string, Action, MenuOptionPriority, Action<Rect>, Thing, float, Func<Rect, bool>, WorldObject, bool, FloatMenuOption> floatMenuOption2
			= Constructor.Get<Func<string, Action, MenuOptionPriority, Action<Rect>, Thing, float, Func<Rect, bool>, WorldObject, bool, FloatMenuOption>>();

		public static FloatMenuOption FloatMenuOption(string label, Action action)
		{
			if (floatMenuOption1 != null)
				return floatMenuOption1(label, action, MenuOptionPriority.Default, null, null, 0f, null, null);
			else if (floatMenuOption2 != null)
				return floatMenuOption2(label, action, MenuOptionPriority.Default, null, null, 0f, null, null, true);
			return default;
		}
	}

	public static class Log
	{
		static readonly Action<string, bool> logWarning1;
		static readonly Action<string> logWarning2;

		static readonly Action<string, bool> logError1;
		static readonly Action<string> logError2;

		static readonly Action<string, bool> logErrorOnce1;
		static readonly Action<string> logErrorOnce2;

		static Log()
		{
			var warning = new MethodWrapper(typeof(Verse.Log), "Warning");
			logWarning1 = warning.GetDelegate<Action<string, bool>>();
			logWarning2 = warning.GetDelegate<Action<string>>();

			var error = new MethodWrapper(typeof(Verse.Log), "Error");
			logError1 = error.GetDelegate<Action<string, bool>>();
			logError2 = error.GetDelegate<Action<string>>();

			var errorOnce = new MethodWrapper(typeof(Verse.Log), "Error");
			logErrorOnce1 = errorOnce.GetDelegate<Action<string, bool>>();
			logErrorOnce2 = errorOnce.GetDelegate<Action<string>>();
		}

		public static void Warning(string text)
		{
			if (logWarning1 != null)
				logWarning1(text, false);
			else if (logWarning2 != null)
				logWarning2(text);
		}

		public static void Error(string text)
		{
			if (logError1 != null)
				logError1(text, false);
			else if (logError2 != null)
				logError2(text);
		}

		public static void ErrorOnce(string text)
		{
			if (logErrorOnce1 != null)
				logErrorOnce1(text, false);
			else if (logErrorOnce2 != null)
				logErrorOnce2(text);
		}
	}
}
