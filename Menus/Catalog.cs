using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Menus
{
	public class Catalog : IClickableMenu
	{
		public static readonly PerScreen<Catalog> ActiveMenu = new();

		public enum AvailableCatalogs
		{
			Furniture = 1,
			Wallpaper = 2,
			All = 3,
		}

		public static bool TryShowCatalog(AvailableCatalogs catalogs)
		{
			// catalog is open
			if (ActiveMenu.Value is Catalog catalog)
				// the same or more permissive
				if ((catalog.Catalogs | catalogs) == catalog.Catalogs)
					return false;
				else
					catalog.exitThisMenuNoSound();

			var menu = new Catalog(catalogs);
			Game1.onScreenMenus.Add(menu);
			ActiveMenu.Value = menu;
			return true;
		}

		public readonly AvailableCatalogs Catalogs;

		private List<ScreenPage> Pages = new();
		private int tab = 0;

		public Catalog(AvailableCatalogs catalogs)
		{
			Catalogs = catalogs;
			if ((catalogs & AvailableCatalogs.Furniture) is not 0)
				Pages.Add(new FurniturePage() { Parent = this });
			if ((catalogs & AvailableCatalogs.Wallpaper) is not 0)
				Pages.Add(new WallFloorPage() { Parent = this });

			var vp = Game1.uiViewport;
			CalculateZones(new(vp.X, vp.Y, vp.Width, vp.Height));
		}

		protected override void cleanupBeforeExit()
		{
			base.cleanupBeforeExit();
			Game1.onScreenMenus.Remove(this);
		}
		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			base.gameWindowSizeChanged(oldBounds, newBounds);
			CalculateZones(newBounds);
		}
		public override void draw(SpriteBatch b)
		{
			Pages[tab].draw(b);
		}
		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);
			Pages[tab].receiveLeftClick(x, y, playSound);
		}
		public override bool isWithinBounds(int x, int y)
		{
			return x < 500;
		}
		private void CalculateZones(Rectangle bounds)
		{
			Rectangle region = new(64, 64, 400, bounds.Height - 128);
			for(int i = 0; i < Pages.Count; i++)
			{
				Pages[i].Resize(region);
			}
		}
	}
}
