using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
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

		public void Draw(SpriteBatch batch, int x, int y, int height)
		{
			batch.Draw(Game1.staminaRect, new Rectangle(x, y, 32, height), Color.Blue);
		}

		public void AdvanceRows(int count)
		{
			Offset = Math.Clamp(Offset + count, 0, Rows - VisibleRows);
			CellOffset = Offset * Columns;
		}

		public void Resize()
		{

		}
	}
}
