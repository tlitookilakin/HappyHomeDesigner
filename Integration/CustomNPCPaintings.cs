using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Menus;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Reflection;

namespace HappyHomeDesigner.Integration
{
	public static class CustomNPCPaintings
	{
		public static Action ShowMenu;

		private const string ID = "AvalonMFX.CustomNPCPaintings";

		internal static void Init()
		{
			if (!ModEntry.helper.ModRegistry.TryGetMod(ID, out var mod))
				return;

			if (!mod.Manifest.TryGetType("DynamicNPCPaintings.UI.Customiser", out var type))
			{
				ModEntry.monitor.Log(ModEntry.i18n.Get("logging.cnpcp.notype"), LogLevel.Warn);
				ModEntry.monitor.Log("CNPC: no menu type", LogLevel.Trace);
				return;

			}

			ShowMenu = typeof(CustomNPCPaintings)
				.GetMethod(nameof(ShowMenuImpl), BindingFlags.Static | BindingFlags.NonPublic)
				.MakeGenericMethod(type).CreateDelegate<Action>();
		}

		private static void ShowMenuImpl<T>() where T: IClickableMenu, new()
		{
			Game1.activeClickableMenu = new T();
		}

		public static ClickableTextureComponent GetButton()
			=> ShowMenu is null ? null : new(new(0, 0, 64, 64), Catalog.MenuTexture, new(0, 97, 16, 16), 4f);
	}
}
