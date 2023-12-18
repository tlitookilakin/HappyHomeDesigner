using HappyHomeDesigner.Menus;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
				case 1308:
					ShowCatalog(Catalog.AvailableCatalogs.Wallpaper); 
					break;
				case 1226:
					if (__instance.heldObject.Value is StardewValley.Object sobj && sobj.ParentSheetIndex is 1308)
						ShowCatalog(Catalog.AvailableCatalogs.All);
					else
						ShowCatalog(Catalog.AvailableCatalogs.Furniture);
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
				ModEntry.monitor.Log("Failed to display display UI", LogLevel.Debug);
		}
	}
}
