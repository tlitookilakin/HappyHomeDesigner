using HappyHomeDesigner.Data;
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
		private Texture2D sheet;
		private readonly Rectangle region;
		private readonly int CellHeight;
		private readonly int CellWidth;
		private readonly string id;
		private readonly float Scale;

		private static readonly Rectangle background = new(128, 128, 64, 64);
		private static readonly Rectangle favRibbon = new(0, 38, 6, 6);

		public WallEntry(Wallpaper wallPaper, ICollection<string> favorites)
		{
			item = wallPaper;

			var modData = item.GetSetData();

			id = modData is not null ?
				modData.Id + ':' + item.ParentSheetIndex.ToString() :
				item.ParentSheetIndex.ToString();

			Favorited = favorites.Remove(id);

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

		public void DrawBackground(SpriteBatch batch, int x, int y)
		{
			IClickableMenu.drawTextureBox(batch, Game1.menuTexture, background, x, y, CellWidth, CellHeight, Color.White, 1f, false);
		}

		public void Draw(SpriteBatch batch, int x, int y)
		{
			// defer texture load to prevent Lag Spike Of Doom
			sheet ??= GetTexture();

			batch.Draw(sheet, new Vector2(x + 4, y + 4), region, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);

			if (Favorited)
				batch.Draw(Catalog.MenuTexture, new Rectangle(x + 4, y + 4, 18, 18), favRibbon, Color.White);
		}

		/// <summary>Attempts to apply the wall/floor to the current room</summary>
		/// <param name="playSound">Whether or not to play sounds</param>
		/// <param name="state">if applied, the undo-redo state item that represents this operation</param>
		/// <returns>True if applied, otherwise false</returns>
		public bool TryApply(bool playSound, out WallFloorState state)
		{
			state = default;

			if (Game1.currentLocation is not DecoratableLocation where)
				return false;

			(var x, var y) = Game1.player.TilePoint;

			if (item.isFloor.Value)
			{
				var id = where.GetFloorID(x, y);
				if (id is null)
					return false;

				var existing = where.appliedFloor.TryGetValue(id, out var xid) ? xid : "0";

				var modData = item.GetSetData();
				var name = modData is null ?
					item.ParentSheetIndex.ToString() :
					$"{modData.Id}:{item.ParentSheetIndex}";

				if (existing == name)
					return false;

				where.SetFloor(name, id);

				state = new() { area = id, isFloor = true, old = existing, which = name};
			} else
			{
				string id = where.GetWallpaperID(x, y);
				string floor = where.GetFloorID(x, y);
				while (id is null)
				{
					y--;
					if (y is < 0)
						return false;

					// detect room boundary, search for adjacent walls
					if (floor != null && where.GetFloorID(x, y) != floor && FindAdjacentWalls(x, y - 1, where, floor) is string s)
					{
						id = s;
						break;
					}

					id = where.GetWallpaperID(x, y);
				}

				var existing = where.appliedWallpaper.TryGetValue(id, out var xid) ? xid : "0";

				var modData = item.GetSetData();
				var name = modData is null ?
					item.ParentSheetIndex.ToString() :
					$"{modData.Id}:{item.ParentSheetIndex}";

				if (existing == name)
					return true;

				where.SetWallpaper(name, id);

				state = new() { area = id, isFloor = false, old = existing, which = name };
			}

			if (playSound)
				Game1.playSound("stoneStep");

			return true;
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public string GetName()
		{
			return id;
		}

		/// <returns>The texture used by this wall/floor</returns>
		private Texture2D GetTexture()
		{
			var modData = item.GetSetData();
			if (modData is not null)
			{
				try
				{
					return ModEntry.helper.GameContent.Load<Texture2D>(modData.Texture);
				}
				catch (Exception)
				{
					return ModEntry.helper.GameContent.Load<Texture2D>("Maps/walls_and_floors");
				}
			}
			return ModEntry.helper.GameContent.Load<Texture2D>("Maps/walls_and_floors");
		}

		private static string FindAdjacentWalls(int x, int y, DecoratableLocation where, string originalFloor)
		{
			Queue<int> toCheck = [];
			toCheck.Enqueue(x);
			int c = x;
			int w = where.Map.GetLayer("Back").LayerWidth - 1;

			while (toCheck.TryDequeue(out x))
			{
				if (originalFloor != where.GetFloorID(x, y + 2))
					continue;

				if (where.GetWallpaperID(x, y) is string s)
					return s;

				if (x > 0 && x <= c)
					toCheck.Enqueue(x - 1);

				if (x < w && x >= c)
					toCheck.Enqueue(x + 1);
			}

			return null;
		}
	}
}
