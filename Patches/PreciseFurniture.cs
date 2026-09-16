using HappyHomeDesigner.Framework;
using HarmonyLib;
using StardewModdingAPI;
using System;
using System.Reflection;

namespace HappyHomeDesigner.Patches
{
	internal class PreciseFurniture
	{
		public static void Apply(HarmonyHelper harmony)
		{
			if (!ModEntry.helper.ModRegistry.TryGetMod("Espy.PreciseFurniture", out var mod))
				return;

			if (!TryPatch(harmony, mod.Manifest))
				ModEntry.monitor.Log("Some patches for Precise Furniture failed, some things may be weird...", StardewModdingAPI.LogLevel.Warn);
			else
				ModEntry.monitor.Log("All patches for Precise Furniture applied.", StardewModdingAPI.LogLevel.Trace);
		}

		private static bool TryPatch(HarmonyHelper harmony, IManifest mod)
		{
			if (mod.TryGetType("PreciseFurniture.Framework.Patches.StandardObjects.FishTankFurniturePatch", out Type target))
				harmony.With(target, "GetTankBoundsPostfix").Prefix(IgnorePostfix);
			else
				return false;

			if (mod.TryGetType("PreciseFurniture.Framework.Patches.StandardObjects.FurniturePatch", out target))
				harmony.With(target, "GetSeatPositionsPrefix").Prefix(IgnorePrefix);
			else
				return false;

			return true;
		}

		private static bool IgnorePrefix(out bool __result)
		{
			__result = true;
			return false;
		}

		private static bool IgnorePostfix()
		{
			return false;
		}
	}
}
