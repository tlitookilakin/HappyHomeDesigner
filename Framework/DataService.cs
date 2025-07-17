using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Framework
{
	public static class DataService
	{
		public const string PATH = ModEntry.MOD_ID + "_Layouts";

		public static List<RoomLayoutData> GetLayoutsFor(GameLocation where)
		{
			List<RoomLayoutData> entries = null;
			try
			{
				entries = ModEntry.helper.Data.ReadGlobalData<List<RoomLayoutData>>($"{PATH}_{where.Name}");
			}
			catch (Exception e)
			{
				ModEntry.monitor.Log($"Error loading layouts for location '{where.Name}':\n{e}", LogLevel.Warn);
			}
			return entries ?? [];
		}

		public static void SaveLayoutsFor(GameLocation where, IList<RoomLayoutData> data)
		{
			try
			{
				ModEntry.helper.Data.WriteGlobalData($"{PATH}_{where.Name}", data);
			}
			catch(Exception e)
			{
				ModEntry.monitor.Log($"Error saving layouts for location '{where.Name}':\n{e}", LogLevel.Warn);
			}
		}

		public static IEnumerable<string> GetFavoritesFor(Farmer who, string key)
		{
			IEnumerable<string> favs = null;
			string altKey = key.Replace('/', '_');

			if (!Context.IsSplitScreen || who.IsMainPlayer)
				favs = ModEntry.helper.Data.ReadGlobalData<IEnumerable<string>>(altKey);

			if (favs is null)
			{
				if (Game1.player.modData.TryGetValue(key, out var s))
					favs = s.Split('	', StringSplitOptions.RemoveEmptyEntries);
				else
					favs = [];
			}

			return favs;
		}

		public static void SaveFavoritesFor(Farmer who, string key, IEnumerable<string> favorites)
		{
			string altKey = key.Replace('/', '_');

			if (!Context.IsSplitScreen || who.IsMainPlayer)
			{
				ModEntry.helper.Data.WriteGlobalData(altKey, favorites);
				who.modData.Remove(key);
				return;
			}
			
			who.modData[key] = string.Join('	', favorites);
		}
	}
}
