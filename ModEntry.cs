﻿using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Integration;
using HappyHomeDesigner.Menus;
using HappyHomeDesigner.Patches;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner
{
	public class ModEntry : Mod
	{
		public const string MOD_ID = "tlitookilakin.HappyHomeDesigner";

		internal static IMonitor monitor;
		internal static IManifest manifest;
		internal static IModHelper helper;
		internal static Config config;
		internal static ITranslationHelper i18n;
		internal static IAssetName uiPath;
		internal static IAssetName furnitureData;
		internal static IAssetName sprite;
		private static string whichUI = "ui";

		public override void Entry(IModHelper helper)
		{
			monitor = Monitor;
			ModEntry.helper = helper;
			i18n = Helper.Translation;
			config = Helper.ReadConfig<Config>();
			uiPath = helper.GameContent.ParseAssetName($"Mods/{ModManifest.UniqueID}/UI");
			furnitureData = helper.GameContent.ParseAssetName("Data/Furniture");
			sprite = helper.GameContent.ParseAssetName($"Mods/{ModManifest.UniqueID}/Catalogue");
			manifest = ModManifest;

			helper.Events.GameLoop.GameLaunched += Launched;
			helper.Events.Input.ButtonPressed += OnButtonPressed;
			helper.Events.Input.MouseWheelScrolled += OnMouseScroll;
			helper.Events.Player.Warped += OnWarp;

			helper.Events.Content.AssetRequested += OnAssetRequested;
		}

		private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
		{
			if (e.NameWithoutLocale.Equals(uiPath))
				e.LoadFromModFile<Texture2D>($"assets/{whichUI}.png", AssetLoadPriority.Low);
			else if (e.NameWithoutLocale.Equals(furnitureData))
				e.Edit(AddCatalogue, AssetEditPriority.Default);
			else if (e.NameWithoutLocale.Equals(sprite))
				e.LoadFromModFile<Texture2D>("assets/catalog.png", AssetLoadPriority.Low);
		}

		private void AddCatalogue(IAssetData asset)
		{
			if (asset.Data is Dictionary<string, string> data)
				data.TryAdd(
					manifest.UniqueID + "_Catalogue",
					$"Happy Home Catalogue/table/2 2/-1/1/230000/-1/{i18n.Get("furniture.Catalog.name")}/0/Mods\\{manifest.UniqueID}\\Catalogue/true"
				);
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
				if (catalog.isWithinBounds(mouse.X, mouse.Y))
				{
					catalog.receiveScrollWheelAction(-Math.Sign(e.Delta));
					e.Suppress();
				}
			}
		}

		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (!e.IsSuppressed() && config.CloseWithKey && Game1.activeClickableMenu is null)
			{
				if (Catalog.ActiveMenu.Value is Catalog cat) {

					if (config.ToggleShortcut.JustPressed())
					{
						cat.Toggle(true);
						helper.Input.Suppress(e.Button);
						return;
					}

					var binds = Game1.options.menuButton;
					for (int i = 0; i < binds.Length; i++)
					{
						if ((int)binds[i].key == (int)e.Button)
						{
							cat.exitThisMenu();
							helper.Input.Suppress(e.Button);
						}
					}
				}
			}
		}

		private void Launched(object sender, GameLaunchedEventArgs e)
		{
			if (Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu"))
			{
				IGMCM.API = Helper.ModRegistry.GetApi<IGMCM>("spacechase0.GenericModConfigMenu");
				IGMCM.Installed = true;
				config.Register(IGMCM.API, ModManifest);
			}

			whichUI =
				helper.ModRegistry.IsLoaded("Maraluna.OvergrownFloweryInterface") ?
				"ui_overgrown" :
				helper.ModRegistry.IsLoaded("ManaKirel.VintageInterface2") ?
				"ui_vintage" :
				// vanilla
				"ui";

			Patch(new(ModManifest.UniqueID));

			AlternativeTextures.Init(Helper);
			CustomFurniture.Init(Helper);
		}

		private static void Patch(Harmony harmony)
		{
			ReplaceShop.Apply(harmony);
			ItemCloneFix.Apply(harmony);
			FurnitureAction.Apply(harmony);
			InventoryCombine.Apply(harmony);
			SearchFocusFix.Apply(harmony);

			AltTex.Apply(harmony);
			// TODO rewrite patches when dga comes back
			//DGA.Apply(harmony);
		}
	}
}
