using HappyHomeDesigner.Data;
using HappyHomeDesigner.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HappyHomeDesigner.Integration
{
	internal static class Calcifer
	{
		public static bool Active { get; private set; } = false;
		private const string CALCIFER_ID = "sophie.Calcifer";
		private const string ACTIONS_PATH = $"{CALCIFER_ID}/FurnitureActions";

		private static IAssetName ActionsName;
		private static Func<object> LoadData;

		public static IReadOnlyDictionary<string, string> CatalogueByShopName
		{
			get
			{
				if (shopFurnitureLookup is null)
					RebuildCache();
				return shopFurnitureLookup;
			}
		}

		public static IReadOnlyDictionary<string, string> ShopByCatalogueId
		{
			get
			{
				if (furnitureShopLookup is null)
					RebuildCache();
				return furnitureShopLookup;
			}
		}

		private static Dictionary<string, string> furnitureShopLookup;
		private static Dictionary<string, string> shopFurnitureLookup;
		private static readonly Dictionary<string, StyleCollection> collectionCache = [];

		internal static void Init(IModHelper helper)
		{
			// already initialized
			if (Active)
				return;

			Active = helper.ModRegistry.IsLoaded(CALCIFER_ID);

			// mod not loaded
			if (!Active)
				return;

			ActionsName = helper.GameContent.ParseAssetName(ACTIONS_PATH);
			helper.Events.Content.AssetsInvalidated += Invalidated;

			if(!ModUtilities.TryFindAssembly("Calcifer", out var asm))
			{
				ModEntry.monitor.Log("Could not find Calcifer assembly. Calcifer integration failed.", LogLevel.Warn);
				Active = false;
				return;
			}

			var type = asm.GetType("Calcifer.Calcifer.Features.FrunitureActionData");
			if (type is null)
			{
				ModEntry.monitor.Log("Could not find Calcifer data model. Calcifer integration failed.", LogLevel.Warn);
				Active = false;
				return;
			}

			LoadData = typeof(Calcifer)
				.GetMethod(nameof(LoadAsset), BindingFlags.Static | BindingFlags.NonPublic)
				.MakeGenericMethod(type)
				.CreateDelegate<Func<object>>(helper.GameContent);
		}

		public static bool TryGetCollection(string shopId, out IStyleSet collection)
		{
			if (collectionCache.TryGetValue(shopId, out var cached))
			{
				collection = cached;
				return true;
			}

			if (shopFurnitureLookup.TryGetValue(shopId, out var itemId) && ItemRegistry.GetData(itemId) is ParsedItemData itemData)
			{
				StyleCollection style = new()
				{
					DisplayName = itemData.DisplayName,
					IconTexture = itemData.GetTextureName(),
					IconSource = itemData.GetSourceRect(),
					Description = itemData.Description,
					UnlockItem = itemId
				};

				collectionCache[shopId] = style;
				collection = style;
				return true;
			}

			collection = null;
			return false;
		}

		private static void Invalidated(object sender, AssetsInvalidatedEventArgs e)
		{
			if (e.Equals(ActionsName))
			{
				shopFurnitureLookup = null;
				furnitureShopLookup = null;
				collectionCache.Clear();
			}
		}

		private static void RebuildCache()
		{
			Dictionary<string, string> furnToShop = [];
			Dictionary<string, string> shopToFurn = [];

			var shops = DataLoader.Shops(Game1.content)
				.Where(static d => d.Value.CustomFields.ContainsKey(ModEntry.MOD_ID))
				.Select(static d => d.Key)
				.ToHashSet();

			foreach(var pair in (IEnumerable<dynamic>)LoadData())
			{
				string key = pair.Key;
				string value = ((IEnumerable<dynamic>)pair.Value.TileActions)
					.FirstOrDefault(static a => a.TileAction.StartsWith("OpenShop")).TileAction;

				var split = value.Split(' ');
				if (split.Length < 2)
					continue;

				var shopId = split[1];
				if (!shops.Contains(shopId))
					continue;

				furnToShop[key] = shopId;
				shopToFurn[shopId] = key;
			}

			furnitureShopLookup = furnToShop;
			shopFurnitureLookup = shopToFurn;
		}

		private static object LoadAsset<T>(IGameContentHelper helper) where T : class
		{
			return helper.Load<T>(ActionsName);
		}
	}
}
