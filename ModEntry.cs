using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Integration;
using HappyHomeDesigner.Menus;
using HappyHomeDesigner.Patches;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace HappyHomeDesigner
{
	// TODO add blueprint book item
	// TODO add blueprint book UI

	public class ModEntry : Mod
	{
		public const string MOD_ID = "tlitookilakin.HappyHomeDesigner";

		internal static IMonitor monitor;
		internal static IManifest manifest;
		internal static IModHelper helper;
		internal static Config config;
		internal static ITranslationHelper i18n;

		public override void Entry(IModHelper helper)
		{
			monitor = Monitor;
			ModEntry.helper = helper;
			i18n = Helper.Translation;
			config = Helper.ReadConfig<Config>();
			manifest = ModManifest;

			helper.Events.GameLoop.GameLaunched += Launched;
			helper.Events.Input.ButtonPressed += OnButtonPressed;
			helper.Events.Input.ButtonReleased += OnButtonReleased;
			helper.Events.Input.MouseWheelScrolled += OnMouseScroll;
			helper.Events.Player.Warped += OnWarp;

			AssetManager.Init(Helper);
			InventoryWatcher.Init(Helper);
		}

		private void OnWarp(object sender, WarpedEventArgs e)
		{
			if (Catalog.ActiveMenu.Value is Catalog catalog)
				catalog.exitThisMenuNoSound();
		}

		private void OnMouseScroll(object sender, MouseWheelScrolledEventArgs e)
		{
			if (e.Delta is not 0 && Catalog.ActiveMenu.Value is Catalog catalog)
			{
				var mouse = Game1.getMousePosition(true);
				if (config.AlwaysLockScroll || catalog.isWithinBounds(mouse.X, mouse.Y))
				{
					catalog.receiveScrollWheelAction(-Math.Sign(e.Delta));
					e.Suppress();
				}
			}
		}

		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (!e.IsSuppressed() && Game1.activeClickableMenu is null && Catalog.TryApplyButton(e.Button, true))
				e.Button.Suppress();

			#if DEBUG
			if (e.Button is SButton.Home && Game1.currentLocation is GameLocation where)
				Game1.activeClickableMenu = new BlueprintMenu(where);
			#endif
		}

		private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
		{
			if (!e.IsSuppressed() && Game1.activeClickableMenu is null && Catalog.TryApplyButton(e.Button, false))
				e.Button.Suppress();
		}

		private void Launched(object sender, GameLaunchedEventArgs e)
		{
			if (Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu"))
			{
				IGMCM.API = Helper.ModRegistry.GetApi<IGMCM>("spacechase0.GenericModConfigMenu");
				IGMCM.Installed = true;
				config.Register(IGMCM.API, ModManifest);
			}

			Patch(new(ModManifest.UniqueID));

			AlternativeTextures.Init(Helper);
			CustomNPCPaintings.Init();
			Spacecore.Init();
		}

		private static void Patch(Harmony harmony)
		{
			ReplaceShop.Apply(harmony);
			ItemCloneFix.Apply(harmony);
			FurnitureAction.Apply(harmony);
			InventoryCombine.Apply(harmony);
			SearchFocusFix.Apply(harmony);
			ItemReceive.Apply(harmony);
			HandCatalogue.Apply(harmony);
			CatalogFX.Apply(harmony);
			Misc.Apply(harmony);
			CraftablePlacement.Apply(harmony);
			AltTex.Apply(harmony);
		}
	}
}
