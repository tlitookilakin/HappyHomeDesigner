﻿using HappyHomeDesigner.Menus;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Objects;
using System;

namespace HappyHomeDesigner.Patches
{
	internal class FurnitureAction
	{
		internal static void Apply(Harmony harmony)
		{
			harmony.Patch(typeof(Furniture).GetMethod(nameof(Furniture.checkForAction)),
				prefix: new(typeof(FurnitureAction), nameof(CheckAction)));
		}

		private static bool CheckAction(Furniture __instance, ref bool __result)
		{
			if (__instance.Name.Contains("HappyHomeDesigner"))
			{
				ShowCatalog(Catalog.AvailableCatalogs.All);
				__result = true;
				return false;
			}

			switch (__instance.ParentSheetIndex)
			{
				// Wallpaper catalog
				case 1308:
					if (ModEntry.config.ReplaceWallpaperCatalog)
						ShowCatalog(Catalog.AvailableCatalogs.Wallpaper);
					else
						return true;
					break;

				// Furniture catalog
				case 1226:
					if (__instance.heldObject.Value is StardewValley.Object sobj && sobj.ParentSheetIndex is 1308)
						ShowCatalog(Catalog.AvailableCatalogs.All);
					else if (ModEntry.config.ReplaceFurnitureCatalog)
						ShowCatalog(Catalog.AvailableCatalogs.Furniture);
					else
						return true;
					break;

				default:
					return true;
			}

			__result = true;
			return false;
		}

		private static void ShowCatalog(Catalog.AvailableCatalogs available)
		{
			if (Catalog.TryShowCatalog(available))
				ModEntry.monitor.Log("Table activated!", LogLevel.Debug);
			else
				ModEntry.monitor.Log("Failed to display UI", LogLevel.Debug);
		}
	}
}
