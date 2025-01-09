using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace HappyHomeDesigner.Framework
{
	public static class RoomLayoutManager
	{
		public const string PATH = "Layouts";

		public static List<RoomLayoutData> GetLayoutsFor(GameLocation where)
		{
			var index = ModEntry.helper.Data.ReadGlobalData<IList<string>>($"{PATH}..{where.Name}.._INDEX");
			if (index is null)
				return [];

			List<RoomLayoutData> entries = [];

			foreach (var name in index)
			{
				var entry = ModEntry.helper.Data.ReadGlobalData<RoomLayoutData>($"{PATH}..{where.Name}..{name}");
				if (entry is null)
					continue;

				entries.Add(entry);
			}

			return entries;
		}

		public static void SaveLayoutsFor(GameLocation where, IList<RoomLayoutData> data)
		{
			ModEntry.helper.Data.WriteGlobalData($"{PATH}..{where.Name}.._INDEX", data.Select(static e => e.ID));
			foreach (var item in data)
				ModEntry.helper.Data.WriteGlobalData($"{PATH}..{where.Name}..{item.ID}", item);
		}
	}
}
