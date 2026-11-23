using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Menus;
using HarmonyLib;
using StardewValley;
using StardewValley.GameData.Shops;
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

			il
				.MatchStartForward(
					new CodeMatch(OpCodes.Call, typeof(Game1).GetProperty(nameof(Game1.activeClickableMenu)).SetMethod)
				)
				.MatchStartBackwards(
					new CodeMatch(OpCodes.Ldarg_0)
				)
				.Advance(1)
				.Insert(
					new CodeInstruction(OpCodes.Ldarg_0)
				)
				.CreateLabel(out var skip)
				.InsertAndAdvance(
					new(OpCodes.Ldloc_1),
					new(OpCodes.Call, typeof(ReplaceShop).GetMethod(nameof(TryReplaceMenu))),
					new(OpCodes.Brfalse, skip),
					new(OpCodes.Ldc_I4_1),
					new(OpCodes.Ret)
				);

			return il.InstructionEnumeration();
		}

		public static bool TryReplaceMenu(string shopId, ShopData data)
		{
			if (data.CountsAsCatalog(shopId))
			{
				Catalog.ShowCatalog(shopId);
				return true;
			}

			return false;
		}
	}
}
