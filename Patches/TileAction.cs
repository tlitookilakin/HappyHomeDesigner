using HappyHomeDesigner.Menus;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System;

namespace HappyHomeDesigner.Patches
{
    internal class TileAction
	{
		public static void Apply(Harmony harmony)
		{
			harmony.Patch(typeof(GameLocation).GetMethod(nameof(GameLocation.performAction)), postfix: new(typeof(TileAction), nameof(DoAction)));
		}

		private static bool DoAction(bool handled, string action, Farmer who)
		{
			if (handled)
				return true;

			if (action is not "HappyHomeDesigner")
				return false;

			if (who == Game1.player)
			{
				if (Catalog.TryShowCatalog(Catalog.AvailableCatalogs.All))
					ModEntry.monitor.Log("Table activated!", LogLevel.Debug);
				else
					ModEntry.monitor.Log("Failed to display display UI", LogLevel.Debug);
			}
			return true;
		}
	}
}
