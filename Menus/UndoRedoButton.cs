using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Menus
{
	public partial class UndoRedoButton : ClickableComponent
	{
		public UndoRedoButton(Rectangle bounds, string name) : base(bounds, name)
		{
		}

		public UndoRedoButton(Rectangle bounds, Item item) : base(bounds, item)
		{
		}

		public UndoRedoButton(Rectangle bounds, string name, string label) : base(bounds, name, label)
		{
		}

		public void recieveLeftClick(int x, int y, bool playSound)
		{
			int relX = x - (bounds.Left + (bounds.Width - 128) / 2);
			int relY = y - (bounds.Top + (bounds.Height - 64) / 2);

			if (relY is >= 0 and <= 64)
			{
				switch (relX)
				{
					case > 128:
					case < 0:
						break;
					case <= 64:
						Undo(playSound);
						break;
					default:
						Redo(playSound);
						break;
				}
			}
		}

		public void Draw(SpriteBatch batch)
		{
			int bx = bounds.Left + (bounds.Width - 128) / 2;
			int by = bounds.Top + (bounds.Height - 64) / 2;

			// shadow
			batch.Draw(Catalog.MenuTexture,
				new Rectangle(bx - 8, by + 8, 128, 64),
				new Rectangle(96, 24, 32, 16),
				Color.Black * .4f
			);

			// bg
			batch.Draw(Catalog.MenuTexture,
				new Rectangle(bx, by, 128, 64),
				new Rectangle(96, 24, 32, 16),
				Color.White
			);

			// undo
			batch.Draw(Catalog.MenuTexture,
				new Rectangle(bx, by, 64, 64),
				new Rectangle(96, 40, 16, 16),
				backwards.Count is not 0 ? Color.White : Color.White * .4f
			);

			// redo
			batch.Draw(Catalog.MenuTexture,
				new Rectangle(bx + 64, by, 64, 64),
				new Rectangle(112, 40, 16, 16),
				forwards.Count is not 0 ? Color.White : Color.White * .4f
			);
		}
	}
}
