using HappyHomeDesigner.Data;
using HappyHomeDesigner.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Menus
{
	internal class ItemPage : ScreenPage, IItemPool
	{
		private readonly List<ItemEntry> entries = [];
		private readonly HashSet<string> knownIDs = [];
		private readonly GridPanel Panel;
		private readonly ClickableTextureComponent TrashSlot
			= new(new(0, 0, 64, 64), Catalog.MenuTexture, new(32, 48, 16, 16), 4f, true);
		private int skipped;
		private readonly ClickableTextureComponent Tab;

		public event EventHandler<ItemPoolChangedEvent> ItemPoolChanged;

		public override ICollection<string> KnownIDs => [];

		public IReadOnlyList<IGridItem> Items => entries;

		private ItemEntry Hovered
		{
			get => _hovered;
			set
			{
				if (_hovered == value)
					return;

				_hovered?.Hovered = false;
				_hovered = value;
				_hovered?.Hovered = true;
			}
		}
		private ItemEntry _hovered;

		public ItemPage()
		{
			Panel = new(this, 80, 80, true);
			Tab = new(new(0, 0, 64, 64), Catalog.MenuTexture, new(64, 48, 16, 16), 4f);
			Tab.visible = false;
		}

		/// <inheritdoc/>
		public override int Count() 
			=> entries.Count;

		/// <inheritdoc/>
		public override ClickableTextureComponent GetTab()
			=> Tab;

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

			Hovered = Panel.TrySelect(x, y, out int i) ? (Panel.VisibleItems.Items[i] as ItemEntry) : null;
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
				var item = Panel.VisibleItems.Items[index] as ItemEntry;

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

		public IGridItem GetFocusedItem()
		{
			return null;
		}

		/// <inheritdoc/>
		public override void AppendItems(List<KeyValuePair<IStyleSet, ItemQueryResult>> Items)
		{
			bool changed = false;
			foreach ((_, var res) in Items)
			{
				var item = res.Item;
				if (item is Furniture or Wallpaper || item is not Item obj || item.HasTypeBigCraftable())
					continue;

				if (knownIDs.Add(item.QualifiedItemId))
				{
					changed = true;
					entries.Add(new(obj));
					Tab.visible = true;
				}
				else
				{
					skipped++;
				}

			}

			if (changed)
				ItemPoolChanged?.Invoke(this, new(this, null, false));
		}

		/// <inheritdoc/>
		public override void FinalizeItems()
		{
			LogLoaded("items", entries.Count, skipped);
		}
	}
}
