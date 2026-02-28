using HappyHomeDesigner.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Menus;
using StardewValley;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using HappyHomeDesigner.Patches;
using System.Linq;
using HappyHomeDesigner.Widgets;
using StardewValley.Internal;
using HappyHomeDesigner.Data;

namespace HappyHomeDesigner.Menus
{
	/// <summary>Base type used for pages that with a list of items that support variants and direct placement.</summary>
	/// <typeparam name="T">The variant entry type</typeparam>
	/// <typeparam name="TE">The wrapped item type</typeparam>
	internal abstract class VariantPage<T, TE> : ScreenPage, IItemPool
		where TE: Item
		where T : VariantEntry<TE>
	{
		private readonly string KeyFavs;

		protected readonly List<T> entries = [];
		protected readonly List<T> Favorites = [];
		protected SimpleItemPool VariantPool;
		protected T Selected
		{
			get => _selected;
			set
			{
				if (_selected == value)
					return;

				_selected?.Selected = false;
				_selected = value;
				_selected?.Selected = true;

				if (value != null)
					VariantPool.SetItems(value.GetVariants(), true);
			}
		}
		private T _selected;
		protected T Hovered
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
		private T HoveredVariant
		{
			get => _hovered_variant;
			set
			{
				if (_hovered_variant == value)
					return;

				_hovered_variant?.Hovered = false;
				_hovered_variant = value;
				_hovered_variant?.Hovered = true;
			}
		}
		protected T _hovered;
		private T _hovered_variant;
		protected List<T>[] CustomFilters;
		protected ClickableTextureComponent Tab;

		public IReadOnlyList<IGridItem> Items
		{
			get => _items;
			set
			{
				if (_items == value)
					return;

				var old = _items;
				_items = value;

				ItemPoolChanged?.Invoke(this, new(this, old, true));
			}
		}
		private IReadOnlyList<IGridItem> _items = [];
		private int skipped;
		private readonly string typeName;

		protected int iconRow;
		protected readonly GridPanel MainPanel;
		protected readonly GridPanel VariantPanel;
		protected readonly ClickableTextureComponent TrashSlot = new(new(0, 0, 64, 64), Catalog.MenuTexture, new(32, 48, 16, 16), 4f, true);

		internal static HashSet<string> knownIDs = [];

		public override ICollection<string> KnownIDs => knownIDs;

		private static HashSet<string> preservedFavorites;

		public event EventHandler<ItemPoolChangedEvent> ItemPoolChanged;

		/// <summary>Create and setup a variant page</summary>
		/// <param name="FavoritesKey">The moddata key used to track favorites for this page</param>
		/// <param name="typeName">Used for logging</param>
		public VariantPage(string FavoritesKey, string typeName)
		{
			this.typeName = typeName;

			KeyFavs = FavoritesKey;
			preservedFavorites = [.. DataService.GetFavoritesFor(Game1.player, KeyFavs)];
			knownIDs.Clear();

			VariantPool = new(() => null);
			MainPanel = new(this, CELL_SIZE, CELL_SIZE, true);
			VariantPanel = new(VariantPool, CELL_SIZE, CELL_SIZE, false);

			MainPanel.VisibleItems.ItemPoolChanged += DisplayChanged;

			Init();
			Tab.visible = false;

			Items = entries;

			if (custom_tabs is not null)
			{
				int c = custom_tabs.Count + 1;
				CustomFilters = new List<T>[c];
				for (int i = 0; i < c; i++)
					CustomFilters[i] = [];
			}
			else
			{
				CustomFilters = [];
			}
		}

		/// <inheritdoc/>
		public override void AppendItems(List<KeyValuePair<IStyleSet, ItemQueryResult>> Items)
		{
			bool changed = false;
			foreach ((_, var item) in GetItemsFrom(Items, preservedFavorites))
			{
				if (knownIDs.Add(item.ToString()))
				{
					changed = true;
					entries.Add(item);
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
			LogLoaded(typeName, entries.Count, skipped);
		}

		protected virtual void DisplayChanged(object sender, ItemPoolChangedEvent e)
		{
			if (Selected is IGridItem s && !e.Source.Items.Contains(s))
				Selected = null;
		}

		public override ClickableTextureComponent GetTab()
		{
			return Tab;
		}

		public bool TrySetCustomFilter(T entry)
		{
			if (custom_tabs is null)
				return false;

			for (int i = 0; i < custom_tabs.Count; i++)
			{
				var tab = custom_tabs[i];
				if (tab.FilterCondition is "TRUE" || GameStateQuery.CheckConditions(tab.FilterCondition, inputItem: entry.Item))
					CustomFilters[i].Add(entry);
			}

			return true;
		}

		/// <summary>Initial setup. Happens after favorites are loaded and before items are processed.</summary>
		public abstract void Init();

		/// <summary>Processes the source items into appropriate variant entries</summary>
		/// <param name="source">The raw item list</param>
		/// <param name="favorites">The favorites list</param>
		/// <returns>The processed items</returns>
		public abstract IEnumerable<KeyValuePair<IStyleSet, T>> GetItemsFrom(IEnumerable<KeyValuePair<IStyleSet, ItemQueryResult>> source, ICollection<string> favorites);

		/// <inheritdoc/>
		public override int Count() 
			=> entries.Count;

		public override void draw(SpriteBatch b)
		{
			MainPanel.DrawShadow(b);
			if (Selected != null)
				VariantPanel.DrawShadow(b);

			base.draw(b);
			DrawFilters(b, iconRow, 1, xPositionOnScreen, yPositionOnScreen);
			TrashSlot.draw(b);
			MainPanel.draw(b);

			//AltTex.forceMenuDraw = true;
			if (Selected != null)
				VariantPanel.draw(b);
			//AltTex.forceMenuDraw = false;
		}

		/// <inheritdoc/>
		public override void DrawTooltip(SpriteBatch b)
		{
			if (Hovered is not null)
			{
				if (ModEntry.config.FurnitureTooltips)
					drawToolTip(b, Hovered.Item.getDescription(), Hovered.Item.DisplayName, Hovered.Item);

				if (ModEntry.config.Magnify)
					DrawMagnified(b, Hovered.Item);
			}

			if (HoveredVariant is not null)
			{
				if (ModEntry.config.Magnify)
					DrawMagnified(b, HoveredVariant.Item);
			}
		}

		public override void Resize(Rectangle region)
		{
			base.Resize(region);

			const int top_margin = 232;

			MainPanel.Resize(width - 36, height - 64, xPositionOnScreen + 55, yPositionOnScreen);
			VariantPanel.Resize(
				CELL_SIZE * 3 + 32,
				height - top_margin, 
				Game1.uiViewport.Width - CELL_SIZE * 3 - 64, 
				yPositionOnScreen + top_margin
			);
			TrashSlot.setPosition(
				MainPanel.xPositionOnScreen + MainPanel.width - 64 + GridPanel.BORDER_WIDTH,
				MainPanel.yPositionOnScreen + MainPanel.height + GridPanel.BORDER_WIDTH + GridPanel.MARGIN_BOTTOM
			);
			InventoryButton.setPosition(
				MainPanel.xPositionOnScreen + MainPanel.width - 128 + GridPanel.BORDER_WIDTH - GridPanel.MARGIN_BOTTOM,
				MainPanel.yPositionOnScreen + MainPanel.height + GridPanel.BORDER_WIDTH + GridPanel.MARGIN_BOTTOM
			);
		}

		public override void performHoverAction(int x, int y)
		{
			MainPanel.performHoverAction(x, y);
			VariantPanel.performHoverAction(x, y);

			Hovered = MainPanel.TrySelect(x, y, out int index) ?
				(MainPanel.VisibleItems.Items[index] as T) :
				null;

			HoveredVariant = VariantPanel.TrySelect(x, y, out index) ?
				(VariantPanel.VisibleItems.Items[index] as T) :
				null;
		}

		/// <summary>
		/// Change which items are displayed based on the current filter
		/// </summary>
		/// <returns>The new list of items to be displayed</returns>
		public abstract IReadOnlyList<IGridItem> ApplyFilter();

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);

			if (!MainPanel.isWithinBounds(x, y) && TrySelectFilter(x, y, playSound))
			{
				Items = ApplyFilter();
				return;
			}

			HandleGridClick(x, y, playSound, MainPanel, true);
			if (Selected != null)
				HandleGridClick(x, y, playSound, VariantPanel, false);

			if (TrashSlot.containsPoint(x, y))
				DeleteActiveItem(playSound, knownIDs);
		}

		public override void receiveScrollWheelAction(int direction)
		{
			var pos = Game1.getMousePosition(true);

			if (MainPanel.isWithinBounds(pos.X, pos.Y))
				MainPanel.receiveScrollWheelAction(direction);

			else if (VariantPanel.isWithinBounds(pos.X, pos.Y))
				VariantPanel.receiveScrollWheelAction(direction);
		}

		/// <summary>Handles mouse interaction with a specific panel of this page</summary>
		/// <param name="mx">Mouse X</param>
		/// <param name="my">Mouse Y</param>
		/// <param name="playSound">Whether or not to play sounds</param>
		/// <param name="panel">Which panel to test</param>
		/// <param name="allowVariants">Whether variants can be displayed for items in this panel or not</param>
		private void HandleGridClick(int mx, int my, bool playSound, GridPanel panel, bool allowVariants)
		{
			panel.receiveLeftClick(mx, my, playSound);

			if (panel.TrySelect(mx, my, out int index))
			{
				var entry = panel.VisibleItems.Items[index] as T;

				if (allowVariants)
				{
					if (ModEntry.config.FavoriteModifier.IsDown())
					{
						if (entry.ToggleFavorite(playSound))
							Favorites.Add(entry);
						else
							Favorites.Remove(entry);

						if (Items == Favorites)
						{
							IReadOnlyList<T> old = entry.Favorited ? Favorites.CopyWithout(entry) : [.. Favorites, entry];
							ItemPoolChanged?.Invoke(this, new(this, old, false));
						}

						return;
					}

					if (entry.HasVariants)
					{
						Selected = entry;
						if (playSound)
							Game1.playSound("shwip");

						return;
					}
					Selected = null;
				}

				if (ModEntry.config.GiveModifier.IsDown())
				{
					if (Game1.player.addItemToInventoryBool(entry.GetOne()) && playSound)
						Game1.playSound("pickUpItem");
					return;
				}

				DeleteActiveItem(false, knownIDs);

				var allowSet = true;
				if (Game1.player.TemporaryItem is Item current)
				{
					current.onDetachedFromParent();
					if (!Game1.player.addItemToInventoryBool(current))
					{
						if (current.modData.ContainsKey(CraftablePlacement.UNIQUE_ITEM_FLAG))
							allowSet = false;
						else
							Game1.createItemDebris(current, Game1.player.Position, -1);
					}
				}

				if (allowSet)
				{
					Game1.player.TemporaryItem = entry.GetOne();
					if (playSound)
						Game1.playSound("stoneStep");
				}
			}
		}

		public override void receiveRightClick(int x, int y, bool playSound = true)
		{
			MainPanel.receiveRightClick(x, y, playSound);
		}

		public override bool isWithinBounds(int x, int y)
		{
			return
				base.isWithinBounds(x, y) ||
				MainPanel.isWithinBounds(x, y) ||
				(Selected != null && VariantPanel.isWithinBounds(x, y)) ||
				TrashSlot.containsPoint(x, y);
		}

		/// <inheritdoc/>
		public override void Exit()
		{
			DataService.SaveFavoritesFor(Game1.player, KeyFavs, Favorites.Select(Convert.ToString).Concat(preservedFavorites));
		}

		/// <inheritdoc/>
		public override bool TryApplyButton(SButton button, bool IsPressed)
		{
			// TODO controller movement

			switch (button)
			{
				case SButton.Delete:
					if (IsPressed)
						DeleteActiveItem(true, knownIDs);
					break;
				default:
					return false;
			}

			return true;
		}

		/// <inheritdoc/>
		public override void DeleteActiveItem(bool playSound)
		{
			DeleteActiveItem(playSound, knownIDs);
		}

		public IReadOnlyList<IGridItem> ApplyFilterCustom()
		{
			if (custom_tabs is null)
				return ApplyFilter();

			return current_filter < CustomFilters.Length ? CustomFilters[current_filter] : Favorites;
		}

		public IGridItem GetFocusedItem()
		{
			return Selected;
		}
	}
}
