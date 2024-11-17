﻿using HappyHomeDesigner.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Menus;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using System.Linq;

namespace HappyHomeDesigner.Menus
{
	/// <summary>Base type used for pages that with a list of items that support variants and direct placement.</summary>
	/// <typeparam name="T">The variant entry type</typeparam>
	/// <typeparam name="TE">The wrapped item type</typeparam>
	internal abstract class VariantPage<T, TE> : ScreenPage 
		where TE: Item
		where T : VariantEntry<TE>
	{
		private readonly string KeyFavs;

		protected readonly List<T> entries = [];
		protected IReadOnlyList<VariantEntry<TE>> variants = [];
		protected readonly List<T> Favorites = [];
		private bool showVariants = false;
		private int variantIndex = -1;
		private T? variantItem;
		protected TE? hovered;
		protected TE? hovered_variant;

		protected int iconRow;
		protected readonly GridPanel MainPanel = new(CELL_SIZE, CELL_SIZE, true);
		protected readonly GridPanel VariantPanel = new(CELL_SIZE, CELL_SIZE, false);
		protected readonly ClickableTextureComponent TrashSlot = new(new(0, 0, 48, 48), Catalog.MenuTexture, new(32, 48, 16, 16), 3f, true) { myID = 10 };

		private static readonly Rectangle FrameSource = new(0, 256, 60, 60);
		internal static HashSet<string> knownIDs = new();

		private static string[] preservedFavorites;

		/// <summary>Create and setup a variant page</summary>
		/// <param name="existing">A list of items that belong to this page</param>
		/// <param name="FavoritesKey">The moddata key used to track favorites for this page</param>
		/// <param name="typeName">Used for logging</param>
		public VariantPage(IEnumerable<ISalable> existing, string FavoritesKey, string typeName)
		{
			KeyFavs = FavoritesKey;

			var favorites = new HashSet<string>(
				Game1.player.modData.TryGetValue(KeyFavs, out var s) ?
				s.Split('	', StringSplitOptions.RemoveEmptyEntries) :
				[]
			);

			knownIDs.Clear();
			int skipped = 0;

			Init();

			var timer = Stopwatch.StartNew();

			foreach (var item in GetItemsFrom(existing, favorites))
			{
				if (knownIDs.Add(item.ToString()))
					entries.Add(item);
				else
					skipped++;
			}

			timer.Stop();
			ModEntry.monitor.Log($"Populated {entries.Count} {typeName} items in {timer.ElapsedMilliseconds} ms", LogLevel.Debug);
			if (skipped is not 0)
				ModEntry.monitor.Log($"Found and skipped {skipped} duplicate {typeName} items", LogLevel.Debug);

			MainPanel.DisplayChanged += UpdateDisplay;

			MainPanel.Items = entries;
			VariantPanel.Items = variants;

			preservedFavorites = [.. favorites];
			controlRegions.Add(MainPanel.Control);
		}

		/// <summary>Initial setup. Happens after favorites are loaded and before items are processed.</summary>
		public abstract void Init();

		/// <summary>Processes the source items into appropriate variant entries</summary>
		/// <param name="source">The raw item list</param>
		/// <param name="favorites">The favorites list</param>
		/// <returns>The processed items</returns>
		public abstract IEnumerable<T> GetItemsFrom(IEnumerable<ISalable> source, ICollection<string> favorites);

		/// <inheritdoc/>
		public override int Count() 
			=> entries.Count;

		/// <summary>Updates the selected cell index when the search or filter changes</summary>
		public void UpdateDisplay()
		{
			variantIndex = MainPanel.FilteredItems.Find(variantItem);

			if (variantIndex is -1)
				variantItem = null;

			showVariants = variantIndex is not -1;
		}

		public override void draw(SpriteBatch b)
		{
			MainPanel.DrawShadow(b);
			if (showVariants)
				VariantPanel.DrawShadow(b);

			base.draw(b);
			DrawFilters(b, iconRow, 1, xPositionOnScreen, yPositionOnScreen);
			MainPanel.draw(b);
			TrashSlot.draw(b);

			if (variantIndex is >= 0)
			{
				int cols = MainPanel.Columns;
				int variantDrawIndex = variantIndex - MainPanel.Offset;
				if (variantDrawIndex >= 0 && variantDrawIndex < MainPanel.VisibleCells)
					b.DrawFrame(Game1.menuTexture, new(
						xPositionOnScreen + variantDrawIndex % cols * CELL_SIZE - 8 + 55,
						yPositionOnScreen + variantDrawIndex / cols * CELL_SIZE - 8,
						CELL_SIZE + 16, CELL_SIZE + 16),
						FrameSource, 13, 1, Color.White, 0);
			}

			//AltTex.forceMenuDraw = true;
			if (showVariants)
				VariantPanel.draw(b);
			//AltTex.forceMenuDraw = false;
		}

		/// <inheritdoc/>
		public override void DrawTooltip(SpriteBatch b)
		{
			if (hovered is not null)
			{
				if (ModEntry.config.FurnitureTooltips)
					drawToolTip(b, hovered.getDescription(), hovered.DisplayName, hovered);

				if (ModEntry.config.Magnify)
					DrawMagnified(b, hovered);
			}

			if (hovered_variant is not null)
			{
				if (ModEntry.config.Magnify)
					DrawMagnified(b, hovered_variant);
			}
		}

		public override void Resize(Rectangle region)
		{
			base.Resize(region);

			int bottom_margin = ModEntry.config.LargeVariants ? 0 : 240;
			const int top_margin = 256;

			MainPanel.Resize(width - 36, height - 64, xPositionOnScreen + 55, yPositionOnScreen);
			VariantPanel.Resize(
				CELL_SIZE * 3 + 32, 
				height - (bottom_margin + top_margin), 
				Game1.uiViewport.Width - CELL_SIZE * 3 - 80, 
				yPositionOnScreen + top_margin
			);
			TrashSlot.setPosition(
				MainPanel.xPositionOnScreen + MainPanel.width - 48 + GridPanel.BORDER_WIDTH,
				MainPanel.yPositionOnScreen + MainPanel.height + GridPanel.BORDER_WIDTH + GridPanel.MARGIN_BOTTOM
			);
		}

		public override void performHoverAction(int x, int y)
		{
			MainPanel.performHoverAction(x, y);
			VariantPanel.performHoverAction(x, y);

			hovered = MainPanel.TrySelect(x, y, out int index) ?
				((T)MainPanel.FilteredItems[index]).Item :
				null;

			hovered_variant = VariantPanel.TrySelect(x, y, out index) ?
				((T)VariantPanel.FilteredItems[index]).Item :
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
				HideVariants();
				MainPanel.Items = ApplyFilter();
				return;
			}

			HandleGridClick(x, y, playSound, MainPanel, true);
			if (showVariants)
				HandleGridClick(x, y, playSound, VariantPanel, false);

			if (TrashSlot.containsPoint(x, y))
				DeleteActiveItem(playSound, knownIDs);
		}

