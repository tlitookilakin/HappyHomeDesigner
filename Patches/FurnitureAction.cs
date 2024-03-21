﻿using HappyHomeDesigner.Menus;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System.Reflection;

namespace HappyHomeDesigner.Patches
{
	internal class FurnitureAction
	{
		internal static void Apply(Harmony harmony)
		{
			harmony.Patch(
				typeof(Furniture).GetMethod(nameof(Furniture.checkForAction)),
				prefix: new(typeof(FurnitureAction), nameof(CheckAction))
			);

			harmony.Patch(
				typeof(Furniture).GetMethod("loadDescription", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
				postfix: new(typeof(FurnitureAction), nameof(EditDescription))
			);
		}

		private static bool CheckAction(Furniture __instance, ref bool __result)
		{
			if (__instance.ItemId == ModEntry.manifest.UniqueID + "_Catalogue")
			{
				ShowCatalog();
				__result = true;
				return false;
			}

			switch (__instance.ItemId)
			{
				// Furniture catalog
				case "1226":
					if (__instance.heldObject.Value is Furniture sobj && sobj.ItemId is "1308")
						ShowCatalog();
					else
						return true;
					break;

				default:
					return true;
			}

			__result = true;
			return false;
		}

		private static void ShowCatalog()
		{
			if (Catalog.TryShowCatalog("Combined"))
				ModEntry.monitor.Log("Table activated!", LogLevel.Debug);
			else
				ModEntry.monitor.Log("Failed to display UI", LogLevel.Debug);
		}

		private static string EditDescription(string original, Furniture __instance)
		{
			if (ItemRegistry.GetDataOrErrorItem(__instance.ItemId).IsErrorItem)
				return original;

			if (__instance.ItemId == ModEntry.manifest.UniqueID + "_Catalogue")
				return ModEntry.i18n.Get("furniture.Catalog.description");
			return original;
		}
	}
}
