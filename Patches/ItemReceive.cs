using HappyHomeDesigner.Framework;
using StardewValley;

namespace HappyHomeDesigner.Patches
{
	internal class ItemReceive
	{
		public static void Apply(HarmonyHelper helper)
		{
			helper
				.With<Farmer>(nameof(Farmer.GetItemReceiveBehavior)).Postfix(ChangeItemReceiveBehavior)
				.With(nameof(Farmer.OnItemReceived)).Postfix(ReceiveItem)
				.With<Item>(nameof(Item.checkForSpecialItemHoldUpMeessage)).Postfix(AddHoldUpMessage);
		}

		private static void ReceiveItem(Farmer __instance, Item item)
		{
			switch(item.QualifiedItemId)
			{
				case "(O)" + AssetManager.CARD_ID:
					if (__instance.hasOrWillReceiveMail(AssetManager.CARD_FLAG))
						return;

					__instance.mailReceived.Add(AssetManager.CARD_FLAG);
					Game1.PerformActionWhenPlayerFree(
						() => __instance.holdUpItemThenMessage(item, true)
					);
					break;
				case "(O)" + AssetManager.PORTABLE_ID:
					__instance.removeItemFromInventory(item);
					__instance.addItemToInventory(ItemRegistry.Create("(T)" + AssetManager.PORTABLE_ID));
					break;
			}
		}

		private static string AddHoldUpMessage(string original, Item __instance)
		{
			if (__instance.QualifiedItemId is "(O)" + AssetManager.CARD_ID)
				return ModEntry.i18n.Get("item.card.receive");
			return original;
		}

		private static void ChangeItemReceiveBehavior(Item item, ref bool needsInventorySpace, ref bool showNotification)
		{
			switch (item.QualifiedItemId)
			{
				case "(O)" + AssetManager.CARD_ID:
					needsInventorySpace = false;
					showNotification = false;
					break;

				case "(O)" + AssetManager.PORTABLE_ID:
					needsInventorySpace = true;
					showNotification = false;
					break;
			}
		}
	}
}
