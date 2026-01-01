using StardewValley;
using StardewValley.Extensions;
using StardewValley.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using static HarmonyLib.AccessTools;
using static StardewValley.Internal.ItemQueryResolver;

namespace HappyHomeDesigner.Framework
{
	// Lifted directly from the decompile, but with some cleanup, and returns the enumerable directly
	// instead of converting to an array, which allows it to be lazily evaluated.
	// This must be copypasted because harmony reverse patches can't alter the return type.

	internal static class LazyItemResolver
	{

		private static readonly FieldRef<ItemQueryContext, string> queryString =
			ModUtilities.GetDirect<ItemQueryContext, string>(nameof(ItemQueryContext.QueryString));

		public static IEnumerable<ItemQueryResult> TryResolve(string query, ItemQueryContext context, ItemQuerySearchMode filter = ItemQuerySearchMode.All, string perItemCondition = null, int? maxItems = null, bool avoidRepeat = false, HashSet<string> avoidItemIds = null, Action<string, string> logError = null)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				return Helpers.ErrorResult(query, "", logError, "must specify an item ID or query");
			}
			string queryKey = query;
			string arguments = null;
			int splitIndex = query.IndexOf(' ');
			if (splitIndex > -1)
			{
				queryKey = query[..splitIndex];
				arguments = query[(splitIndex + 1)..];
			}
			context ??= new ItemQueryContext();
			queryString(context) = query;
			if (context.ParentContext != null)
			{
				List<string> path = [];
				for (ItemQueryContext cur = context; cur != null; cur = cur.ParentContext)
				{
					bool num = path.Contains(cur.QueryString);
					path.Add(cur.QueryString);
					if (num)
					{
						logError?.Invoke(query, "detected circular reference in item queries: " + string.Join(" -> ", path));
						return [];
					}
				}
			}
			IEnumerable<ItemQueryResult> results;
			if (ItemResolvers.TryGetValue(queryKey, out var resolver))
			{
				results = resolver(queryKey, arguments ?? string.Empty, context, avoidRepeat, avoidItemIds, logError ?? new Action<string, string>(LogNothing));
				if (results is ItemQueryResult[] rawArray && rawArray.Length == 0)
				{
					return rawArray;
				}
				HashSet<string> duplicates = (avoidRepeat ? new HashSet<string>() : null);
				if (!avoidRepeat)
				{
					HashSet<string> hashSet = avoidItemIds;
					if ((hashSet == null || hashSet.Count <= 0) && GameStateQuery.IsImmutablyFalse(perItemCondition))
					{
						goto IL_0174;
					}
				}
				results = results.Where(delegate (ItemQueryResult result)
				{
					HashSet<string> hashSet3 = avoidItemIds;
					if (hashSet3 == null || !hashSet3.Contains(result.Item.QualifiedItemId))
					{
						HashSet<string> hashSet4 = duplicates;
						if (hashSet4 == null || hashSet4.Add(result.Item.QualifiedItemId))
							return GameStateQuery.CheckConditions(perItemCondition, null, null, result.Item as Item);
					}
					return false;
				});
				goto IL_0174;
			}
			Item instance = ItemRegistry.Create(query);
			if (instance != null)
			{
				HashSet<string> hashSet2 = avoidItemIds;
				if (hashSet2 == null || !hashSet2.Contains(instance.QualifiedItemId))
					return [new(instance)];
			}
			return [];
		IL_0174:
			switch (filter)
			{
				case ItemQuerySearchMode.AllOfTypeItem:
					results = results.Where(result => result.Item is Item);
					break;
				case ItemQuerySearchMode.FirstOfTypeItem:
					{
						ItemQueryResult result3 = results.FirstOrDefault(p => p.Item is Item);
						results = (result3 == null) ? [] : [result3];
						break;
					}
				case ItemQuerySearchMode.RandomOfTypeItem:
					{
						ItemQueryResult result2 = (context.Random ?? Game1.random).ChooseFrom(results.Where(p => p.Item is Item).ToArray());
						results = (result2 == null) ? [] : [result2];
						break;
					}
			}
			if (maxItems.HasValue)
			{
				results = results.Take(maxItems.Value);
			}
			return results;
		}

		private static void LogNothing(string q, string e) { }
	}
}
