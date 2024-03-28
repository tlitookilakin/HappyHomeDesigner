using HappyHomeDesigner.Framework;
using HarmonyLib;
using StardewValley;

namespace HappyHomeDesigner.Patches
{
	internal class ItemReceive
	{
		public static void Apply(Harmony harmony)
		{
			harmony.Patch(
				typeof(Farmer).GetMethod(nameof(Farmer.GetItemReceiveBehavior)),
				postfix: new(typeof(ItemReceive), nameof(ChangeItemReceiveBehavior))
			);

			harmony.Patch(
				typeof(Farmer).GetMethod(nameof(Farmer.OnItemReceived)),
				postfix: new(typeof(ItemReceive), nameof(ReceiveItem))
			);

			harmony.Patch(
				typeof(Item).GetMethod(nameof(Item.checkForSpecialItemHoldUpMeessage)),
				postfix: new(typeof(ItemReceive), nameof(AddHoldUpMessage))
			);
		}

		private static void ReceiveItem(Farmer __instance, Item item)
		{
			if (item.QualifiedItemId is "(O)" + AssetManager.CARD_ID)
				Game1.PerformActionWhenPlayerFree(() => {
					__instance.mailReceived.Add(AssetManager.CARD_FLAG);
					__instance.holdUpItemThenMessage(item, true);
				});
		}

		private static string AddHoldUpMessage(string original, Item __instance)
		{
			if (__instance.QualifiedItemId is "(O)" + AssetManager.CARD_ID)
				return ModEntry.i18n.Get("item.card.receive");
			return original;
		}

		private static void ChangeItemReceiveBehavior(Item item, ref bool needsInventorySpace, ref bool showNotification)
		{
			if (item.QualifiedItemId is "(O)" + AssetManager.CARD_ID)
			{
				needsInventorySpace = false;
				showNotification = false;
			}
		}
	}
}
