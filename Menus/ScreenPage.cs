using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;

namespace HappyHomeDesigner.Menus
{
	public abstract class ScreenPage : IClickableMenu
	{
		internal const int CELL_SIZE = 80;
		internal const int FILTER_WIDTH = 48;
		internal const int FILTER_HEIGHT = 32;

		internal int filter_count;
		internal int current_filter;

		public abstract ClickableTextureComponent GetTab();

		public virtual void Resize(Rectangle region)
		{
			width = region.Width;
			height = region.Height;
			xPositionOnScreen = region.X;
			yPositionOnScreen = region.Y;
		}

		public bool TrySelectFilter(int x, int y, bool playSound)
		{
			int relX = x - xPositionOnScreen;
			int relY = y - yPositionOnScreen;

			if (relX is > FILTER_WIDTH)
				return false;

			int which = relY / FILTER_HEIGHT;

			if (which >= filter_count || which == current_filter)
				return false;

			if (playSound)
				Game1.playSound("shwip");

			current_filter = which;
			return true;
		}

		public void DrawFilters(SpriteBatch batch, int textureRow, int ribbonCount, int x, int y)
		{
			int sx = 0;

			int i = 0;
			while(i < filter_count - ribbonCount)
			{
				int nx = i == current_filter ? x - 16 : x;

				// standard bg
				batch.Draw(Catalog.MenuTexture,
					new Rectangle(nx, y, FILTER_WIDTH, FILTER_HEIGHT),
					new Rectangle(0, 24, 24, 16),
					Color.White);

				// icon
				batch.Draw(Catalog.MenuTexture, new Rectangle(nx + 6, y + 4, 24, 24), new Rectangle(sx, textureRow, 12, 12), Color.White);

				y += FILTER_HEIGHT;
				sx += 12;
				i++;
			}
			while(i < filter_count)
			{
				int nx = i == current_filter ? x - 16 : x;

				// ribbon bg
				batch.Draw(Catalog.MenuTexture,
					new Rectangle(nx, y, FILTER_WIDTH, FILTER_HEIGHT),
					new Rectangle(24, 24, 24, 16),
					Color.White);

				// icon
				batch.Draw(Catalog.MenuTexture, new Rectangle(nx + 6, y + 4, 24, 24), new Rectangle(sx, textureRow, 12, 12), Color.White);

				y += FILTER_HEIGHT;
				sx += 12;
				i++;
			}
		}
	}
}
