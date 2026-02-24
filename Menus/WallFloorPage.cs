using HappyHomeDesigner.Data;
using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HappyHomeDesigner.Menus
{
	internal class WallFloorPage : ScreenPage
	{
		private const string KeyWallFav = "tlitookilakin.HappyHomeDesigner/WallpaperFavorites";
		private const string KeyFloorFav = "tlitookilakin.HappyHomeDesigner/FlooringFavorites";

		private readonly List<WallEntry> walls = [];
		private readonly List<WallEntry> floors = [];
		private readonly List<WallEntry> favoriteWalls = [];
		private readonly List<WallEntry> favoriteFloors = [];
		private readonly HashSet<string> preservedWallFavorites;
		private readonly HashSet<string> preservedFloorFavorites;
		private readonly SimpleItemPool WallPool;
		private readonly SimpleItemPool FloorPool;
		private readonly GridPanel WallPanel;
		private readonly GridPanel FloorsPanel;
		private readonly UndoRedoButton<WallFloorState> undoRedo = new(new(0, 0, 144, 80), "undo_redo");
		private readonly HashSet<string> WallAndFloorIds = [];
		private int removedWalls = 0;
		private int removedFloors = 0;
		private ClickableTextureComponent Tab;

		// todo add actual storage
		public override ICollection<string> KnownIDs => WallAndFloorIds;

		private GridPanel ActivePanel;

		public WallFloorPage()
		{
			WallPool = new(() => null);
			FloorPool = new(() => null);

			WallPanel = new(WallPool, 56, 140, true);
			FloorsPanel = new(FloorPool, 72, 72, true);

			filter_count = 4;

			preservedWallFavorites = [.. DataService.GetFavoritesFor(Game1.player, KeyWallFav)];
			preservedFloorFavorites = [.. DataService.GetFavoritesFor(Game1.player, KeyFloorFav)];

			WallPool.SetItems(walls, true);
			FloorPool.SetItems(floors, true);
			ActivePanel = WallPanel;

			Tab = new(new(0, 0, 64, 64), Catalog.MenuTexture, new(80, 24, 16, 16), 4f);
			Tab.visible = false;
		}

		/// <inheritdoc/>
		public override void AppendItems(List<KeyValuePair<IStyleSet, ItemQueryResult>> Items)
		{
			bool changedWall = false;
			bool changedFloor = false;

			foreach ((_, var item) in Items)
			{
				if (item.Item is not Wallpaper wall)
					continue;

				if (wall.isFloor.Value)
				{
					var entry = new WallEntry(wall, preservedFloorFavorites);
					if (WallAndFloorIds.Add(wall.QualifiedItemId))
					{
						Tab.visible = true;
						floors.Add(entry);
						if (entry.Favorited)
							favoriteFloors.Add(entry);
						changedFloor = true;
					}
					else
					{
						removedFloors++;
					}
				}
				else
				{
					var entry = new WallEntry(wall, preservedWallFavorites);
					if (WallAndFloorIds.Add(wall.QualifiedItemId))
					{
						Tab.visible = true;
						walls.Add(entry);
						if (entry.Favorited)
							favoriteWalls.Add(entry);
						changedWall = true;
					}
					else
					{
						removedWalls++;
					}
				}
			}

			if (changedWall)
				WallPool.Update(null, true);

			if (changedFloor)
				FloorPool.Update(null, true);
		}

		/// <inheritdoc/>
		public override void FinalizeItems()
		{
			LogLoaded("wallpaper", walls.Count, removedWalls);
			LogLoaded("flooring", floors.Count, removedFloors);
		}

		/// <inheritdoc/>
		public override int Count() 
			=> Math.Max(floors.Count, walls.Count);

		public override void draw(SpriteBatch b)
		{
			ActivePanel.DrawShadow(b);
			DrawFilters(b, 16, 2, xPositionOnScreen, yPositionOnScreen);
			base.draw(b);
			undoRedo.Draw(b);
			ActivePanel.draw(b);
		}

		public override void performHoverAction(int x, int y)
		{
			base.performHoverAction(x, y);
			ActivePanel.performHoverAction(x, y);
		}

		public override void Resize(Rectangle region)
		{
			base.Resize(region);

			int button_width = 128 + 64 - 8;

			WallPanel.Resize(width - 36, height - 64, xPositionOnScreen + 55, yPositionOnScreen, button_width);
			FloorsPanel.Resize(width - 36, height - 64, xPositionOnScreen + 55, yPositionOnScreen, button_width);
			MoveButtons();
		}

		/// <summary>Adjusts positions of bottom buttons when panel changes size</summary>
		private void MoveButtons()
		{
			undoRedo.bounds = new(
				ActivePanel.width - 128 + ActivePanel.xPositionOnScreen, 
				ActivePanel.height + ActivePanel.yPositionOnScreen + GridPanel.MARGIN_BOTTOM,
				128 + (GridPanel.BORDER_WIDTH * 2), 64 + (GridPanel.BORDER_WIDTH * 2)
			);
			InventoryButton.setPosition(
				undoRedo.bounds.X - 64 + 8,
				undoRedo.bounds.Y + GridPanel.BORDER_WIDTH
			);
		}

		public override void receiveScrollWheelAction(int direction)
		{
			ActivePanel.receiveScrollWheelAction(direction);
		}

		public override void receiveRightClick(int x, int y, bool playSound = true)
		{
			ActivePanel.receiveRightClick(x, y, playSound);
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);

			if (!ActivePanel.isWithinBounds(x, y) && TrySelectFilter(x, y, playSound))
			{
				ActivePanel = (current_filter & 1) is not 0 ? FloorsPanel : WallPanel;

				if ((current_filter >> 1) is not 0)
				{
					WallPool.SetItems(favoriteWalls, false);
					FloorPool.SetItems(favoriteFloors, false);
				} else
				{
					WallPool.SetItems(walls, false);
					FloorPool.SetItems(floors, false);
				}

				MoveButtons();

				return;
			}

			if (ActivePanel.TrySelect(x, y, out int index))
			{
				var item = ActivePanel.VisibleItems.Items[index] as WallEntry;

				if (ModEntry.config.FavoriteModifier.IsDown())
				{
					var Favorites = (current_filter & 1) is not 0 ? favoriteFloors : favoriteWalls;

					if (item.ToggleFavorite(playSound))
						Favorites.Add(item);
					else
						Favorites.Remove(item);

					if ((current_filter >> 1) is not 0)
						(ActivePanel == WallPanel ? WallPool : FloorPool).Update(item, item.Favorited);

					return;
				}

				if (!ModEntry.config.GiveModifier.IsDown() && item.TryApply(playSound, out var undoState))
					undoRedo.Push(undoState);

				else if (Game1.player.addItemToInventoryBool(item.GetOne()) && playSound)
					Game1.playSound("pickUpItem");

				return;
			}

			undoRedo.recieveLeftClick(x, y, playSound);
		}

		public override bool isWithinBounds(int x, int y)
		{
			return 
				base.isWithinBounds(x, y) || 
				ActivePanel.isWithinBounds(x, y) || 
				undoRedo.containsPoint(x, y);
		}

		/// <inheritdoc/>
		public override ClickableTextureComponent GetTab()
			=> Tab;

		/// <inheritdoc/>
		public override void Exit()
		{
			DataService.SaveFavoritesFor(Game1.player, KeyFloorFav, favoriteFloors.Select(Convert.ToString).Concat(preservedFloorFavorites));
			DataService.SaveFavoritesFor(Game1.player, KeyWallFav, favoriteWalls.Select(Convert.ToString).Concat(preservedWallFavorites));
		}

		/// <inheritdoc/>
		public override bool TryApplyButton(SButton button, bool IsPressed)
		{
			// TODO add controller support

			return false;
		}

		public override void DeleteActiveItem(bool playSound)
		{
			if (Game1.player.ActiveObject is not Wallpaper wall)
				return;

			if (Game1.player.ActiveObject == Game1.player.TemporaryItem)
				Game1.player.TemporaryItem = null;
			else
				Game1.player.removeItemFromInventory(Game1.player.ActiveObject);

			if (playSound)
				Game1.playSound("trashcan");
		}
    }
}
