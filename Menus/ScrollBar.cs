using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;

namespace HappyHomeDesigner.Menus
{
	public class ScrollBar
	{
		public int Rows = 0;
		public int Columns = 1;
		public int VisibleRows = 0;
		public int Offset { get; private set; }
		public int CellOffset { get; private set; }

		private int height = 1;
		private int x;
		private int y;
		private Rectangle scroller;

		private ClickableTextureComponent UpArrow = new(new(0, 0, 44, 48), Game1.mouseCursors, new(421, 459, 11, 12), 4f);
		private ClickableTextureComponent DownArrow = new(new(0, 0, 44, 48), Game1.mouseCursors, new(421, 472, 11, 12), 4f);

		private static readonly Rectangle BackgroundSource = new(403, 383, 6, 6);
		private static readonly Rectangle ThumbSource = new(435, 463, 6, 10);

		public void Draw(SpriteBatch batch)
		{
			// debug
			// batch.Draw(Game1.staminaRect, scroller, Color.Blue);

			if (VisibleRows < Rows)
			{
				UpArrow.draw(batch);
				DownArrow.draw(batch);

				if (height is >= 256)
				{
					// bar
					DrawStrip(batch, Game1.mouseCursors, BackgroundSource, scroller, 4, Color.White);
					DrawStrip(batch, Game1.mouseCursors, ThumbSource,
						new(scroller.X, scroller.Y + scroller.Height * Offset / Rows, 
							scroller.Width, Math.Max(scroller.Height * VisibleRows / Rows, ThumbSource.Height * 4)),
						4, Color.White);
				}
			}
		}

		public void AdvanceRows(int count)
		{
			if (VisibleRows >= Rows)
				Offset = 0;
			else
				Offset = Math.Clamp(Offset + count, 0, Rows - VisibleRows);
			CellOffset = Offset * Columns;
		}

		public void Resize(int height, int x, int y)
		{
			height = Math.Max(1, height);
			this.height = height;
			this.x = x;
			this.y = y;

			UpArrow.setPosition(x, y);
			DownArrow.setPosition(x, y + height - 48);
			scroller = new(x + 4, y + 52, 40, height - 108);
		}

		public void Hover(int mx, int my, bool mouseDown)
		{
			UpArrow.tryHover(mx, my);
			DownArrow.tryHover(mx, my);
		}

		public static void DrawStrip(SpriteBatch batch, Texture2D texture, Rectangle source, Rectangle dest, int scale, Color color)
		{
			// buffer is how much from each end to use as end
			// 1px gap for odd sizes and 2px gap for even sizes
			int buffer = source.Height / 2 - (1 - (source.Height & 1));
			int x = (dest.Width / 2) - (source.Width * scale / 2) + dest.X;
			x = x / scale * scale;

			batch.Draw(texture, 
				new Rectangle(x, dest.Y, source.Width * scale, buffer * scale),
				new Rectangle(source.X, source.Y, source.Width, buffer),
				color);
			batch.Draw(texture,
				new Rectangle(x, dest.Y + buffer * scale, source.Width * scale, dest.Height - buffer * scale * 2),
				new Rectangle(source.X, source.Y + buffer, source.Width, source.Height - buffer * 2),
				color);
			batch.Draw(texture,
				new Rectangle(x, dest.Y + dest.Height - buffer * scale, scale * source.Width, buffer * scale),
				new Rectangle(source.X, source.Y + source.Height - buffer, source.Width, buffer),
				color);
		}
	}
}
