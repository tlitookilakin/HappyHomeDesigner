using HappyHomeDesigner.Integration;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Framework
{
	public class Config
	{
		private static string[] skins;
		private static string[] skinFiles;
		private int skindex;
		private static Texture2D logo;

		internal string UiName => skindex > 0 ? skinFiles[skindex - 1] : GetAutoSkin();

		public bool CloseWithKey { get; set; }
		public KeybindList GiveModifier { get; set; }
		public KeybindList FavoriteModifier { get; set; }
		public bool ExtendedCategories { get; set; }
		public bool FurnitureTooltips { get; set; }
		public bool PauseTime { get; set; }
		public bool ReplaceFurnitureCatalog { get; set; }
		public bool ReplaceWallpaperCatalog { get; set; }
		public bool ReplaceRareCatalogs { get; set; }
		public KeybindList ToggleShortcut { get; set; }
		public bool AlwaysLockScroll { get; set; }
		public bool ClientMode { get; set; }
		public bool EarlyDeluxe { get; set; }
		public bool LargeVariants { get; set; }
		public KeybindList OpenMenu { get; set; }
		public string UiSkin
		{
			get => skins[skindex];
			set
			{
				skindex = Array.IndexOf(skins, value);
				if (skindex is -1)
					skindex = 0;
			}
		}
		public bool EasierTrashCatalogue { get; set; }

		public Config()
		{
			logo = ModEntry.helper.ModContent.Load<Texture2D>("assets/logo.png");
			LoadSkins();
			Reset();
		}

		public void Register(IGMCM gmcm, IManifest man)
		{
			gmcm.Register(man, Reset, Save);

			gmcm.AddImage(man, () => logo, logo.Bounds, 2);

			gmcm.QuickBind(man, this, nameof(GiveModifier));
			gmcm.QuickBind(man, this, nameof(FavoriteModifier));
			gmcm.QuickBind(man, this, nameof(ExtendedCategories));
			gmcm.QuickBind(man, this, nameof(PauseTime));
			gmcm.QuickBind(man, this, nameof(ToggleShortcut));
			gmcm.QuickBind(man, this, nameof(ClientMode), true);
			gmcm.QuickBind(man, this, nameof(EasierTrashCatalogue));

			gmcm.QuickPage(man, "tweaks");
			gmcm.QuickBind(man, this, nameof(UiSkin),
				allowedValues: skins,
				formatValue: s => ModEntry.i18n.Get($"skin.{s}")
			);
			gmcm.QuickBind(man, this, nameof(CloseWithKey));
			gmcm.QuickBind(man, this, nameof(FurnitureTooltips));
			gmcm.QuickBind(man, this, nameof(AlwaysLockScroll));
			gmcm.QuickBind(man, this, nameof(LargeVariants));
			gmcm.QuickBind(man, this, nameof(ReplaceFurnitureCatalog));
			gmcm.QuickBind(man, this, nameof(ReplaceWallpaperCatalog));
			gmcm.QuickBind(man, this, nameof(ReplaceRareCatalogs));

			gmcm.QuickPage(man, "cheats");
			gmcm.QuickBind(man, this, nameof(EarlyDeluxe));
			gmcm.QuickBind(man, this, nameof(OpenMenu));
		}

		private void Reset()
		{
			CloseWithKey = true;
			GiveModifier = new(SButton.LeftShift);
			FavoriteModifier = new(SButton.LeftControl);
			ExtendedCategories = true;
			FurnitureTooltips = true;
			PauseTime = true;
			ReplaceFurnitureCatalog = true;
			ReplaceWallpaperCatalog = true;
			ReplaceRareCatalogs = true;
			ToggleShortcut = new(SButton.None);
			AlwaysLockScroll = false;
			ClientMode = false;
			EarlyDeluxe = false;
			LargeVariants = false;
			OpenMenu = new(SButton.None);
			UiSkin = "Auto";
			EasierTrashCatalogue = true;
		}

		private void Save()
		{
			ModEntry.helper.WriteConfig(this);
			ModEntry.helper.GameContent.InvalidateCache(AssetManager.UI_PATH);

			if (Game1.gameMode is Game1.titleScreenGameMode)
				AssetManager.ReloadIfNecessary();
		}

		private static void LoadSkins()
		{
			var skinData = ModEntry.helper.ModContent.Load<Dictionary<string, string>>("assets/recolors.json");
			skinFiles = [.. skinData.Values];
			skins = ["Auto", .. skinData.Keys];
		}

		private static string GetAutoSkin()
		{
			string defaultName = "ui";
			for (int i = 1; i < skins.Length; i++)
			{
				string id = skins[i];
				if (id is "Default")
					defaultName = skinFiles[i - 1];
				else if (ModEntry.helper.ModRegistry.IsLoaded(id))
					return skinFiles[i - 1];
			}
			return defaultName;
		}
	}
}
