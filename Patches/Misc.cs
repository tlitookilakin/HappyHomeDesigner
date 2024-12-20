﻿using HappyHomeDesigner.Framework;
using HarmonyLib;
using StardewValley.Objects;
using StardewValley;
using System.Reflection;
using HappyHomeDesigner.Menus;
using System;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Reflection.Emit;
using SObject = StardewValley.Object;

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

			harmony.TryPatch(
				typeof(FurnitureDataDefinition).GetMethod(nameof(FurnitureDataDefinition.CreateItem)),
				finalizer: new(typeof(Misc), nameof(ReplaceInvalidFurniture))
			);

			harmony.TryPatch(
				typeof(Utility).GetMethod(nameof(Utility.SortAllFurnitures)),
				prefix: new(typeof(Misc), nameof(SortErrorFurniture))
			);

			harmony.TryPatch(
				typeof(Toolbar).GetMethod(nameof(Toolbar.draw), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public),
				prefix: new(typeof(Misc), nameof(SkipToolbar))
			);

			harmony.TryPatch(
				typeof(Toolbar).GetMethod(nameof(Toolbar.receiveLeftClick)),
				prefix: new(typeof(Misc), nameof(SkipToolbar))
			);

			harmony.TryPatch(
				typeof(Toolbar).GetMethod(nameof(Toolbar.receiveRightClick)),
				prefix: new(typeof(Misc), nameof(SkipToolbar))
			);

			harmony.TryPatch(
				typeof(Toolbar).GetMethod(nameof(Toolbar.receiveRightClick)),
				prefix: new(typeof(Misc), nameof(SkipToolbar))
			);

			harmony.TryPatch(
				typeof(Game1).GetMethod(nameof(Game1.drawMouseCursor)),
				transpiler: new(typeof(Misc), nameof(DisableHeldItemDraw))
			);
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

			il.MatchEndBackwards(
				new CodeMatch(c => c.Branches(out jumpTarget))
			);
				il.Advance(1)
				.InsertAndAdvance(
					new(OpCodes.Call, typeof(Misc).GetMethod(nameof(ShouldSkipItemDraw))),
					new(OpCodes.Brtrue, jumpTarget)
				);

			return il.InstructionEnumeration();
		}

		public static bool ShouldSkipItemDraw()
			=> Catalog.ActiveMenu.Value is Catalog c && c.HideActiveObject;

		private static bool SkipToolbar(Toolbar __instance)
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
			if (a == null || a.Name == "ErrorItem" || b == null || b.Name == "ErrorItem")
			{
				__result = 0;
				return false;
			}
			return true;
		}
	}
}
