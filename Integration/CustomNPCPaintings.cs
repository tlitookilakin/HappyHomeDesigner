using HappyHomeDesigner.Menus;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using System;
using System.Linq;
using System.Reflection;

namespace HappyHomeDesigner.Integration
{
	public static class CustomNPCPaintings
	{
		public static Action ShowMenu;

		private const string ID = "AvalonMFX.CustomNPCPaintings";

		internal static void Init()
		{
			if (!ModEntry.helper.ModRegistry.IsLoaded(ID))
				return;

			var man = ModEntry.helper.ModRegistry.Get(ID).Manifest;

			var asm_id = man.EntryDll.Trim();
			if (asm_id.EndsWithIgnoreCase(".dll"))
				asm_id = asm_id[..^4];

			var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == asm_id);

			if (asm == null)
			{
				ModEntry.monitor.Log(ModEntry.i18n.Get("logging.cnpcp.noload"), LogLevel.Warn);
				return;
			}

			var type = asm.GetType("DynamicNPCPaintings.UI.Customizer");

			if (type == null)
			{
				ModEntry.monitor.Log(ModEntry.i18n.Get("logging.cnpcp.notype"), LogLevel.Warn);
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
			=> ShowMenu is null ? null : new(new(0, 0, 64, 64), Catalog.MenuTexture, new(48, 24, 16, 16), 4f);
	}
}
