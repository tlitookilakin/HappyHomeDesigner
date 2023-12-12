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
	internal class WallFloorPage : ScreenPage
	{
		private readonly List<WallEntry> walls = new();
		private readonly List<WallEntry> floors = new();

		private readonly GridPanel WallPanel = new(56, 140);
		private readonly GridPanel FloorsPanel = new(72, 72);
		private GridPanel ActivePanel;

		public WallFloorPage()
		{
			foreach (var item in Utility.getAllWallpapersAndFloorsForFree().Keys)
				if (item is not Wallpaper wall)
					continue;
				else if (wall.isFloor.Value)
					floors.Add(new(wall));
				else
					walls.Add(new(wall));

			AddAltWallsOrFloors(floors, "floor");
			AddAltWallsOrFloors(walls, "wall");

			WallPanel.Items = walls;
			FloorsPanel.Items = floors;
			ActivePanel = WallPanel;
		}

		public static void AddAltWallsOrFloors(IList<WallEntry> items, string type)
		{
			// TODO add AT walls and floors
		}

		public override void draw(SpriteBatch b)
		{
			// debug
			//b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height), Color.Blue);

			ActivePanel.draw(b);
		}

		public override void Resize(Rectangle region)
		{
			base.Resize(region);

			WallPanel.Resize(width - 32, height - 32, xPositionOnScreen + 32, yPositionOnScreen + 32);
			FloorsPanel.Resize(width - 32, height - 32, xPositionOnScreen + 32, yPositionOnScreen + 32);
		}

		public override void receiveScrollWheelAction(int direction)
		{
			ActivePanel.receiveScrollWheelAction(direction);
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			if (ActivePanel.TrySelect(x, y, out int index))
			{
				var item = ActivePanel.Items[index] as WallEntry;

				if (ModEntry.helper.Input.IsDown(SButton.LeftShift) || !item.TryApply(playSound))
					if (Game1.player.addItemToInventoryBool(item.GetOne()) && playSound)
						Game1.playSound("dwop");
			}
		}

		public override bool isWithinBounds(int x, int y)
		{
			return base.isWithinBounds(x, y) || ActivePanel.isWithinBounds(x, y);
		}
	}
}
