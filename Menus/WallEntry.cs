﻿using HappyHomeDesigner.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHomeDesigner.Menus
{
	internal class WallEntry : IGridItem
	{
		private readonly Wallpaper item;
		private readonly Texture2D sheet;
		private readonly Rectangle region;
		private readonly int CellHeight;
		private readonly int CellWidth;
		private readonly float Scale;

		private static readonly Rectangle background = new(128, 128, 64, 64);

		public WallEntry(Wallpaper wallPaper)
		{
			item = wallPaper;

			var modData = item.GetModData();
			if (modData is not null)
			{
				try{
					sheet = ModEntry.helper.GameContent.Load<Texture2D>(modData.Texture);
				} catch (Exception) {
					sheet = ModEntry.helper.GameContent.Load<Texture2D>("Maps/walls_and_floors");
				}
			} else
			{
				sheet = ModEntry.helper.GameContent.Load<Texture2D>("Maps/walls_and_floors");
			}

			if (item.isFloor.Value)
			{
				region = new(item.ParentSheetIndex % 8 * 32, 336 + item.ParentSheetIndex / 8 * 32, 32, 32);
				CellHeight = 72;
				CellWidth = 72;
				Scale = 2f;
			} else
			{
				region = new(item.ParentSheetIndex % 16 * 16, item.ParentSheetIndex / 16 * 48, 16, 44);
				CellHeight = 140;
				CellWidth = 56;
				Scale = 3f;
			}
		}
		public WallEntry(string source, int index) : this(new Wallpaper(source, index)) { }

		public void Draw(SpriteBatch batch, int x, int y)
		{
			//IClickableMenu.drawTextureBox(batch, Game1.menuTexture, background, x, y, 56, CellHeight, Color.White, 1f, false);
			batch.Draw(sheet, new Vector2(x + 4, y + 4), region, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
			batch.DrawFrame(Game1.menuTexture, new(x, y, CellWidth, CellHeight), background, 4, 1, Color.White);
		}
	}
}