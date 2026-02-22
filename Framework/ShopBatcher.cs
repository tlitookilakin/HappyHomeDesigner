using HappyHomeDesigner.Data;
using HappyHomeDesigner.Integration;
using HappyHomeDesigner.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.GameData.Shops;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyHomeDesigner.Framework
{
	internal class ShopBatcher
	{
		public bool Done { get; private set; }

		private static bool inited = false;
		private static IAssetName FurnitureName;
		private static readonly Dictionary<string, Furniture> furnitureCache = [];
		private static Func<ParsedItemData, Item> CreateFurniture;

		private readonly IReadOnlyDictionary<string, ShopData> shops;
		private readonly IEnumerable<StyleCollection> collections;
		private readonly IEnumerable<string> shopsToUse;

		private readonly IEnumerator<IEnumerable<KeyValuePair<string, ItemQueryResult>>> batch;

		private static void Init()
		{
			if (inited) return;

			inited = true;
			FurnitureName = ModEntry.helper.GameContent.ParseAssetName("Data/Furniture");
			ModEntry.helper.Events.Content.AssetsInvalidated += ContentInvalidated;
			CreateFurniture = typeof(ShopBatcher).GetMethod(nameof(CreateAndCache))
				.CreateDelegate<Func<ParsedItemData, Item>>(ItemRegistry.RequireTypeDefinition("(F)"));
		}

		public ShopBatcher(params IEnumerable<string> ShopsToUse)
		{
			Init();

			shopsToUse = ShopsToUse;
			shops = DataLoader.Shops(Game1.content);
			collections = AssetManager.Collections.Values;

			var ctx = new ItemQueryContext(Game1.currentLocation, Game1.player, Game1.random, "Happy Home Designer Catalogue Menu");
			var gsq = new GameStateQueryContext(Game1.currentLocation, Game1.player, null, null, Game1.random);

			batch = shopsToUse
				.SelectMany(id => shops[id].Items, (id, val) => (id, val))
				.Where(e => e.val.Condition is null || GameStateQuery.CheckConditions(e.val.Condition, gsq))
				.SelectMany(
					(item) => GetProvider(item.val, ctx, item.id), 
					(item, result) => new KeyValuePair<string, ItemQueryResult>(item.id, result)
				)
				.TimeChunk(14)
				.GetEnumerator();
		}

		public bool DoBatch(out IDictionary<IStyleSet, List<ISalable>> items)
		{
			items = null;
			if (!batch.MoveNext())
				return false;

			items = new Dictionary<IStyleSet, List<ISalable>>();
			foreach ((var shop, var entry) in batch.Current)
			{
				if (entry.Item is not Item item)
					continue;

				bool foundCollection = false;

				foreach (var collection in collections)
				{
					if (collection.Contains(item))
					{
						foundCollection = true;

						if (!items.TryGetValue(collection, out var group))
							items[collection] = [item];
						else
							group.Add(item);
					}
				}

				if (!foundCollection)
				{
					if (Calcifer.Active && Calcifer.TryGetCollection(shop, out var style))
					{
						if (!items.TryGetValue(style, out var group))
							items[style] = [item];
						else
							group.Add(item);
					}
				}
			}

			return true;
		}

		public static IEnumerable<ItemQueryResult> GetProvider(ShopItemData data, ItemQueryContext ctx, string owner)
		{
			// use simple lazy spawning
			if (data.ItemId.StartsWith("ALL_ITEMS", StringComparison.OrdinalIgnoreCase))
			{
				var split = data.ItemId.Split(' ');
				if (split.Length < 2) // invalid
					goto Fallback;

				var category = split[1];
				IItemDataDefinition definition;

				try
				{
					definition = ItemRegistry.RequireTypeDefinition(category);
				} 
				catch (KeyNotFoundException)
				{
					ModEntry.monitor.Log(
						$"Possibly broken item query '{data.ItemId}'; type qualifier '{category}' is unknown. Found in shop entry '{owner}' -> '{data.Id}'."
					, LogLevel.Info);
					goto Fallback;
				}

				// use simple tags-only condition or no filter if possible
				var tags = data.PerItemCondition is string c && 
					c.StartsWith("ITEM_CONTEXT_TAG") && !c.Contains(',')
					? ArgUtility.SplitBySpaceQuoteAware(c) : [];

				var items = LazyResolve(
					ItemRegistry.RequireTypeDefinition(category),
					split.Contains("@isRandomSale"),
					tags,
					// use cache for furniture; other object types should be fine as-is
					category.EqualsIgnoreCase("(F)") ? CreateFurniture : definition.CreateItem
				);

				// complex condition, run full check
				if (tags.Length is 0 && data.PerItemCondition is string cond)
					items = items.Where(i => GameStateQuery.CheckConditions(cond, targetItem: i.Item as Item));

				// clip max
				if (data.MaxItems is int max)
					items = items.Take(max);

				return items;
			}

			// single furniture item
			else if (data.ItemId.StartsWith("(F)", StringComparison.OrdinalIgnoreCase))
			{
				Misc.IsLazyFurnitureContext = true;
				var item = ItemRegistry.Create(data.ItemId);
				Misc.IsLazyFurnitureContext = false;

				return [new(item)];
			}

		Fallback:
			// fallback to standard lazy
			return ItemQueryResolver.TryResolve(
				data.ItemId, ctx,
				perItemCondition: data.PerItemCondition,
				maxItems: data.MaxItems,
				avoidRepeat: data.AvoidRepeat,
				logError: static (q, e) => ModEntry.monitor.Log($"Error checking query '{q}': {e}", LogLevel.Warn)
			);
		}

		private static IEnumerable<ItemQueryResult> LazyResolve(IItemDataDefinition def, bool randomSale, string[] tags, Func<ParsedItemData, Item> factory)
		{
			var ids = def.GetAllIds();

			if (tags.Length > 2)
			{
				// cut out query and target
				tags = tags[2..];

				if (tags.Length is 1)
					ids = ids.Where(id => ItemContextTagManager.HasBaseTag(id, tags[0]));
				else
					ids = ids.Where(id => ItemContextTagManager.DoAllTagsMatch(tags, ItemContextTagManager.GetBaseContextTags(id)));
			}

			var datas = ids.Select(def.GetData);

			if (randomSale)
				datas = datas.Where(d => !d.ExcludeFromRandomSale);

			return datas
				.Select(factory)
				.Where(f => f.Name is not "ErrorItem")
				.Select(f => new ItemQueryResult(f));
		}

		private static void ContentInvalidated(object sender, AssetsInvalidatedEventArgs e)
		{
			if (e.NamesWithoutLocale.Any(name => name.Equals(FurnitureName)))
				furnitureCache.Clear();
		}

		public static Item CreateAndCache(FurnitureDataDefinition definition, ParsedItemData data)
		{
			if (furnitureCache.TryGetValue(data.ItemId, out var f))
				return f;

			Misc.IsLazyFurnitureContext = true;
			f = (Furniture)definition.CreateItem(data);
			Misc.IsLazyFurnitureContext = false;

			furnitureCache[data.ItemId] = f;
			return f;
		}
	}
}
