using HappyHomeDesigner.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Objects;
using System.Collections.Generic;

namespace HappyHomeDesigner.Menus
{
	internal class ItemPage : ScreenPage
	{
		private readonly List<ItemEntry> entries = new();
		private readonly HashSet<string> knownIDs = new();
		private readonly GridPanel Panel = new(80, 80, true);
		private readonly ClickableTextureComponent TrashSlot
			= new(new(0, 0, 64, 64), Catalog.MenuTexture, new(32, 48, 16, 16), 4f, true);

		public ItemPage(IEnumerable<ISalable> source)
		{
			foreach(var item in source)
			{
				if (item is Furniture or Wallpaper or null || item is not Item obj || item.HasTypeBigCraftable())
					continue;

				if (!knownIDs.Add(item.QualifiedItemId))
					continue;

				entries.Add(new(obj));
			}

			Panel.Items = entries;
		}

		/// <inheritdoc/>
		public override int Count() 
			=> entries.Count;

		/// <inheritdoc/>
		public override ClickableTextureComponent GetTab() 
			=> new(new(0, 0, 64, 64), Catalog.MenuTexture, new(64, 48, 16, 16), 4f);

		public override void draw(SpriteBatch b)
		{
			Panel.DrawShadow(b);
			TrashSlot.draw(b);
			base.draw(b);
			Panel.draw(b);
		}

		public override void performHoverAction(int x, int y)
		{
			base.performHoverAction(x, y);
			Panel.performHoverAction(x, y);
		}

		public override void Resize(Rectangle region)
		{
			base.Resize(region);

			Panel.Resize(width - 36, height - 64, xPositionOnScreen + 55, yPositionOnScreen);
			TrashSlot.setPosition(
				Panel.xPositionOnScreen + Panel.width - 64 + GridPanel.BORDER_WIDTH,
				Panel.yPositionOnScreen + Panel.height + GridPanel.BORDER_WIDTH + GridPanel.MARGIN_BOTTOM
			);
			InventoryButton.setPosition(
				Panel.xPositionOnScreen + Panel.width - 128 + GridPanel.BORDER_WIDTH - GridPanel.MARGIN_BOTTOM,
				Panel.yPositionOnScreen + Panel.height + GridPanel.BORDER_WIDTH + GridPanel.MARGIN_BOTTOM
			);
		}

		public override void receiveScrollWheelAction(int direction)
		{
			Panel.receiveScrollWheelAction(direction);
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);
			if (Panel.TrySelect(x, y, out int index))
			{
				var item = Panel.Items[index] as ItemEntry;

				if (Game1.player.addItemToInventoryBool(item.item.getOne()) && playSound)
					Game1.playSound("pickUpItem");

				return;
			}

			if (TrashSlot.containsPoint(x, y) && Game1.player.ActiveObject.CanDelete(knownIDs))
			{
				if (Game1.player.ActiveObject == Game1.player.TemporaryItem)
					Game1.player.TemporaryItem = null;
				else
					Game1.player.removeItemFromInventory(Game1.player.ActiveObject);

				if (playSound)
					Game1.playSound("trashcan");
			}
		}

		public override bool isWithinBounds(int x, int y)
		{
			return 
				base.isWithinBounds(x, y) ||
				Panel.isWithinBounds(x, y) || 
				TrashSlot.containsPoint(x, y);
		}

		/// <inheritdoc/>
		public override bool TryApplyButton(SButton button, bool IsPressed)
		{
			// TODO controller support

			return false;
		}

		public override void DeleteActiveItem(bool playSound)
		{
			DeleteActiveItem(playSound, knownIDs);
		}

		public override void receiveRightClick(int x, int y, bool playSound = true)
		{
			Panel.receiveRightClick(x, y, playSound);
		}
	}
}
