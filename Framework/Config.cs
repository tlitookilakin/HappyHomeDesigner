using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StarModGen.Lib;
using StarModGen.Utils;

namespace HappyHomeDesigner.Framework
{
	[Config(false)]
	public partial class Config
	{
		private static string[] skins;
		private static string[] skinFiles;
		private int skindex;
		private static Texture2D logo;

		internal string UiName => skindex > 0 ? skinFiles[skindex - 1] : GetAutoSkin();

		[ConfigValue(true, "controls")]
		public bool CloseWithKey { get; set; }

		[ConfigValue(SButton.LeftShift, "controls")]
		public KeybindList GiveModifier { get; set; }

		[ConfigValue(SButton.LeftControl, "controls")]
		public KeybindList FavoriteModifier { get; set; }

		[ConfigValue(true)]
		public bool ExtendedCategories { get; set; }

		[ConfigValue(false, "tweaks")]
		public bool FurnitureTooltips { get; set; }

		[ConfigValue(true)]
		public bool PauseTime { get; set; }

		[ConfigValue(true, "tweaks")]
		public bool ReplaceFurnitureCatalog { get; set; }

        [ConfigValue(true, "tweaks")]
        public bool ReplaceWallpaperCatalog { get; set; }

        [ConfigValue(true, "tweaks")]
        public bool ReplaceRareCatalogs { get; set; }

        [ConfigValue(SButton.None, "controls")]
        public KeybindList ToggleShortcut { get; set; }

		[ConfigValue(false, "controls")]
		public bool AlwaysLockScroll { get; set; }

		[ConfigValue(false)]
		public bool ClientMode { get; set; }

		[ConfigValue(false, "cheats")]
		public bool EarlyDeluxe { get; set; }

		[ConfigValue(false, "tweaks")]
		public bool LargeVariants { get; set; }

		[ConfigValue(SButton.None, "cheats")]
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

		[ConfigValue(true, "tweaks")]
		public bool EasierTrashCatalogue { get; set; }

		[ConfigValue(false)]
		public bool Magnify { get; set; }

		[ConfigValue(2f, "tweaks")]
		[ConfigRange(Max = 5f, Min = 1f, Step = .5f)]
		public partial float MagnifyScale { get; set; }

		[ConfigValue(true, "tweaks")]
		public bool GMCMButton { get; set; }

		[ConfigValue(SButton.Delete, "controls")]
		public KeybindList QuickDelete { get; set; }

		[ConfigValue(true)]
		public bool PickupCraftables { get; set; }

		[ConfigValue(true, "tweaks")]
		public bool ExpandSearch { get; set; }

		[ConfigValue(true, "tweaks")]
		public bool SeasonalOverlay { get; set; }

		[ConfigValue(false, "tweaks")]
		public bool DisableBlueprintChecks { get; set; }

		internal static Config Init(IModHelper h, IManifest man)
        {
            logo = ModEntry.helper.ModContent.Load<Texture2D>("assets/logo.png");
            Registering += OnRegistering;
            Reset += OnReset;
			LoadSkins();
			return Create(h, man);
		}

        private static void OnReset(Config cfg)
        {
			cfg.UiSkin = "Auto";
        }

        private static void OnRegistering(object sender, IGMCMApi e)
        {
			e.AddImage(ModEntry.manifest, () => logo, logo.Bounds, 2); 
			e.QuickPage(ModEntry.manifest, "tweaks");
            e.QuickBind(ModEntry.manifest, sender, nameof(UiSkin),
                allowedValues: skins,
                formatValue: s => ModEntry.i18n.Get($"skin.{s}")
            );
        }

		private static void LoadSkins()
		{
			var skinData = ModEntry.helper.ModContent.Load<Dictionary<string, string>>("assets/data/recolors.json");
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
