using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Menus;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StarModGen.Utils;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HappyHomeDesigner.Patches
{
	internal class ReplaceShop
	{
		internal static void Apply(HarmonyHelper helper)
		{
			helper.WithAll<Utility>(nameof(Utility.TryOpenShopMenu)).Transpiler(PatchOpenShop);
		}

		public static IEnumerable<CodeInstruction> PatchOpenShop(IEnumerable<CodeInstruction> src, ILGenerator gen)
		{
			var il = new CodeMatcher(src, gen);

			il.MatchStartForward(
				new CodeMatch(OpCodes.Call, typeof(Game1).GetProperty(nameof(Game1.activeClickableMenu)).SetMethod)
			);

			if (il.IsInvalid)
			{
				ModEntry.monitor.Log($"Failed to patch shop open! Could not find injection point.", LogLevel.Error);
				return null;
			}

			il.Insert(
				new CodeMatch(OpCodes.Call, typeof(ReplaceShop).GetMethod(nameof(CheckAndReplace)))
			);

			return il.InstructionEnumeration();
		}

		public static ShopMenu CheckAndReplace(ShopMenu menu)
		{
			if (Catalog.TryShowCatalog(menu))
				return null;
			return menu;
		}
	}
}
