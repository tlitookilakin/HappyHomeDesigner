using StardewValley;
using System.Collections.Generic;

namespace HappyHomeDesigner.Framework
{
	public static class RoomLayoutManager
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
	}
}
