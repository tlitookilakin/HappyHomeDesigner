using HappyHomeDesigner.Framework;
using HarmonyLib;
using System.Reflection;

namespace HappyHomeDesigner.Patches
{
	internal class PreciseFurniture
	{
		public static void Apply(HarmonyHelper harmony)
		{
			if (!ModEntry.helper.ModRegistry.IsLoaded("Espy.PreciseFurniture"))
				return;

			ModEntry.monitor.Log("Integrating Precise Furniture...", StardewModdingAPI.LogLevel.Debug);

			if(!ModUtilities.TryFindAssembly("PreciseFurniture", out var asm))
			{
				ModEntry.monitor.Log("Failed to find precise furniture assembly, some things may be weird...", StardewModdingAPI.LogLevel.Warn);
				return;
			}

			bool failed = false;
			var patcher = harmony.Patcher;
			var patch = new HarmonyMethod(typeof(PreciseFurniture), nameof(IgnorePatch));

			if (
				asm.GetType("PreciseFurniture.Framework.Patches.StandardObjects.FishTankFurniturePatch")?
				.GetMethod("GetTankBoundsPostfix", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
				is MethodBase method
			)
			{
				patcher.Patch(method, prefix: patch);
			}
			else
			{
				failed = true;
			}

			if (
				asm.GetType("PreciseFurniture.Framework.Patches.StandardObjects.FurniturePatch")?
				.GetMethod("GetSeatPositionsPrefix", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
				is MethodBase method2
			)
			{
				patcher.Patch(method2, prefix: patch);
			}
			else
			{
				failed = true;
			}

			if (failed)
				ModEntry.monitor.Log("Some patches for Precise Furniture failed, some things may be weird...", StardewModdingAPI.LogLevel.Warn);
			else
				ModEntry.monitor.Log("All patches for Precise Furniture applied.", StardewModdingAPI.LogLevel.Trace);
		}

		private static bool IgnorePatch(out bool __result)
		{
			__result = true;
			return false;
		}
	}
}
