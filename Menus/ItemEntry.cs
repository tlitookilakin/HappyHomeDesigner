using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;

namespace HappyHomeDesigner.Menus
{
	internal class ItemEntry : IGridItem
	{
		private const int CELL_SIZE = 80;
		private static readonly Rectangle background = new(128, 128, 64, 64);

		public Item item;

		public bool Hovered
		{
			get => hovered;
			set
			{
				if (value == hovered)
					return;

				hovered = value;
				hoverChangedTick = Game1.ticks - (7 - Math.Min(Game1.ticks - hoverChangedTick, 7));
			}
		}
		private bool hovered;
		private int hoverChangedTick;

		public ItemEntry(Item item)
		{
			this.item = item;
		}

		public void DrawBackground(SpriteBatch batch, int x, int y)
		{
			IClickableMenu.drawTextureBox(batch, Game1.menuTexture, background, x, y, CELL_SIZE, CELL_SIZE, Color.White, 1f, false);
		}

		public void Draw(SpriteBatch b, int x, int y)
		{
			float scale = Math.Clamp(Game1.ticks - hoverChangedTick, 0, 7) / 7f;
			if (!hovered)
				scale = 1f - scale;

			item.drawInMenu(b, new(x + 8, y + 8), 1f + (scale * .3f));
		}

		/// <inheritdoc/>
		public string GetName()
		{
			return item.DisplayName;
		}

		public override string ToString()
		{
			return item.DisplayName + '|' + item.ItemId;
		}

		public bool ToggleFavorite(bool playSound)
		{
			return false;
		}
	}
}
