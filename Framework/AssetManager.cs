using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Powers;
using StardewValley.GameData.Shops;
using StarModGen.Lib;
using System.ComponentModel;

namespace HappyHomeDesigner.Framework
{
	internal partial class AssetManager : INotifyPropertyChanged
	{
		public const string CARD_MAIL = MOD_ID + "_CardMail";
		public const string FAIRY_MAIL = MOD_ID + "_FairyMail";
		public const string CARD_ID = MOD_ID + "_MembershipCard";
		public const string CARD_FLAG = MOD_ID + "_IsCollectorMember";
		public const string TEXT_PATH = "Mods/" + MOD_ID + "/Strings";
		public const string CATALOGUE_ID = MOD_ID + "_Catalogue";
		public const string COLLECTORS_ID = MOD_ID + "_CollectorsCatalogue";
		public const string DELUXE_ID = MOD_ID + "_DeluxeCatalogue";
		public const string PORTABLE_ID = MOD_ID + "_HandCatalogue";
		public const string BLUEPRINT_ID = MOD_ID + "_BlueprintBook";

		private static bool IsClientMode;
		private static readonly string[] ServerRequired = 
			["Data/Furniture", "Data/Powers", "Data/Shops", "Data/Mail", "Data/Tools"];

		private static readonly string[] RareCatalogueShops = 
			["JunimoFurnitureCatalogue", "TrashFurnitureCatalogue", "RetroFurnitureCatalogue", "WizardFurnitureCatalogue", "JojaFurnitureCatalogue"];

		[Asset("LooseSprites/Book_Animation")]
		public partial Texture2D BookSpriteSheet { get; }

		[Asset("/UI")]
		public partial Texture2D MenuTexture { get; }

		[Asset("/Overlay", "textures/season_overlay")]
		public partial Texture2D OverlayTexture { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        [AssetEntry]
		public partial void Setup(IModHelper helper);

		public AssetManager(Config cfg)
		{
			IsClientMode = cfg.ClientMode;
		}

		private void ReloadIfNecessary()
		{
			if (IsClientMode == ModEntry.config.ClientMode)
				return; // no change

			IsClientMode = ModEntry.config.ClientMode;

			foreach (var item in ServerRequired)
				ModEntry.helper.GameContent.InvalidateCache(item);
		}

		[AssetEdit("Data/Mail")]
		private void AddMail(IAssetData asset)
		{
			if (ModEntry.config.ClientMode)
				return;

			if (asset.Data is Dictionary<string, string> data)
			{
				var raw = ModEntry.helper.ModContent.Load<Dictionary<string, string>>("assets/data/mail.json");

				foreach ((var key, var val) in raw)
					data[$"{MOD_ID}_{key}"] = string.Format(
						val.Replace("MOD_ID", MOD_ID),
                        ModEntry.helper.Translation.Get($"mail.{key}.text"),
                        ModEntry.helper.Translation.Get($"mail.{key}.name")
					);
			}
		}

		[AssetEdit("Data/Powers")]
		private void AddCardPower(IAssetData asset)
		{
			if (ModEntry.config.ClientMode)
				return;

			if (asset.Data is Dictionary<string, PowersData> data)
			{
				data.TryAdd(
					CARD_ID, new()
					{
						DisplayName = $"[LocalizedText {TEXT_PATH}:item.card.name]",
						Description = $"[LocalizedText {TEXT_PATH}:item.card.desc]",
						TexturePath = $"Mods/{MOD_ID}/Catalogue",
						TexturePosition = ItemRegistry.GetData(CARD_ID).GetSourceRect().Location,
						UnlockedCondition = "PLAYER_HAS_MAIL Current " + CARD_FLAG
					}
				);
			}
		}

		[AssetEdit("Data/Shops")]
		private void TagShops(IAssetData asset)
		{
			if (asset.Data is Dictionary<string, ShopData> data)
			{
				for (int i = 0; i < RareCatalogueShops.Length; i++)
					if (data.TryGetValue(RareCatalogueShops[i], out var shop))
						(shop.CustomFields ??= [])["HappyHomeDesigner/Catalogue"] = "True";

				if (!ModEntry.config.ClientMode)
				{
					if (data.TryGetValue("Carpenter", out var shop))
					{
						shop.Items.Add(new()
						{
							Id = $"{MOD_ID}_Collectors",
							ItemId = $"(F){MOD_ID}_CollectorsCatalogue",
							Condition = "PLAYER_HAS_MAIL Current " + CARD_FLAG
						});
						shop.Items.Add(new()
						{
							Id = $"{MOD_ID}_Blueprint",
							ItemId = $"(T){MOD_ID}_BlueprintBook",
							Price = 5000
						});
					}

					if (ModEntry.config.EasierTrashCatalogue && data.TryGetValue("ShadowShop", out shop))
						shop.Items.Add(new() {
							Id = MOD_ID + "_TrashCatalogue",
							ItemId = "(F)TrashCatalogue",
							Condition = "PLAYER_HEARTS Current Krobus 6",
							Price = 1000
						});
				}

				#if DEBUG

				if (data.TryGetValue("Catalogue", out var catalogue))
				{
					catalogue.Items.Add(new() {
						Id = "DEBUG_HAPPYHOME_HOUSEPLANT",
						ItemId = "(BC)7",
						Price = 0
					});

					catalogue.Items.Add(new() {
						Id = "DEBUG_HAPPYHOME_STONE",
						ItemId = "(O)390",
						Price = 0
					});
				}

				#endif
			}
		}
	}
}
