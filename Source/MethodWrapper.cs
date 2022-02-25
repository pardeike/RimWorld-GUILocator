using HarmonyLib;
using System;

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
}
