using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Menus;
using HarmonyLib;
using System;
using System.Reflection;

namespace HappyHomeDesigner.Patches
{
	internal static class ManaBars
	{
		public static void Apply(HarmonyHelper harmony)
		{
			var patch = new HarmonyMethod(typeof(ManaBars), nameof(SkipIfHidden));

			if (ModEntry.helper.ModRegistry.IsLoaded("Zexu2K.MagicStardew.C") && GetMethod("MagicStardew", "MagicStardew.ManaBar", "OnRenderedHud") is MethodInfo m)
				harmony.Patcher.Patch(m, patch);

			if (ModEntry.helper.ModRegistry.IsLoaded("moonslime.ManaBarAPI") && GetMethod("ManaBarAPI", "WizardryManaBar.Core.Events", "OnRenderedHud") is MethodInfo m2)
				harmony.Patcher.Patch(m2, patch);
		}

		private static MethodInfo GetMethod(string asm, string type, string method)
		{
			var target = Type.GetType($"{type}, {asm}")?.GetMethod(method, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			if (target is null)
				ModEntry.monitor.Log($"Failed to patch {asm}:{type}:{method}, could not find.");

			return target;
		}

		private static bool SkipIfHidden()
		{
			return Catalog.ActiveMenu.Value == null;
		}
	}
}
