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

		private int scrollPos = 0;
		private int variantScrollPos = 0;
		private int hCells = 0;

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
			DrawItemPanel(b, entries, xPositionOnScreen, yPositionOnScreen, scrollPos);

			//AltTex.forceMenuDraw = true;
			if (showVariants)
				DrawItemPanel(b, variants, Game1.uiViewport.Width - width - xPositionOnScreen, yPositionOnScreen, variantScrollPos);
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

			HandleGridClick(x, y, playSound, xPositionOnScreen, yPositionOnScreen, entries, scrollPos, true);
			if (showVariants)
				HandleGridClick(x, y, playSound, Game1.uiViewport.Width - xPositionOnScreen - width, yPositionOnScreen, variants, variantScrollPos, false);
		}

		private void showVariantsFor(FurnitureEntry entry)
		{
			var vars = entry.GetVariants();
			variants.Clear();
			for(int i = 0; i < vars.Count; i++)
				variants.Add(new(vars[i]));
			showVariants = true;
		}
		private void hideVariants()
		{
			showVariants = false;
		}

		private void DrawItemPanel(SpriteBatch b, List<FurnitureEntry> items, int x, int y, int offset)
		{
			drawTextureBox(b, x - 12, y - 12, width + 24, height + 24, Color.White);
			int count = Math.Min(items.Count - scrollPos, height / CELL_SIZE * hCells);
			for (int i = 0; i < count; i++)
				items[i + offset].Draw(b, CELL_SIZE * (i % hCells) + x, CELL_SIZE * (i / hCells) + y);
		}
		private void HandleGridClick(int mx, int my, bool playSound, int x, int y, List<FurnitureEntry> items, int offset, bool allowVariants)
		{
			int relX = mx - x;
			int relY = my - y;

			if (relX > 0 && relX < width && relY > 0 && relY < height)
			{
				int index = relX / CELL_SIZE + hCells * (relY / CELL_SIZE) + offset;
				if (index > items.Count)
					return;

				var entry = items[index];

				if (allowVariants)
				{
					if (entry.HasVariants)
					{
						showVariantsFor(entry);
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
	}
}
