﻿using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using HappyHomeDesigner.Widgets;

namespace HappyHomeDesigner.Menus
{
	// TODO add controller support
	public class GridPanel : IClickableMenu
	{
		public const int BORDER_WIDTH = 16;
		public const int MARGIN_BOTTOM = 8;

		public readonly int CellWidth;
		public readonly int CellHeight;

		public int Offset => scrollBar.CellOffset;
		public int Columns => scrollBar.Columns;
		public int VisibleCells => scrollBar.VisibleRows * scrollBar.Columns;
		public IReadOnlyList<IGridItem> FilteredItems => search.Filtered;

		public event Action DisplayChanged;
		public ScrollBar scrollBar = new();

		private static readonly Rectangle BackgroundSource = new(384, 373, 18, 18);
		private readonly SearchBox search = 
			new(null, Game1.smallFont, Game1.textColor) { TitleText = ModEntry.i18n.Get("ui.search.name")};
		private readonly bool search_visible;

		public IReadOnlyList<IGridItem> Items
		{
			get => items;
			set
			{
				items = value;
				search.Source = items;
				UpdateCount();
				scrollBar.Reset();
			}
		}
		private IReadOnlyList<IGridItem> items;

		public GridPanel(int cellWidth, int cellHeight, bool showSearch)
		{
			CellWidth = cellWidth;
			CellHeight = cellHeight;

			search.OnTextChanged += UpdateCount;

			search_visible = showSearch;
		}

		public override void draw(SpriteBatch b)
		{
			int offset = scrollBar.CellOffset;
			int cols = scrollBar.Columns;

			var displayed = search.Filtered;

			drawTextureBox(b, Game1.mouseCursors, BackgroundSource, xPositionOnScreen - BORDER_WIDTH, 
				yPositionOnScreen - (BORDER_WIDTH + 4), width + (BORDER_WIDTH * 2), 
				height + (BORDER_WIDTH * 2 + 4), Color.White, 4f, false);

			int count = Math.Min(displayed.Count - offset, height / CellHeight * cols);
			for (int i = 0; i < count; i++)
				displayed[i + offset].Draw(b, CellWidth * (i % cols) + xPositionOnScreen, CellHeight * (i / cols) + yPositionOnScreen);

			scrollBar.Draw(b);
			if (search_visible)
				search.Draw(b);

			DrawOverlay(b);
		}

		private void DrawOverlay(SpriteBatch b)
		{
			if (!ModEntry.config.SeasonalOverlay)
				return;

			int yOffset = Game1.currentLocation.GetSeasonIndex() * 64;
			b.Draw(Catalog.OverlayTexture,
				new Rectangle(xPositionOnScreen - 28, yPositionOnScreen - 32, 256, 256),
				new Rectangle(0, yOffset, 64, 64), Color.White
			);
			b.Draw(Catalog.OverlayTexture,
				new Rectangle(xPositionOnScreen + width - 256 + 28, yPositionOnScreen + height - 256 + 28, 256, 256),
				new Rectangle(64, yOffset, 64, 64), Color.White
			);
		}

		/// <summary>Called before the panels and tabs are drawn</summary>
		public void DrawShadow(SpriteBatch b)
		{
			drawTextureBox(b, Game1.mouseCursors, BackgroundSource, xPositionOnScreen - BORDER_WIDTH - 4, 
				yPositionOnScreen - (BORDER_WIDTH + 4) + 4, width + 32, height + 36, Color.Black * .4f, 4f, false);
		}

		public override void receiveScrollWheelAction(int direction)
		{
			scrollBar.AdvanceRows(direction);
		}

		public override void performHoverAction(int x, int y)
		{
			scrollBar.Hover(x, y);
			if (search_visible)
				search.Update();
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			scrollBar.Click(x, y);
		}

		public void Resize(int width, int height, int x, int y, int search_right_offset = (64 + 4) * 2 - BORDER_WIDTH)
		{
			this.width = width / CellWidth * CellWidth;
			this.height = Math.Max(height / CellHeight * CellHeight, CellHeight);
			xPositionOnScreen = x;
			yPositionOnScreen = y;

			scrollBar.Columns = width / CellWidth;
			scrollBar.VisibleRows = height / CellHeight;
			scrollBar.Resize(this.height + (BORDER_WIDTH * 2) + 4, xPositionOnScreen + this.width + BORDER_WIDTH, yPositionOnScreen - (BORDER_WIDTH + 4));
			UpdateCount();

			search.X = xPositionOnScreen - BORDER_WIDTH;
			search.Y = yPositionOnScreen + this.height + BORDER_WIDTH + MARGIN_BOTTOM + 8;
			search.SmallWidth = this.width - search_right_offset;
			search.FullWidth = this.width + BORDER_WIDTH + 8;
			search.Height = 48;
		}

		public override bool isWithinBounds(int x, int y)
		{
			int relX = x - xPositionOnScreen;
			int relY = y - yPositionOnScreen;
			// add padding for scrollbar
			return 
				relX is >= -BORDER_WIDTH && relY is >= -BORDER_WIDTH && 
				relX < width + BORDER_WIDTH + ScrollBar.WIDTH && relY < height + BORDER_WIDTH
				|| search.ContainsPoint(x, y);
		}

		/// <summary>Select an item from the grid if possible</summary>
		/// <param name="x">Mouse X</param>
		/// <param name="y">Mouse Y</param>
		/// <param name="which">The index of the selected item</param>
		/// <returns>Whether or not an item was selected</returns>
		public bool TrySelect(int x, int y, out int which)
		{
			which = -1;

			int relX = x - xPositionOnScreen;
			int relY = y - yPositionOnScreen;

			if (relX is < 0 || relY is < 0 || relX > width || relY > height)
				return false;

			which = relX / CellWidth + scrollBar.Columns * (relY / CellHeight) + scrollBar.CellOffset;
			return which < search.Filtered.Count;
		}

		/// <summary>Update the item count when the item list changes</summary>
		public void UpdateCount()
		{
			scrollBar.Rows = search.Filtered.Count / scrollBar.Columns + (search.Filtered.Count % scrollBar.Columns is not 0 ? 1 : 0);
			DisplayChanged?.Invoke();
		}
	}
}
