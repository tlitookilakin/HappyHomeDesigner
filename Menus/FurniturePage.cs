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

		private readonly GridPanel MainPanel = new(CELL_SIZE, CELL_SIZE);
		private readonly GridPanel VariantPanel = new(CELL_SIZE, CELL_SIZE);

		private static readonly Rectangle FrameSource = new(0, 256, 60, 60);

		public FurniturePage()
		{
			var season = Game1.player.currentLocation.GetSeasonForLocation();
			foreach (var item in Utility.getAllFurnituresForFree().Keys)
				if (item is Furniture furn)
					entries.Add(new(furn, season));
			MainPanel.Items = entries;
			VariantPanel.Items = variants;
		}
		public override void draw(SpriteBatch b)
		{
			base.draw(b);
			
			MainPanel.draw(b);

			if (variantIndex is >= 0)
			{
				int cols = MainPanel.Columns;
				DrawFrame(b, Game1.menuTexture, new(
					xPositionOnScreen + variantIndex % cols * CELL_SIZE - 8 + 32,
					yPositionOnScreen + variantIndex / cols * CELL_SIZE - 8,
					CELL_SIZE + 16, CELL_SIZE + 16),
					FrameSource, 13, 1, Color.White, 0);
			}

			//AltTex.forceMenuDraw = true;
			if (showVariants)
				VariantPanel.draw(b);
			//AltTex.forceMenuDraw = false;
		}
		public override void Resize(Rectangle region)
		{
			base.Resize(region);

			MainPanel.Resize(width - 32, height - 32, xPositionOnScreen + 32, yPositionOnScreen);
			VariantPanel.Resize(CELL_SIZE * 3 + 32, yPositionOnScreen + 256, Game1.uiViewport.Width - xPositionOnScreen - CELL_SIZE * 3 - 32, height - 256);
		}
		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);

			HandleGridClick(x, y, playSound, MainPanel, true);
			if (showVariants)
				HandleGridClick(x, y, playSound, VariantPanel, false);
		}

		private void ShowVariantsFor(FurnitureEntry entry, int index)
		{
			variantIndex = index;
			var vars = entry.GetVariants();
			variants.Clear();
			for(int i = 0; i < vars.Count; i++)
				variants.Add(new(vars[i]));
			VariantPanel.Items = variants;
			showVariants = true;
		}

		private void HideVariants()
		{
			variantIndex = -1;
			showVariants = false;
		}

		private void HandleGridClick(int mx, int my, bool playSound, GridPanel panel, bool allowVariants)
		{
			// TODO play sound

			if (panel.TrySelect(mx, my, out int index))
			{
				var entry = panel.Items[index] as FurnitureEntry;

				if (allowVariants)
				{
					if (entry.HasVariants)
					{
						ShowVariantsFor(entry, index);
						return;
					}
					HideVariants();
				}

				if (ModEntry.helper.Input.IsDown(SButton.LeftShift))
				{
					Game1.player.addItemToInventoryBool(entry.GetOne());
					return;
				}
				if (allowVariants && ModEntry.helper.Input.IsDown(SButton.LeftAlt))
				{
					// TODO add favorite
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
			return base.isWithinBounds(x, y) || (showVariants && VariantPanel.isWithinBounds(x, y));
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
