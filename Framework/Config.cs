﻿using HappyHomeDesigner.Integration;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;

namespace HappyHomeDesigner.Framework
{
	public class Config
	{

		private static Texture2D logo;

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

		public Config()
		{
			logo = ModEntry.helper.ModContent.Load<Texture2D>("assets/logo.png");
			Reset();
		}

		public void Register(IGMCM gmcm, IManifest man)
		{
			gmcm.Register(man, Reset, Save);

			gmcm.AddImage(man, () => logo, logo.Bounds, 2);

			gmcm.QuickBind(man, this, nameof(CloseWithKey));
			gmcm.QuickBind(man, this, nameof(GiveModifier));
			gmcm.QuickBind(man, this, nameof(FavoriteModifier));
			gmcm.QuickBind(man, this, nameof(ExtendedCategories));
			gmcm.QuickBind(man, this, nameof(FurnitureTooltips));
			gmcm.QuickBind(man, this, nameof(PauseTime));
			gmcm.QuickBind(man, this, nameof(ReplaceFurnitureCatalog));
			gmcm.QuickBind(man, this, nameof(ReplaceWallpaperCatalog));
			gmcm.QuickBind(man, this, nameof(ReplaceRareCatalogs));
			gmcm.QuickBind(man, this, nameof(ToggleShortcut));
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
		}
		private void Save()
		{
			ModEntry.helper.WriteConfig(this);
		}
	}
}
