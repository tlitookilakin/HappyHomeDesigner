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
using System.IO;

namespace HappyHomeDesigner
{
	public class ModEntry : Mod
	{
		internal static IMonitor monitor;
		internal static IManifest manifest;
		internal static IModHelper helper;
		internal static Config config;
		internal static ITranslationHelper i18n;
		internal static string uiPath = "";
		private static string whichUI = "ui";

		public override void Entry(IModHelper helper)
		{
			monitor = Monitor;
			ModEntry.helper = helper;
			i18n = Helper.Translation;
			config = Helper.ReadConfig<Config>();
			uiPath = $"Mods/{ModManifest.UniqueID}/UI";
			manifest = ModManifest;

			helper.Events.GameLoop.GameLaunched += Launched;
			helper.Events.Input.ButtonPressed += OnButtonPressed;
			helper.Events.Input.MouseWheelScrolled += OnMouseScroll;
			helper.Events.Player.Warped += OnWarp;

			helper.Events.Content.AssetRequested += OnAssetRequested;
		}

		private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
		{
			if (e.NameWithoutLocale.IsEquivalentTo(uiPath))
				e.LoadFromModFile<Texture2D>($"assets/{whichUI}.png", AssetLoadPriority.Low);
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
			Patch(new(ModManifest.UniqueID));

			if (Helper.ModRegistry.IsLoaded("spacechase0.DynamicGameAssets"))
			{
				IDynamicGameAssets.API = Helper.ModRegistry.GetApi<IDynamicGameAssets>("spacechase0.DynamicGameAssets");
				IDynamicGameAssets.API.AddEmbeddedPack(ModManifest, Helper.DirectoryPath);
			}

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

			AlternativeTextures.Init(Helper);
			CustomFurniture.Init(Helper);
		}

		private static void Patch(Harmony harmony)
		{
			ItemCloneFix.Apply(harmony);
			AltTex.Apply(harmony);
			FurnitureAction.Apply(harmony);
		}
	}
}
