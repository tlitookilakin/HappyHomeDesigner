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
		private int scrollPos = 0;
		private int hCells = 0;
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
			drawTextureBox(b, xPositionOnScreen - 12, yPositionOnScreen - 12, width + 24, height + 24, Color.White);
			int count = Math.Min(entries.Count - scrollPos, height / CELL_SIZE * hCells);
			for (int i = 0; i < count; i++)
				entries[i + scrollPos].Draw(b, CELL_SIZE * (i % hCells) + xPositionOnScreen, CELL_SIZE * (i / hCells) + yPositionOnScreen);
		}
		public override void Resize(Rectangle region)
		{
			base.Resize(region);
			hCells = width / CELL_SIZE;
			width = hCells * CELL_SIZE;
			height = height / CELL_SIZE * CELL_SIZE;
		}
		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);

			int relX = x - xPositionOnScreen;
			int relY = y - yPositionOnScreen;

			// button grid
			if (relX > 0 && relX < width && relY > 0 && relY < height)
			{
				int index = relX / CELL_SIZE + hCells * (relY / CELL_SIZE) + scrollPos;
				if (index > entries.Count)
					return;

				var entry = entries[index];

				if (entry.HasVariants)
				{
					showVariantsFor(entry);
					return;
				}
				hideVariants();

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

		private void showVariantsFor(FurnitureEntry entry)
		{
			var variants = entry.GetVariants();
		}
		private void hideVariants()
		{

		}
	}
}
