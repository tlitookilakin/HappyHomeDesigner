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
			List<RoomLayoutData> entries = ModEntry.helper.Data.ReadGlobalData<List<RoomLayoutData>>($"{PATH}_{where.Name}");
			return entries ?? [];
		}

		public static void SaveLayoutsFor(GameLocation where, IList<RoomLayoutData> data)
		{
			ModEntry.helper.Data.WriteGlobalData($"{PATH}_{where.Name}", data);
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
