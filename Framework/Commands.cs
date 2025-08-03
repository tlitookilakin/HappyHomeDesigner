using StardewModdingAPI;
using StardewValley;
using System.Linq;
using System.Text;

namespace HappyHomeDesigner.Framework
{
	internal static class Commands
	{
		public static void BindAll(IModHelper helper)
		{
			helper.ConsoleCommands.Add(
				"hhd_export_layout_property", 
				"Exports the current farmhouse furniture layout to the clipboard for use with the FarmHouseFurniture map property." +
				"Also prints the current wallpaper and flooring.",
				ExportFurniture
			);
		}

		private static void ExportFurniture(string cmd, string[] args)
		{
			var sb = new StringBuilder();
			var where = Utility.getHomeOfFarmer(Game1.player);

			foreach (var item in where.furniture)
				sb.Append(item.ItemId).Append(' ')
					.Append(item.TileLocation.X).Append(' ')
					.Append(item.TileLocation.Y).Append(' ')
					.Append(item.currentRotation.Value).Append(' ');

			var txt = sb.ToString();
			DesktopClipboard.SetText(txt);

			ModEntry.monitor.Log(
				$"Wallpaper: ${where.appliedWallpaper.Values.FirstOrDefault()}\nFlooring: ${where.appliedFloor.Values.FirstOrDefault()}",
				LogLevel.Info
			);
		}
	}
}
