using HappyHomeDesigner.Framework;
using HarmonyLib;
using StardewValley.Objects;
using StardewValley;
using System.Reflection;
using HappyHomeDesigner.Menus;

namespace HappyHomeDesigner.Patches
{
	internal class Misc
	{
		internal static void Apply(Harmony harmony)
		{
			harmony.TryPatch(
				typeof(Furniture).GetMethod("loadDescription", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
				postfix: new(typeof(Misc), nameof(EditDescription))
			);

			harmony.TryPatch(
				typeof(Utility).GetMethod(nameof(Utility.isWithinTileWithLeeway)),
				postfix: new(typeof(Misc), nameof(SetFreePlace))
			);

			harmony.TryPatch(
				typeof(Furniture).GetMethod(nameof(Furniture.IsCloseEnoughToFarmer)),
				postfix: new(typeof(Misc), nameof(SetFreePlace))
			);
		}

		private static string EditDescription(string original, Furniture __instance)
		{
			if (ItemRegistry.GetDataOrErrorItem(__instance.ItemId).IsErrorItem)
				return original;

			return __instance.ItemId switch
			{
				AssetManager.CATALOGUE_ID => ModEntry.i18n.Get("furniture.Catalogue.desc"),
				AssetManager.COLLECTORS_ID => ModEntry.i18n.Get("furniture.CollectorsCatalogue.desc"),
				AssetManager.DELUXE_ID => ModEntry.i18n.Get("furniture.DeluxeCatalogue.desc"),
				_ => original
			};
		}

		private static bool SetFreePlace(bool free_place_allowed)
			=> free_place_allowed || Catalog.MenuVisible();
	}
}
