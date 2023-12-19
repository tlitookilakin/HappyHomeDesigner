using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Patches;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyHomeDesigner.Menus
{
	internal class FurniturePage : ScreenPage
	{
		private const int FURNITURE_MAX = 18;

		private readonly List<FurnitureEntry> entries = new();
		private readonly List<FurnitureEntry> variants = new();
		private bool showVariants = false;
		private int variantIndex = -1;
		private readonly int iconRow;

		private readonly GridPanel MainPanel = new(CELL_SIZE, CELL_SIZE);
		private readonly GridPanel VariantPanel = new(CELL_SIZE, CELL_SIZE);
		private readonly List<FurnitureEntry>[] Filters;

		private static readonly Rectangle FrameSource = new(0, 256, 60, 60);
		private static readonly int[] ExtendedTabMap = {0, 0, 1, 1, 2, 3, 4, 5, 6, 2, 2, 3, 7, 8, 2, 9, 5, 8};
		private static readonly int[] DefaultTabMap = {1, 1, 1, 1, 0, 0, 2, 4, 4, 4, 4, 0, 3, 2, 4, 5, 4, 4};
		private const int DEFAULT_EXTENDED = 2;
		private const int DEFAULT_DEFAULT = 4;

		public FurniturePage()
		{
			int[] Map;
			int default_slot;

			if (ModEntry.config.ExtendedCategories)
			{
				Map = ExtendedTabMap;
				default_slot = DEFAULT_EXTENDED;
				iconRow = 0;
			}
			else
			{
				Map = DefaultTabMap;
				default_slot = DEFAULT_DEFAULT;
				iconRow = 8;
			}

			filter_count = Map.Max() + 1;
			Filters = new List<FurnitureEntry>[filter_count];
			for (int i = 0; i < Filters.Length; i++)
				Filters[i] = new();
			filter_count += 2;

			var season = Game1.player.currentLocation.GetSeasonForLocation();
			foreach (var item in Utility.getAllFurnituresForFree().Keys)
			{
				if (item is Furniture furn)
				{
					var entry = new FurnitureEntry(furn, season);
					var type = furn.furniture_type.Value;
					entries.Add(entry);
					if (type is < FURNITURE_MAX and >= 0)
						Filters[Map[type]].Add(entry);
					else
						Filters[default_slot].Add(entry);
				}
			}

			MainPanel.Items = entries;
			VariantPanel.Items = variants;
		}
		public override void draw(SpriteBatch b)
		{
			base.draw(b);
			DrawFilters(b, iconRow, 1, xPositionOnScreen, yPositionOnScreen);
			MainPanel.draw(b);

			if (variantIndex is >= 0)
			{
				int cols = MainPanel.Columns;
				int variantDrawIndex = variantIndex - MainPanel.Offset;
				if (variantDrawIndex >= 0 && variantDrawIndex < MainPanel.VisibleCells)
				b.DrawFrame(Game1.menuTexture, new(
					xPositionOnScreen + variantDrawIndex % cols * CELL_SIZE - 8 + 52,
					yPositionOnScreen + variantDrawIndex / cols * CELL_SIZE - 8,
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

			MainPanel.Resize(width - 36, height - 64, xPositionOnScreen + 52, yPositionOnScreen);
			VariantPanel.Resize(CELL_SIZE * 3 + 32, height - 496, Game1.uiViewport.Width - CELL_SIZE * 3 - 64, yPositionOnScreen + 256);
		}
		public override void performHoverAction(int x, int y)
		{
			MainPanel.performHoverAction(x, y);
			VariantPanel.performHoverAction(x, y);
		}
		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);

			if (TrySelectFilter(x, y, playSound))
			{
				HideVariants();
				MainPanel.Items = 
					// all items
					(current_filter is 0) ? entries :
					// categories
					(current_filter <= Filters.Length) ? Filters[current_filter - 1] :
					// favorites
					entries;
				return;
			}

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

		public override void receiveScrollWheelAction(int direction)
		{
			var pos = Game1.getMousePosition(true);
			if (MainPanel.isWithinBounds(pos.X, pos.Y))
				MainPanel.receiveScrollWheelAction(direction);
			else if (VariantPanel.isWithinBounds(pos.X, pos.Y))
				VariantPanel.receiveScrollWheelAction(direction);
		}

		private void HandleGridClick(int mx, int my, bool playSound, GridPanel panel, bool allowVariants)
		{
			panel.receiveLeftClick(mx, my, playSound);

			if (panel.TrySelect(mx, my, out int index))
			{
				var entry = panel.Items[index] as FurnitureEntry;

				if (allowVariants)
				{
					if (entry.HasVariants)
					{
						ShowVariantsFor(entry, index);
						if (playSound)
							Game1.playSound("shwip");
						return;
					}
					HideVariants();
				}


				if (allowVariants && ModEntry.config.FavoriteModifier.IsDown())
				{
					// TODO add favorite
					return;
				}
				if (ModEntry.config.GiveModifier.IsDown())
				{
					if (Game1.player.addItemToInventoryBool(entry.GetOne()) && playSound)
						Game1.playSound("pickUpItem");
					return;
				}

				if (Game1.player.ActiveObject is Furniture activeFurn && activeFurn.Price is 0)
					if (activeFurn != Game1.player.TemporaryItem)
						Game1.player.removeItemFromInventory(activeFurn);

				Game1.player.TemporaryItem = entry.GetOne();
				if (playSound)
					Game1.playSound("stoneStep");
			}
		}
		public override bool isWithinBounds(int x, int y)
		{
			return base.isWithinBounds(x, y) || 
				MainPanel.isWithinBounds(x, y) || 
				(showVariants && VariantPanel.isWithinBounds(x, y));
		}
		public override ClickableTextureComponent GetTab()
		{
			return new(new(0, 0, 64, 64), Catalog.MenuTexture, new(64, 24, 16, 16), 4f);
		}
	}
}
