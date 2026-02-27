using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Integration;
using HappyHomeDesigner.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HappyHomeDesigner.Menus
{
	public class Catalog : IClickableMenu
	{
		public static readonly PerScreen<Catalog> ActiveMenu = new();
		internal static Texture2D MenuTexture;
		internal static Texture2D OverlayTexture;
		private static bool WasJustHovered = false;
		private static readonly PerScreen<Catalog> LastMenu = new();
		private static IAssetName FurnitureName;

		/// <summary>Initialize static data</summary>
		internal static void Init(IModHelper helper)
		{
			FurnitureName = helper.GameContent.ParseAssetName("Data/Furniture");

			helper.Events.Content.AssetsInvalidated += (s, e) =>
			{
				if (e.NamesWithoutLocale.Any(n => n.Equals(FurnitureName)))
					LastMenu.ResetAllScreens();
			};

			helper.Events.Player.Warped += (s, e) =>
			{
				if (e.OldLocation.Name != e.NewLocation.Name)
					LastMenu.Value = null;
			};
		}

        /// <summary>Open the catalogue with a list of shop ids</summary>
        public static void ShowCatalog(params IEnumerable<string> shops)
		{
			MenuTexture = ModEntry.helper.GameContent.Load<Texture2D>(AssetManager.UI_PATH);
			OverlayTexture = ModEntry.helper.GameContent.Load<Texture2D>(AssetManager.OVERLAY_TEXTURE);

			if (ActiveMenu.Value is Catalog catalog) 
			{
				if (catalog.Type.ContainsAll(shops))
					return;
				else
					catalog.exitThisMenuNoSound(); 
			}
			else if (LastMenu.Value is Catalog last && last.Type.ContainsAll(shops))
			{
				Game1.onScreenMenus.Insert(0, last);
				ActiveMenu.Value = last;
				Game1.playSound("bigSelect");
				return;
			}

			var watch = Stopwatch.StartNew();

			var menu = new Catalog(shops.ToHashSet(), new(shops), watch);
			Game1.onScreenMenus.Insert(0, menu);
			ActiveMenu.Value = menu;
			LastMenu.Value = menu;
			Game1.isTimePaused = ModEntry.config.PauseTime;
		}

		/// <returns>True if any menu is active on any screen, otherwise false</returns>
		internal static bool HasAnyActive()
		{
			return ActiveMenu.GetActiveValues().Where(v => v.Value is not null).Any();
		}

		/// <returns>True if the menu is on screen</returns>
		public static bool MenuVisible()
		{
			return ActiveMenu.Value != null;
		}

		internal static void UpdateGMCMButton()
		{
			var enabled = ModEntry.config.GMCMButton;
			foreach (var menu in ActiveMenu.GetActiveValues())
				menu.Value?.UpdateGMCMButton(enabled);
		}

		/// <summary>Try and do something with a button that was just pressed or released</summary>
		/// <param name="IsPressed">True if it was just pressed, false if it was just released.</param>
		/// <returns>True if the button did something and should be suppressed, otherwise false.</returns>
		public static bool TryApplyButton(SButton button, bool IsPressed)
		{
			if (ActiveMenu.Value is Catalog cat)
				return cat.TryApplyButtonImpl(button, IsPressed);

			if (ModEntry.config.OpenMenu.JustPressed())
			{
				ShowCatalog(ModUtilities.GetCollectorShops("Furniture Catalogue", "Catalogue"));
				return true;
			}

			return false;
		}

		public readonly ICollection<string> Type;

		private readonly List<ScreenPage> Pages = [];
		private readonly List<ClickableTextureComponent> Tabs = [];
		private readonly ClickableTextureComponent CloseButton;
		private ClickableTextureComponent SettingsButton;
		private readonly ClickableTextureComponent CNPButton;
		private readonly ClickableTextureComponent ToggleButton;
		private int tab = 0;
		private bool Toggled = true;
		private Point screenSize;
		private readonly InventoryWrapper PlayerInventory = new();
		private ShopBatcher batcher;
		private readonly Stopwatch timer;
		private int totalItems;

		public ICollection<string> KnownIds
				=> Pages[tab].KnownIDs;

		private Catalog(ICollection<string> ids, ShopBatcher batch, Stopwatch watch, bool playSound = true)
		{
			Type = ids;
			batcher = batch;
			timer = watch;
			AlternativeTextures.UpdateIndex();

			Pages.Add(new FurniturePage());
			Pages.Add(new WallFloorPage());
			Pages.Add(new BigObjectPage());
			Pages.Add(new ItemPage());

			foreach (var page in Pages)
				Tabs.Add(page.GetTab()); 
			
			CloseButton = new(new(0, 0, 48, 48), Game1.mouseCursors, new(337, 494, 12, 12), 3f, false);
			ToggleButton = new(new(0, 0, 48, 48), Game1.mouseCursors, new(352, 494, 12, 12), 3f, false);

			if (IGMCM.Installed && ModEntry.config.GMCMButton)
				SettingsButton = new(new(0, 0, 36, 36), MenuTexture, new(48, 97, 12, 12), 3f, true);

			CNPButton = CustomNPCPaintings.GetButton();
			if (CNPButton is not null)
				Tabs.Add(CNPButton);

			Resize(Game1.uiViewport.ToRect());

			if (playSound)
				Game1.playSound("bigSelect");

			timer.Stop();
		}

		public bool InventoryOpen
		{
			get => PlayerInventory.Visible;
			set => PlayerInventory.Visible = value;
		}

		public bool HideActiveObject
		{
			get
			{
				(int mx, int my) = Game1.getMousePosition(true);
				return PlayerInventory.Visible && PlayerInventory.isWithinBounds(mx, my);
			}
		}

		private void UpdateGMCMButton(bool enabled)
		{
			if (!IGMCM.Installed)
				return;

			if (enabled)
				SettingsButton ??= new(new(0, 0, 36, 36), MenuTexture, new(48, 97, 12, 12), 3f, true);
			else
				SettingsButton = null;
		}

		private void TickBatch()
		{
			timer.Start();
			if (batcher.DoBatch(out var data))
			{
				totalItems += data.Count;

				foreach (var page in Pages)
					page.AppendItems(data);

				timer.Stop();
				ArrangeTabs();
			}
			else
			{
				timer.Start();

				foreach (var page in Pages)
					page.FinalizeItems();

				timer.Stop();
				ModEntry.monitor.Log(
					ModEntry.i18n.Get("logging.totalTime", new {count = totalItems, time = timer.Elapsed.FormatReadable()}),
					LogLevel.Info
				);
				batcher = null;
			}
		}

		protected override void cleanupBeforeExit()
		{
			base.cleanupBeforeExit();
			Game1.onScreenMenus.Remove(this);
			Game1.player.TemporaryItem = null;
			ActiveMenu.Value = null!;
			Game1.isTimePaused = false;
			for (int i = 0;i < Pages.Count; i++)
				Pages[i].Exit();

			if (Game1.keyboardDispatcher.Subscriber is SearchBox)
				Game1.keyboardDispatcher.Subscriber = null;
		}

		public override void performHoverAction(int x, int y)
		{
			WasJustHovered = true;

			ToggleButton.tryHover(x, y);

			if (!Toggled)
				return;

			PlayerInventory.performHoverAction(x, y);

			Pages[tab].performHoverAction(x, y);
		}

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			base.gameWindowSizeChanged(oldBounds, newBounds);
			Resize(newBounds);
		}

		public override void update(GameTime time)
		{
			base.update(time);
			if (batcher != null)
				TickBatch();
		}

		public override void draw(SpriteBatch b)
		{
			var shadow = Color.Black * .4f;

			int m_x = Game1.getMouseX();
			int m_y = Game1.getMouseY();
			if (WasJustHovered && !isWithinBounds(m_x, m_y))
			{
				performHoverAction(m_x, m_y);
				WasJustHovered = false;
			}

			if (screenSize.X != Game1.uiViewport.Width || screenSize.Y != Game1.uiViewport.Height)
				Resize(Game1.uiViewport.ToRect());

			ToggleButton.draw(b);

			if (!Toggled)
				return;

			SettingsButton?.draw(b);

			for (int i = 0; i < Tabs.Count; i++)
			{
				Tabs[i].draw(b, shadow, 0f, xOffset: -4, yOffset: i == tab ? 0 : 4);
				Tabs[i].draw(b, i == tab ? Color.White : Color.DarkGray, 0f, yOffset: i == tab ? -4 : 0);
			}

			Pages[tab].draw(b);

			PlayerInventory.draw(b);
			CloseButton.draw(b);

			Pages[tab].DrawTooltip(b);
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);

			if (ToggleButton.containsPoint(x, y))
				Toggle(playSound);

			if (!Toggled)
				return;

			if (PlayerInventory.isWithinBounds(x, y))
			{
				PlayerInventory.receiveLeftClick(x, y);
				return;
			}

			Pages[tab].receiveLeftClick(x, y, playSound);
			for (int i = 0; i < Tabs.Count; i++)
			{
				if (Tabs[i].visible && Tabs[i].containsPoint(x, y) && i < Pages.Count)
				{
					if (playSound && tab != i)
						Game1.playSound("shwip");
					tab = i;
					break;
				}
			}

			if (CloseButton.containsPoint(x, y))
				exitThisMenu();

			if (SettingsButton is not null && SettingsButton.containsPoint(x, y))
			{
				IGMCM.API.OpenModMenu(ModEntry.manifest);
				if (playSound)
					Game1.playSound("bigSelect");

				// TODO remove when fixed in gmcm
				// config temp fix
				var cfg = Game1.activeClickableMenu;
				cfg.GetType().GetField("ReturnToList",
					System.Reflection.BindingFlags.Instance |
					System.Reflection.BindingFlags.NonPublic |
					System.Reflection.BindingFlags.Public
				).SetValue(cfg, () => {
					Game1.activeClickableMenu = null;
				});
			}

			if (CNPButton is not null && CNPButton.containsPoint(x, y))
				CustomNPCPaintings.ShowMenu();
		}

		public override bool isWithinBounds(int x, int y)
		{
			if (PlayerInventory.Visible && PlayerInventory.isWithinBounds(x, y))
				return true;

			if (ToggleButton.containsPoint(x, y))
				return true;

			if (!Toggled)
				return false;

			for (int i = 0; i < Tabs.Count; i++)
				if (Tabs[i].containsPoint(x, y))
					return true;

			return 
				Pages[tab].isWithinBounds(x, y) || 
				CloseButton.containsPoint(x, y) || 
				(SettingsButton is not null && SettingsButton.containsPoint(x, y));
		}

		private void ArrangeTabs()
		{
			int tabX = xPositionOnScreen + 48;
			int tabY = yPositionOnScreen - 80;
			for (int i = 0; i < Tabs.Count; i++)
			{
				var tabComp = Tabs[i];
				if (!tabComp.visible)
					continue;

				tabComp.setPosition(tabX, tabY);
				tabX += tabComp.bounds.Width;
			}
		}

		private void Resize(Rectangle bounds)
		{
			screenSize = bounds.Size;

			Rectangle region = new(32, 96, 400, bounds.Height - 160);
			if (ModEntry.ANDROID)
				region.X += 80;

			xPositionOnScreen = region.X;
			yPositionOnScreen = region.Y;
			width = region.Width;
			height = region.Height;

			for (int i = 0; i < Pages.Count; i++)
				Pages[i].Resize(region);

			ArrangeTabs();

			CloseButton.bounds.Location = new(40, 52);
			ToggleButton.bounds.Location = new(16, bounds.Height - 64);

			var currentTab = Pages[tab];

			PlayerInventory.movePosition(
				Math.Max(currentTab.width + currentTab.xPositionOnScreen + 48 + 3 + 3, (bounds.Width - PlayerInventory.width) / 2) - PlayerInventory.xPositionOnScreen, 
				(ToggleButton.bounds.Bottom - PlayerInventory.height) - PlayerInventory.yPositionOnScreen
			);

			SettingsButton?.setPosition(currentTab.xPositionOnScreen + currentTab.width, yPositionOnScreen - 64);
		}

		public override void receiveScrollWheelAction(int direction)
		{
			if (!Toggled)
				return;

			Pages[tab].receiveScrollWheelAction(direction);
		}

		public void Toggle(bool playSound)
		{
			ToggleButton.sourceRect.X = Toggled ? 365 : 352;
			Toggled = !Toggled;

			if (playSound)
				Game1.playSound("shwip");
		}

		public override void receiveRightClick(int x, int y, bool playSound = true)
		{
			PlayerInventory.receiveRightClick(x, y, playSound);
			Pages[tab].receiveRightClick(x, y, playSound);
		}

		private bool TryApplyButtonImpl(SButton button, bool IsPressed)
		{
			if (ModEntry.config.ToggleShortcut.JustPressed())
			{
				Toggle(true);
				return true;
			}

			if (!Toggled)
				return false;

			if (ModEntry.config.QuickDelete.JustPressed())
			{
				Pages[tab].DeleteActiveItem(true);
				return true;
			}

			if (ModEntry.config.CloseWithKey && Game1.activeClickableMenu is null && IsPressed)
			{
				var binds = Game1.options.menuButton;
				for (int i = 0; i < binds.Length; i++)
				{
					if ((int)binds[i].key == (int)button)
					{
						exitThisMenu();
						return true;
					}
				}
			}

			// TODO controller inputs

			return Pages[tab].TryApplyButton(button, IsPressed);
		}
	}
}
