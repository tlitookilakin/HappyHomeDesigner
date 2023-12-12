using HappyHomeDesigner.Patches;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Menus
{
    internal class FurniturePage : ScreenPage
	{
		internal const int CELL_SIZE = 80;

		private readonly List<FurnitureEntry> entries = new();
		private readonly List<FurnitureEntry> variants = new();
		private bool showVariants = false;
		private int variantIndex = -1;

		private int scrollPos = 0;
		private int variantScrollPos = 0;
		private int hCells = 0;
		private int varCells = 3;

		private Rectangle RegionMain;
		private Rectangle RegionVariant;
		public FurniturePage()
		{
			var season = Game1.player.currentLocation.GetSeasonForLocation();
			foreach (var item in Utility.getAllFurnituresForFree().Keys)
				if (item is Furniture furn)
					entries.Add(new(furn, season));
		}
		public override void draw(SpriteBatch b)
		{
			base.draw(b);
			DrawItemPanel(b, entries, xPositionOnScreen, yPositionOnScreen, scrollPos, hCells, height);

			if (variantIndex is >= 0)
			{
				DrawFrame(b, Game1.menuTexture, new(
					xPositionOnScreen + variantIndex % hCells * CELL_SIZE - 8,
					yPositionOnScreen + variantIndex / hCells * CELL_SIZE - 8,
					CELL_SIZE + 16, CELL_SIZE + 16),
					new(0, 256, 60, 60), 13, 1, Color.White, 0);
			}

			//AltTex.forceMenuDraw = true;
			if (showVariants)
				DrawItemPanel(b, variants, 
					Game1.uiViewport.Width - xPositionOnScreen - (varCells * CELL_SIZE + 24),
					yPositionOnScreen + 256, variantScrollPos, varCells, height - 256);
			//AltTex.forceMenuDraw = false;
		}
		public override void Resize(Rectangle region)
		{
			base.Resize(region);
			hCells = width / CELL_SIZE;
			width = hCells * CELL_SIZE;
			height = height / CELL_SIZE * CELL_SIZE;

			RegionMain = new(xPositionOnScreen - 32, yPositionOnScreen, width + 64, height + 32);
			RegionVariant = new(Game1.uiViewport.Width - xPositionOnScreen - width - 32, yPositionOnScreen, width + 32, height);
		}
		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);

			HandleGridClick(x, y, playSound, xPositionOnScreen, yPositionOnScreen, entries, scrollPos, true, hCells, height);
			if (showVariants)
				HandleGridClick(x, y, playSound, 
					Game1.uiViewport.Width - xPositionOnScreen - (varCells * CELL_SIZE + 24), 
					yPositionOnScreen + 256, variants, variantScrollPos, false, varCells, height - 256);
		}

		private void showVariantsFor(FurnitureEntry entry, int index)
		{
			variantIndex = index;
			var vars = entry.GetVariants();
			variants.Clear();
			for(int i = 0; i < vars.Count; i++)
				variants.Add(new(vars[i]));
			showVariants = true;
		}
		private void hideVariants()
		{
			variantIndex = -1;
			showVariants = false;
		}

		private void DrawItemPanel(SpriteBatch b, List<FurnitureEntry> items, int x, int y, int offset, int cellsW, int h)
		{
			drawTextureBox(b, Game1.mouseCursors, new(384, 373, 18, 18), x - 16, y - 20, cellsW * CELL_SIZE + 32, h + 36, Color.White, 4f);
			int count = Math.Min(items.Count - offset, h / CELL_SIZE * cellsW);
			for (int i = 0; i < count; i++)
				items[i + offset].Draw(b, CELL_SIZE * (i % cellsW) + x, CELL_SIZE * (i / cellsW) + y);
		}
		private void HandleGridClick(int mx, int my, bool playSound, int x, int y, List<FurnitureEntry> items, int offset, bool allowVariants, int cellsW, int h)
		{
			int relX = mx - x;
			int relY = my - y;

			if (relX > 0 && relX < cellsW * CELL_SIZE && relY > 0 && relY < h)
			{
				int index = relX / CELL_SIZE + cellsW * (relY / CELL_SIZE) + offset;
				if (index > items.Count)
					return;

				var entry = items[index];

				if (allowVariants)
				{
					if (entry.HasVariants)
					{
						showVariantsFor(entry, index);
						return;
					}
					hideVariants();
				}

				if (ModEntry.helper.Input.IsDown(SButton.LeftShift))
				{
					Game1.player.addItemToInventoryBool(entry.GetOne());
					return;
				}

				if (Game1.player.ActiveObject is Furniture activeFurn && activeFurn.Price is 0)
					if (activeFurn != Game1.player.TemporaryItem)
						Game1.player.removeItemFromInventory(activeFurn);

				Game1.player.TemporaryItem = entry.GetOne();
			}
		}
		public override bool isWithinBounds(int x, int y)
		{
			if (RegionMain.Contains(x, y))
				return true;
			if (showVariants && RegionVariant.Contains(x, y))
				return true;
			return false;
		}
		private static void DrawFrame(SpriteBatch b, Texture2D texture, Rectangle dest, Rectangle source, int padding, int scale, Color color, int top = 0)
		{
			int destPad = padding * scale;
			int dTop = top * scale + destPad;
			int sTop = top + padding;

			// top
			int dy = dest.Y;
			int sy = source.Y;
			b.Draw(texture, 
				new Rectangle(dest.X, dy, destPad, dTop), 
				new Rectangle(source.X, sy, padding, sTop), 
				color);
			b.Draw(texture,
				new Rectangle(dest.X + destPad, dy, dest.Width - destPad * 2, dTop),
				new Rectangle(source.X + padding, sy, source.Width - padding * 2, sTop),
				color);
			b.Draw(texture,
				new Rectangle(dest.X + dest.Width - destPad, dy, destPad, dTop),
				new Rectangle(source.X + source.Width - padding, sy, padding, sTop),
				color
				);

			// mid
			dy += dTop;
			sy += sTop;
			b.Draw(texture,
				new Rectangle(dest.X, dy, destPad, dest.Height - destPad * 2),
				new Rectangle(source.X, sy, padding, source.Height - padding * 2),
				color);
			b.Draw(texture,
				new Rectangle(dest.X + dest.Width - destPad, dy, destPad, dest.Height - destPad * 2),
				new Rectangle(source.X + source.Width - padding, sy, padding, source.Height - padding * 2),
				color
				);

			// bottom
			dy = dest.Y + dest.Height - destPad;
			sy = source.Y + source.Height - padding;
			b.Draw(texture,
				new Rectangle(dest.X, dy, destPad, destPad),
				new Rectangle(source.X, sy, padding, padding),
				color);
			b.Draw(texture,
				new Rectangle(dest.X + destPad, dy, dest.Width - destPad * 2, destPad),
				new Rectangle(source.X + padding, sy, source.Width - padding * 2, padding),
				color);
			b.Draw(texture,
				new Rectangle(dest.X + dest.Width - destPad, dy, destPad, destPad),
				new Rectangle(source.X + source.Width - padding, sy, padding, padding),
				color
				);
		}
	}
}
