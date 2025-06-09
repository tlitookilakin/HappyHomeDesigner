using HappyHomeDesigner.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
		private readonly string[] preservedWallFavorites;
		private readonly string[] preservedFloorFavorites;

		private readonly GridPanel WallPanel = new(56, 140, true);
		private readonly GridPanel FloorsPanel = new(72, 72, true);
		private readonly UndoRedoButton<WallFloorState> undoRedo = new(new(0, 0, 144, 80), "undo_redo");

		private GridPanel ActivePanel;

		public WallFloorPage(IEnumerable<ISalable> items)
		{
			filter_count = 4;

			var wallFavs = new HashSet<string>(
				Game1.player.modData.TryGetValue(KeyWallFav, out var s) ? 
				s.Split('	', StringSplitOptions.RemoveEmptyEntries) :
				[]
			);

			var floorFavs = new HashSet<string>(
				Game1.player.modData.TryGetValue(KeyFloorFav, out s) ? 
				s.Split('	', StringSplitOptions.RemoveEmptyEntries) :
				[]
			);

			var knownWalls = new HashSet<string>();
			var knownFloors = new HashSet<string>();

			int removedWalls = 0;
			int removedFloors = 0;

			var timer = Stopwatch.StartNew();

			foreach (var item in items)
			{
				if (item is not Wallpaper wall)
					continue;

				if (wall.isFloor.Value)
				{
					var entry = new WallEntry(wall, floorFavs);
					if (knownFloors.Add(entry.ToString()))
					{
						floors.Add(entry);
						if (entry.Favorited)
							favoriteFloors.Add(entry);
					} else
					{
						removedFloors++;
					}
				}
				else
				{
					var entry = new WallEntry(wall, wallFavs);
					if (knownWalls.Add(entry.ToString()))
					{
						walls.Add(entry);
						if (entry.Favorited)
							favoriteWalls.Add(entry);
					} else
					{
						removedWalls++;
					}
				}
			}

			timer.Stop();
			ModEntry.monitor.Log($"Populated {floors.Count} floors and {walls.Count} walls in {timer.ElapsedMilliseconds} ms", LogLevel.Debug);

			if (removedFloors is not 0 || removedWalls is not 0)
				ModEntry.i18n.Log("logging.removedWallsAndFloors", new { walls = removedWalls, floors = removedFloors }, LogLevel.Info);
			ModEntry.monitor.Log($"removed {removedWalls} duplicate walls and {removedFloors} duplicate floors.", LogLevel.Trace);

			WallPanel.Items = walls;
			FloorsPanel.Items = floors;
			ActivePanel = WallPanel;

			preservedWallFavorites = [.. wallFavs];
			preservedFloorFavorites = [.. floorFavs];
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
					WallPanel.Items = favoriteWalls;
					FloorsPanel.Items = favoriteFloors;
				} else
				{
					WallPanel.Items = walls;
					FloorsPanel.Items = floors;
				}

				MoveButtons();

				return;
			}

			if (ActivePanel.TrySelect(x, y, out int index))
			{
				var item = ActivePanel.FilteredItems[index] as WallEntry;

				if (ModEntry.config.FavoriteModifier.IsDown())
				{
					var Favorites = (current_filter & 1) is not 0 ? favoriteFloors : favoriteWalls;

					if (item.ToggleFavorite(playSound))
						Favorites.Add(item);
					else
						Favorites.Remove(item);

					if ((current_filter >> 1) is not 0)
						ActivePanel.UpdateCount();

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
			=> new(new(0, 0, 64, 64), Catalog.MenuTexture, new(80, 24, 16, 16), 4f);

		/// <inheritdoc/>
		public override void Exit()
		{
			Game1.player.modData[KeyFloorFav] = string.Join('	', favoriteFloors) + '	' + string.Join('	', preservedFloorFavorites);
			Game1.player.modData[KeyWallFav] = string.Join('	', favoriteWalls) + '	' + string.Join('	', preservedWallFavorites);
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
