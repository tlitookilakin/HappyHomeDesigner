﻿using HappyHomeDesigner.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Menus
{
	internal class WallEntry : IGridItem
	{
		public bool Favorited;

		private readonly Wallpaper item;
		private readonly Texture2D sheet;
		private readonly Rectangle region;
		private readonly int CellHeight;
		private readonly int CellWidth;
		private readonly string id;
		private readonly float Scale;

		private static readonly Rectangle background = new(128, 128, 64, 64);
		private static readonly Rectangle favRibbon = new(0, 38, 6, 6);

		public WallEntry(Wallpaper wallPaper, IList<string> favorites)
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
				id = modData.ID + ':' + item.ParentSheetIndex.ToString();
			} else
			{
				sheet = ModEntry.helper.GameContent.Load<Texture2D>("Maps/walls_and_floors");
				id = item.ParentSheetIndex.ToString();
			}

			Favorited = favorites.Contains(id);

			if (item.isFloor.Value)
			{
				region = new(item.ParentSheetIndex % 8 * 32, 336 + item.ParentSheetIndex / 8 * 32, 32, 32);
				CellHeight = 72;
				CellWidth = 72;
				Scale = 2f;

				if (modData is not null)
					region.Y -= 336;

			} else
			{
				region = new(item.ParentSheetIndex % 16 * 16, item.ParentSheetIndex / 16 * 48, 16, 44);
				CellHeight = 140;
				CellWidth = 56;
				Scale = 3f;
			}
		}

		public void Draw(SpriteBatch batch, int x, int y)
		{
			//IClickableMenu.drawTextureBox(batch, Game1.menuTexture, background, x, y, 56, CellHeight, Color.White, 1f, false);
			batch.Draw(sheet, new Vector2(x + 4, y + 4), region, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
			batch.DrawFrame(Game1.menuTexture, new(x, y, CellWidth, CellHeight), background, 4, 1, Color.White);

			if (Favorited)
				batch.Draw(Catalog.MenuTexture, new Rectangle(x + 4, y + 4, 18, 18), favRibbon, Color.White);
		}

		public bool TryApply(bool playSound)
		{
			// TODO add undo

			if (Game1.currentLocation is not DecoratableLocation where)
				return false;

			var x = Game1.player.getTileX();
			var y = Game1.player.getTileY();

			if (item.isFloor.Value)
			{
				var id = where.GetFloorID(x, y);
				if (id is null)
					return false;

				var modData = item.GetModData();
				where.SetFloor(modData is null ?
					item.ParentSheetIndex.ToString() :
					$"{modData.ID}:{item.ParentSheetIndex}",
					id);
			} else
			{
				string id = where.GetWallpaperID(x, y);
				while (id is null)
				{
					y--;
					if (y is < 0)
						return false;
					id = where.GetWallpaperID(x, y);
				}

				var modData = item.GetModData();
				where.SetWallpaper(modData is null ?
					item.ParentSheetIndex.ToString() :
					$"{modData.ID}:{item.ParentSheetIndex}",
					id);
			}
			if (playSound)
				Game1.playSound("stoneStep");
			return true;
		}
		public Wallpaper GetOne()
		{
			return item.getOne() as Wallpaper;
		}

		public bool ToggleFavorite(bool playSound)
		{
			Favorited = !Favorited;

			if (playSound)
				Game1.playSound(Favorited ? "jingle1" : "cancel");

			return Favorited;
		}
		public override string ToString()
		{
			return id;
		}
	}
}
