using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Patches;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
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
		internal static Texture2D MenuTexture;

		public enum AvailableCatalogs
		{
			Furniture = 1,
			Wallpaper = 2,
			All = 3,
		}

		public static bool TryShowCatalog(AvailableCatalogs catalogs)
		{
			MenuTexture ??= ModEntry.helper.GameContent.Load<Texture2D>(ModEntry.uiPath);

			// catalog is open
			if (ActiveMenu.Value is Catalog catalog)
				// the same or more permissive
				if ((catalog.Catalogs | catalogs) == catalog.Catalogs)
					return false;
				else
					catalog.exitThisMenuNoSound();

			// TODO keep place in closed catalog

			var menu = new Catalog(catalogs);
			Game1.onScreenMenus.Add(menu);
			ActiveMenu.Value = menu;
			Game1.isTimePaused = ModEntry.config.PauseTime;
			return true;
		}

		public readonly AvailableCatalogs Catalogs;

		private List<ScreenPage> Pages = new();
		private int tab = 0;
		private List<ClickableTextureComponent> Tabs = new();
		private ClickableTextureComponent CloseButton;

		public Catalog(AvailableCatalogs catalogs)
		{
			Catalogs = catalogs;
			if ((catalogs & AvailableCatalogs.Furniture) is not 0)
				Pages.Add(new FurniturePage());
			if ((catalogs & AvailableCatalogs.Wallpaper) is not 0)
				Pages.Add(new WallFloorPage());

			if (Pages.Count is not 1)
				for (int i = 0; i < Pages.Count; i++)
					Tabs.Add(Pages[i].GetTab());

			CloseButton = new(new(0, 0, 48, 48), Game1.mouseCursors, new(337, 494, 12, 12), 3f);

			var vp = Game1.uiViewport;
			Resize(new(vp.X, vp.Y, vp.Width, vp.Height));
			AltTex.forcePreviewDraw = true;
			AltTex.forceMenuDraw = true;

			Game1.playSound("bigSelect");
		}

		protected override void cleanupBeforeExit()
		{
			base.cleanupBeforeExit();
			AltTex.forcePreviewDraw = false;
			AltTex.forceMenuDraw = false;
			Game1.onScreenMenus.Remove(this);
			Game1.player.TemporaryItem = null;
			ActiveMenu.Value = null;
			Game1.isTimePaused = false;
			for (int i = 0;i < Pages.Count; i++)
				Pages[i].Exit();
		}
		public override void performHoverAction(int x, int y)
		{
			Pages[tab].performHoverAction(x, y);
		}
		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			base.gameWindowSizeChanged(oldBounds, newBounds);
			Resize(newBounds);
		}
		public override void draw(SpriteBatch b)
		{
			Pages[tab].draw(b);
			CloseButton.draw(b);

			for (int i = 0; i < Tabs.Count; i++)
				Tabs[i].draw(b, i == tab ? Color.White : Color.DarkGray, 0f);
		}
		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);
			Pages[tab].receiveLeftClick(x, y, playSound);

			for (int i = 0; i < Tabs.Count; i++)
			{
				if (Tabs[i].containsPoint(x, y))
				{
					if (playSound && tab != i)
						Game1.playSound("shwip");
					tab = i;
					break;
				}
			}

			if (CloseButton.containsPoint(x, y))
				exitThisMenu();
		}
		public override bool isWithinBounds(int x, int y)
		{
			for (int i = 0; i < Tabs.Count; i++)
				if (Tabs[i].containsPoint(x, y))
					return true;

			return Pages[tab].isWithinBounds(x, y) || CloseButton.containsPoint(x, y);
		}
		private void Resize(Rectangle bounds)
		{
			Rectangle region = new(32, 96, 400, bounds.Height - 160);
			for (int i = 0; i < Pages.Count; i++)
				Pages[i].Resize(region);

			int tabX = xPositionOnScreen + 96;
			int tabY = yPositionOnScreen + 16;
			for (int i = 0; i < Tabs.Count; i++)
			{
				var tabComp = Tabs[i];
				tabComp.bounds.X = tabX;
				tabX += tabComp.bounds.Width;
				tabComp.bounds.Y = tabY;
			}

			CloseButton.bounds.Location = new(40, 52);
		}

		public override void receiveScrollWheelAction(int direction)
		{
			Pages[tab].receiveScrollWheelAction(direction);
		}
	}
}
