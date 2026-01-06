using HappyHomeDesigner.Data;
using HappyHomeDesigner.Integration;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.GameData.Shops;
using StardewValley.Internal;
using System.Collections.Generic;
using System.Linq;

namespace HappyHomeDesigner.Framework
{
	internal class ShopBatcher
	{
		public bool Done { get; private set; }

		private readonly IReadOnlyDictionary<string, ShopData> shops;
		private readonly IEnumerable<StyleCollection> collections;
		private readonly IEnumerable<string> shopsToUse;

		private readonly IEnumerator<KeyValuePair<string, ItemQueryResult>[]> batch;

		public ShopBatcher(IEnumerable<string> ShopsToUse)
		{
			shopsToUse = ShopsToUse;
			shops = DataLoader.Shops(Game1.content);
			collections = AssetManager.Collections.Values;

			var ctx = new ItemQueryContext(Game1.currentLocation, Game1.player, Game1.random, "Happy Home Designer Catalogue Menu");
			var gsq = new GameStateQueryContext(Game1.currentLocation, Game1.player, null, null, Game1.random);

			batch = shopsToUse
				.SelectMany(id => shops[id].Items, (id, val) => (id, val))
				.Where(e => e.val.Condition is null || GameStateQuery.CheckConditions(e.val.Condition, gsq))
				.SelectMany(item => LazyItemResolver.TryResolve(
					item.val.ItemId, ctx,
					perItemCondition: item.val.PerItemCondition,
					maxItems: item.val.MaxItems,
					avoidRepeat: item.val.AvoidRepeat,
					logError: static (q, e) => ModEntry.monitor.Log($"Error checking query '{q}': {e}", LogLevel.Warn)
				), 
				(item, result) => new KeyValuePair<string, ItemQueryResult>(item.id, result))
				.Chunk(100)
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
	}
}
