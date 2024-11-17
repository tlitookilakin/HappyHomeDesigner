using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Menus;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using SObject = StardewValley.Object;

namespace HappyHomeDesigner.Patches
{
	internal static class CraftablePlacement
	{
		const string UNIQUE_ITEM_FLAG = ModEntry.MOD_ID + "_UNIQUE_ITEM";
		private static Action<Item, Item>? getOneFrom;

		internal static void Apply(Harmony harmony)
		{

			harmony.TryPatch(
				typeof(GameLocation).GetMethod(nameof(GameLocation.LowPriorityLeftClick)),
				postfix: new(typeof(CraftablePlacement), nameof(TryPickupCraftable))
			);

			harmony.TryPatch(
				typeof(SObject).GetMethod(nameof(SObject.placementAction)),
				postfix: new(typeof(CraftablePlacement), nameof(UpdateIfNeeded))
			);

			if (typeof(Item).GetMethod("GetOneCopyFrom", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) is MethodInfo info)
				getOneFrom = info.CreateDelegate<Action<Item, Item>>();
			else
				ModEntry.monitor.Log("Failed to reflect GetOneCopyFrom", StardewModdingAPI.LogLevel.Error);
		}

		private static void UpdateIfNeeded(bool __result, GameLocation location, int x, int y, Farmer who, SObject __instance)
		{
			if (!__result || !__instance.modData.ContainsKey(UNIQUE_ITEM_FLAG))
				return;

			Vector2 tile = new(x / 64, y / 64);

			if (location.Objects.TryGetValue(tile, out var placed) && placed.QualifiedItemId == __instance.QualifiedItemId)
			{
				placed.modData.Remove(UNIQUE_ITEM_FLAG);
				Swap(placed.heldObject, __instance.heldObject);
				placed.showNextIndex.Value = __instance.showNextIndex.Value;
				placed.ParentSheetIndex = __instance.ParentSheetIndex;

				if (placed.IsTextSign() && __instance.IsTextSign())
				{
					placed.signText.Value = __instance.signText.Value;
				}
				else if (placed is Chest chest && __instance is Chest oldChest)
				{
					var items = oldChest.Items.ToList();
					oldChest.Items.Clear();
					chest.Items.AddRange(items);
					chest.playerChoiceColor.Value = oldChest.playerChoiceColor.Value;
				}
				else if (placed is Sign sign && __instance is Sign oldSign)
				{
					Swap(oldSign.displayItem, sign.displayItem);
					sign.displayType.Value = oldSign.displayType.Value;
				}
				else if (placed is IndoorPot pot && __instance is IndoorPot oldPot)
				{
					Swap(oldPot.hoeDirt, pot.hoeDirt);
					Swap(oldPot.bush, pot.bush);
					pot.bushLoadDirty.Value = true;
				}
			}
			else
			{
				ModEntry.monitor.Log("Failed to update placed unique object: ID mismatch", StardewModdingAPI.LogLevel.Warn);
				Game1.createItemDebris(__instance, tile * 64f, -1, location);
			}
		}

		private static bool TryPickupCraftable(bool handled, int x, int y, Farmer who)
		{
			if (handled)
				return true;

			if (Game1.activeClickableMenu != null || !Catalog.MenuVisible() || !ModEntry.config.PickupCraftables)
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

						int slot = who.getIndexOfInventoryItem(item);
						if (slot is >= 0 and <= 12)
							who.CurrentToolIndex = slot;

						return true;
					}
					else if (item.modData.ContainsKey(UNIQUE_ITEM_FLAG))
					{
						where.objects[tile] = item;
					}
				}
			}

			return false;
		}

		private static SObject? CreateClone(SObject item)
		{
			var type = item.GetType();
			SObject? newItem;
			try
			{
				newItem = (SObject?)Activator.CreateInstance(type, true);
				if (newItem is null)
					return null;
			}
			catch (Exception ex)
			{
				ModEntry.monitor.Log($"Could not clone object: {ex}", StardewModdingAPI.LogLevel.Debug);
				return null;
			}

			getOneFrom!(newItem, item);
			Swap(item.heldObject, newItem.heldObject);
			newItem.modData[UNIQUE_ITEM_FLAG] = "T";
			newItem.ItemId = item.ItemId;
			newItem.bigCraftable.Value = item.bigCraftable.Value;
			newItem.Category = item.Category;
			newItem.Type = item.Type;
			newItem.showNextIndex.Value = item.showNextIndex.Value;
			newItem.ParentSheetIndex = item.ParentSheetIndex;
			return newItem;
		}

		private static bool TryPickupObject(SObject obj, [NotNullWhen(true)] out SObject? result)
		{
			result = null;

			if (obj.ItemId == "-1")
				return false;

			if (obj is Mannequin mann)
			{
				result = (SObject)mann.getOne();
				return true;
			}

			if (obj.IsTextSign())
			{
				result = (SObject)obj.getOne();
				result.signText.Value = obj.signText.Value;
				result.modData[UNIQUE_ITEM_FLAG] = "T";
				result.showNextIndex.Value = obj.showNextIndex.Value;
				return true;
			}

			if (obj is Chest chest)
			{
				if (chest.GetMutex().IsLocked())
					return false;

				if (chest.isEmpty() && chest.heldObject.Value is null)
				{
					result = (SObject)obj.getOne();
					return true;
				}

				if (CreateClone(chest) is not Chest newchest)
					return false;

				if (chest.Items.Count > 0)
				{
					var items = chest.Items.ToList();
					chest.Items.Clear();
					newchest.Items.AddRange(items);
				}

				newchest.playerChoiceColor.Value = chest.playerChoiceColor.Value;
				result = newchest;
				return true;
			}

			if (obj is IndoorPot pot)
			{
				if (CreateClone(pot) is not IndoorPot newpot)
					return false;

				Swap(pot.hoeDirt, newpot.hoeDirt);
				Swap(pot.bush, newpot.bush);
				result = newpot;
				return true;
			}

			if (obj is Sign sign)
			{
				if (CreateClone(sign) is not Sign newsign)
					return false;

				Swap(sign.displayItem, newsign.displayItem);
				newsign.displayType.Value = sign.displayType.Value;
				result = newsign;
				return true;
			}

			if (obj.heldObject.Value is SObject ho)
			{
				if (ho is Chest heldChest && heldChest.GetMutex().IsLocked())
					return false;

				if (obj.HasContextTag("is_machine") && obj.HasContextTag("machine_input") && !obj.readyForHarvest.Value)
					return false;

				if (CreateClone(obj) is not SObject newObj)
					return false;

				result = newObj;
				return true;
			}

			result = (SObject)obj.getOne();
			return true;
		}

		private static void Swap<T>(NetRef<T> from, NetRef<T> to) where T : class, INetObject<INetSerializable>
		{
			T held = from.Value;
			from.Value = null!;
			to.Value = held;
		}
	}
}