		/// <summary>Opens the variant panel for a given item</summary>
		/// <param name="entry">The item to get variants from</param>
		/// <param name="index">The index of the item</param>
		private void ShowVariantsFor(T entry, int index)
		{
			variantIndex = index;
			variantItem = entry;
			variants = entry.GetVariants();
			VariantPanel.Items = variants;
			showVariants = true;
		}

		/// <summary>Closes the variant panel</summary>
		private void HideVariants()
		{
			variantIndex = -1;
			showVariants = false;
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
				var entry = (T)panel.FilteredItems[index];

				if (allowVariants)
				{
					if (ModEntry.config.FavoriteModifier.IsDown())
					{
						if (entry.ToggleFavorite(playSound))
							Favorites.Add(entry);
						else
							Favorites.Remove(entry);

						if (MainPanel.Items == Favorites)
							MainPanel.UpdateCount();

						return;
					}

					if (entry.HasVariants)
					{
						ShowVariantsFor(entry, index);
						if (playSound)
							Game1.playSound("shwip");

						return;
					}
					HideVariants();
				}

				if (ModEntry.config.GiveModifier.IsDown())
				{
					if (Game1.player.addItemToInventoryBool(entry.GetOne()) && playSound)
						Game1.playSound("pickUpItem");
					return;
				}

				DeleteActiveItem(false, knownIDs);

				Game1.player.TemporaryItem = entry.GetOne();
				if (playSound)
					Game1.playSound("stoneStep");
			}
		}

		public override bool isWithinBounds(int x, int y)
		{
			return
				base.isWithinBounds(x, y) ||
				MainPanel.isWithinBounds(x, y) ||
				(showVariants && VariantPanel.isWithinBounds(x, y)) ||
				TrashSlot.containsPoint(x, y);
		}

		/// <inheritdoc/>
		public override void Exit()
		{
			Game1.player.modData[KeyFavs] = string.Join('	', Favorites) + '	' + string.Join('	', preservedFavorites);
		}

		/// <inheritdoc/>
		public override bool TryApplyButton(SButton button, bool IsPressed, Vector2 pointer)
		{
			return MainPanel.TryApplyButton(button, IsPressed, pointer);
		}

		/// <inheritdoc/>
		public override void DeleteActiveItem(bool playSound)
		{
			DeleteActiveItem(playSound, knownIDs);
		}
	}
}
