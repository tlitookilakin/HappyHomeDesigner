using HappyHomeDesigner.Framework;
using HarmonyLib;
using StardewValley.Objects;
using StardewValley;
using HappyHomeDesigner.Menus;
using System;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Reflection.Emit;
using SObject = StardewValley.Object;
using StarModGen.Utils;

namespace HappyHomeDesigner.Patches
{
	internal class Misc
	{
		internal static void Apply(HarmonyHelper helper)
		{
			helper
				.With<Furniture>("loadDescription").Postfix(EditDescription)
				.With(nameof(Furniture.IsCloseEnoughToFarmer)).Postfix(SetFreePlace)
				.With<Utility>(nameof(Utility.isWithinTileWithLeeway)).Postfix(SetFreePlace)
				.With(nameof(Utility.SortAllFurnitures)).Prefix(SortErrorFurniture)
				.With<FurnitureDataDefinition>(nameof(FurnitureDataDefinition.CreateItem)).Finalizer(ReplaceInvalidFurniture)
				.With<Toolbar>(nameof(Toolbar.draw)).Prefix(SkipToolbar)
				.With(nameof(Toolbar.receiveLeftClick)).Prefix(SkipToolbar)
				.With(nameof(Toolbar.receiveRightClick)).Prefix(SkipToolbar);

			if (!ModEntry.ANDROID)
				helper.With<Game1>(nameof(Game1.drawMouseCursor)).Transpiler(DisableHeldItemDraw);
		}

		private static IEnumerable<CodeInstruction> DisableHeldItemDraw(IEnumerable<CodeInstruction> source, ILGenerator gen)
		{
			var il = new CodeMatcher(source, gen);
			Label? jumpTarget = null;

			il
				.MatchStartForward(
					new(OpCodes.Call, typeof(Game1).GetProperty(nameof(Game1.currentLocation))!.GetMethod),
					new(OpCodes.Callvirt, typeof(SObject).GetMethod(nameof(SObject.drawPlacementBounds)))
				);

			il
				.MatchEndBackwards(
					new CodeMatch(c => c.Branches(out jumpTarget))
				)
				.Advance(1)
				.InsertAndAdvance(
					new(OpCodes.Call, typeof(Misc).GetMethod(nameof(ShouldSkipItemDraw))),
					new(OpCodes.Brtrue, jumpTarget)
				);

			return il.InstructionEnumeration();
		}

		public static bool ShouldSkipItemDraw()
			=> Catalog.ActiveMenu.Value is Catalog c && c.HideActiveObject;

		private static bool SkipToolbar()
			=> !(Catalog.ActiveMenu.Value is Catalog c && c.InventoryOpen);

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
			=> (Catalog.ActiveMenu.Value is not Catalog c) ? 
			free_place_allowed : !c.HideActiveObject;

		private static Exception? ReplaceInvalidFurniture(Exception __exception, ParsedItemData data, ref Item __result, FurnitureDataDefinition __instance)
		{
			if (__exception is null || data.IsErrorItem)
				return null;

			var modName = data.ItemId.TryGetModInfo(out var mod) ? mod.Manifest.Name : "the mod that adds that furniture";

			ModEntry.monitor.Log(
				$"Furniture item {data.ItemId} is invalid! It could not be instantiated, and may cause crashes!\nThis is an issue with " +
				$"{modName}! Report it to that mod, not to Happy Home Designer!\nError: {__exception.Message}",
				StardewModdingAPI.LogLevel.Error
			);
			__result = __instance.CreateItem(__instance.GetErrorData(data.ItemId));
			return null;
		}

		private static bool SortErrorFurniture(Furniture a, Furniture b, ref int __result)
		{
			// fix error items not sorting
			if (a == null || a.Name == "ErrorItem" || b == null || b.Name == "ErrorItem")
			{
				__result = 0;
				return false;
			}

			// fix vanilla sort crash
			if (a.QualifiedItemId is "(F)1226" or "(F)1308" && b.QualifiedItemId is "(F)1226" or "(F)1308")
			{
				__result = a.ItemId.CompareTo(b.ItemId);
				return false;
			}

			return true;
		}
	}
}
