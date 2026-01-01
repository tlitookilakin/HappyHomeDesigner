using HappyHomeDesigner.Data;
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

		private readonly IEnumerator<ItemQueryResult[]> batch;

		public ShopBatcher(IEnumerable<string> ShopsToUse)
		{
			shopsToUse = ShopsToUse;
			shops = DataLoader.Shops(Game1.content);
			collections = AssetManager.Collections.Values;

			var ctx = new ItemQueryContext(Game1.currentLocation, Game1.player, Game1.random, "Happy Home Designer Catalogue Menu");
			var gsq = new GameStateQueryContext(Game1.currentLocation, Game1.player, null, null, Game1.random);

			batch = shopsToUse
				.SelectMany(id => shops[id].Items)
				.Where(e => e.Condition is null || GameStateQuery.CheckConditions(e.Condition, gsq))
				.SelectMany(item => LazyItemResolver.TryResolve(
					item.ItemId, ctx,
					perItemCondition: item.PerItemCondition,
					maxItems: item.MaxItems,
					avoidRepeat: item.AvoidRepeat,
					logError: static (q, e) => ModEntry.monitor.Log($"Error checking query '{q}': {e}", LogLevel.Warn)
				))
				.Chunk(100)
				.GetEnumerator();
		}

		public bool DoBatch(out IDictionary<StyleCollection, List<ISalable>> items)
		{
			items = null;
			if (!batch.MoveNext())
				return false;

			items = new Dictionary<StyleCollection, List<ISalable>>();
			foreach (var entry in batch.Current)
			{
				if (entry.Item is not Item item)
					continue;

				foreach (var collection in collections)
				{
					if (collection.Contains(item))
					{
						if (!items.TryGetValue(collection, out var group))
							items[collection] = [item];
						else
							group.Add(item);
					}
				}
			}

			return true;
		}
	}
}
