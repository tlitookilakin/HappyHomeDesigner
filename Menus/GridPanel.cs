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
		public int VisibleCells => scrollBar.VisibleRows * scrollBar.Columns;
		public IReadOnlyList<IGridItem> FilteredItems => search.Filtered;
		public IReadOnlyList<IGridItem> LastFiltered => search.LastFiltered;

		public event Action DisplayChanged;
		public ScrollBar scrollBar = new();

		private static readonly Rectangle BackgroundSource = new(384, 373, 18, 18);
		private readonly SearchBox search = new(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor);
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

			drawTextureBox(b, Game1.mouseCursors, BackgroundSource, xPositionOnScreen - 16, yPositionOnScreen - 20, 
				cols * CellWidth + 32, height + 36, Color.White, 4f, false);

			int count = Math.Min(displayed.Count - offset, height / CellHeight * scrollBar.Columns);
			for (int i = 0; i < count; i++)
				displayed[i + offset].Draw(b, CellWidth * (i % cols) + xPositionOnScreen, CellHeight * (i / cols) + yPositionOnScreen);

			scrollBar.Draw(b);
			if (search_visible)
				search.Draw(b);
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

		public void Resize(int width, int height, int x, int y)
		{
			this.width = width / CellWidth * CellWidth;
			this.height = Math.Max(height / CellHeight * CellHeight, CellHeight);
			xPositionOnScreen = x;
			yPositionOnScreen = y;

			scrollBar.Columns = width / CellWidth;
			scrollBar.VisibleRows = height / CellHeight;
			scrollBar.Resize(this.height + 32, xPositionOnScreen + this.width + 16, yPositionOnScreen - 16);
			UpdateCount();

			search.X = xPositionOnScreen - 15;
			search.Y = yPositionOnScreen + this.height + 25;
		}

		public override bool isWithinBounds(int x, int y)
		{
			int relX = x - xPositionOnScreen;
			int relY = y - yPositionOnScreen;
			// add padding for scrollbar
			return 
				relX is >= -16 && relY is >= -16 && relX < width + 66 && relY < height + 16
				|| search.ContainsPoint(x, y);
		}

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

		public void UpdateCount()
		{
			scrollBar.Rows = search.Filtered.Count / scrollBar.Columns + (search.Filtered.Count % scrollBar.Columns is not 0 ? 1 : 0);
			DisplayChanged?.Invoke();
		}
	}
}
