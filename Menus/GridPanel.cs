using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace HappyHomeDesigner.Menus
{
	public class GridPanel : IClickableMenu
	{
		public readonly int CellWidth;
		public readonly int CellHeight;

		public int Offset => scrollBar.CellOffset;
		public int Columns => scrollBar.Columns;

		private int cells_h;
		private int cells_v;

		public ScrollBar scrollBar = new();

		private static readonly Rectangle BackgroundSource = new(384, 373, 18, 18);

		public IReadOnlyList<IGridItem> Items
		{
			get => items;
			set
			{
				items = value;
				scrollBar.Rows = items.Count / scrollBar.Columns + (items.Count % scrollBar.Columns is not 0 ? 1 : 0);
			}
		}
		private IReadOnlyList<IGridItem> items;

		public GridPanel(int cellWidth, int cellHeight)
		{
			CellWidth = cellWidth;
			CellHeight = cellHeight;
		}

		public override void draw(SpriteBatch b)
		{
			int offset = scrollBar.CellOffset;
			int cols = scrollBar.Columns;

			drawTextureBox(b, Game1.mouseCursors, BackgroundSource, xPositionOnScreen - 16, yPositionOnScreen - 20, 
				cols * CellWidth + 32, height + 36, Color.White, 4f);

			int count = Math.Min(items.Count - offset, height / CellHeight * scrollBar.Columns);
			for (int i = 0; i < count; i++)
				items[i + offset].Draw(b, CellWidth * (i % cols) + xPositionOnScreen, CellHeight * (i / cols) + yPositionOnScreen);

			scrollBar.Draw(b);
		}

		public override void receiveScrollWheelAction(int direction)
		{
			scrollBar.AdvanceRows(direction);
		}

		public void Resize(int width, int height, int x, int y)
		{
			this.width = width / CellWidth * CellWidth;
			this.height = height / CellHeight * CellHeight;
			xPositionOnScreen = x;
			yPositionOnScreen = y;

			scrollBar.Columns = width / CellWidth;
			scrollBar.VisibleRows = height / CellHeight;
			scrollBar.Resize(this.height + 32, xPositionOnScreen + this.width + 16, yPositionOnScreen - 16);
		}

		public override bool isWithinBounds(int x, int y)
		{
			int relX = x - xPositionOnScreen;
			int relY = y - yPositionOnScreen;
			// add padding for scrollbar
			return relX is >= 0 && relY is >= 0 && relX < width + 32 && relY < height;
		}

		public bool TrySelect(int x, int y, out int which)
		{
			which = -1;

			int relX = x - xPositionOnScreen;
			int relY = y - yPositionOnScreen;

			if (relX is < 0 || relY is < 0 || relX > width || relY > height)
				return false;

			which = relX / CellWidth + scrollBar.Columns * (relY / CellHeight) + scrollBar.CellOffset;
			return which < items.Count;
		}
	}
}
