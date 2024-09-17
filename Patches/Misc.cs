using HappyHomeDesigner.Framework;
using HarmonyLib;
using StardewValley.Objects;
using StardewValley;
using System.Reflection;
using HappyHomeDesigner.Menus;
using Microsoft.Xna.Framework;
using SObject = StardewValley.Object;
using Netcode;
using System.Linq;
using System;
using StardewValley.ItemTypeDefinitions;

namespace HappyHomeDesigner.Patches
{
	internal class Misc
	{
		const string UNIQUE_ITEM_FLAG = ModEntry.MOD_ID + "_UNIQUE_ITEM";

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
				typeof(GameLocation).GetMethod(nameof(GameLocation.LowPriorityLeftClick)),
				postfix: new(typeof(Misc), nameof(TryPickupCraftable))
			);

			harmony.TryPatch(
				typeof(FurnitureDataDefinition).GetMethod(nameof(FurnitureDataDefinition.CreateItem)),
				finalizer: new(typeof(Misc), nameof(ReplaceInvalidFurniture))
			);

			harmony.TryPatch(
				typeof(Utility).GetMethod(nameof(Utility.SortAllFurnitures)),
				prefix: new(typeof(Misc), nameof(SortErrorFurniture))
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

		private static bool TryPickupCraftable(bool handled, int x, int y, Farmer who)
		{
			if (handled)
				return true;

			if (Game1.activeClickableMenu != null || !Catalog.MenuVisible())
				return false;

			var where = who.currentLocation;
			Vector2 tile = new(x / 64, y / 64);

			if (where.Objects.TryGetValue(tile, out var obj) && obj.CanBeGrabbed && !obj.isDebrisOrForage())
			{
				if (TryPickupObject(obj, out var item))
				{
					if (who.addItemToInventoryBool(item, true))
					{
						Game1.playSound("coin");
						obj.performRemoveAction();
						where.Objects.Remove(tile);

						return true;
					}
				}
			}

			return false;
		}

		private static bool TryPickupObject(SObject obj, out SObject result)
		{
			result = null;
			
			if (obj is Chest chest)
			{
				if (chest.GetMutex().IsLocked())
					return false;

				var newchest = chest.getOne() as Chest;
				if (chest.Items.Count > 0 || chest.heldObject.Value is not null)
				{
					var items = chest.Items.ToList();
					chest.Items.Clear();
					newchest.Items.AddRange(items);
					Swap(chest.heldObject, newchest.heldObject);
					newchest.modData[UNIQUE_ITEM_FLAG] = "T";
				}
				result = newchest;
				return true;
			}

			if (obj is Sign sign)
			{
				var newSign = obj.getOne() as Sign;
				Swap(sign.displayItem, newSign.displayItem);
				return true;
			}

			if (obj is Mannequin mann)
			{
				if (mann.boots.Value != null || mann.pants.Value != null || mann.shirt.Value != null || mann.hat.Value != null)
				{
					var newMann = mann.getOne() as Mannequin;
					Swap(mann.heldObject, newMann.heldObject);
					Swap(mann.boots, newMann.boots);
					Swap(mann.pants, newMann.pants);
					Swap(mann.shirt, newMann.shirt);
					Swap(mann.hat, newMann.hat);
					result = newMann;
					return true;
				}
			}

			if (obj.heldObject.Value is SObject ho)
			{
				if (ho is Chest heldChest && heldChest.GetMutex().IsLocked())
					return false;

				if (obj.HasContextTag("is_machine") && obj.HasContextTag("machine_input") && !obj.readyForHarvest.Value)
					return false;

				result = obj.getOne() as SObject;
				obj.heldObject.Value = null;
				result.heldObject.Value = ho;
				result.modData[UNIQUE_ITEM_FLAG] = "T";
				return true;
			}

			result = obj.getOne() as SObject;
			return true;
		}

		private static bool CanPickupChest(Chest chest)
			=> chest != null && chest.isEmpty() && !chest.GetMutex().IsLocked();

		private static void Swap<T>(NetRef<T> from, NetRef<T> to) where T : class, INetObject<INetSerializable>
		{
			T held = from.Value;
			from.Value = null;
			to.Value = held;
		}

		private static Exception ReplaceInvalidFurniture(Exception __exception, ParsedItemData data, ref Item __result, FurnitureDataDefinition __instance)
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
