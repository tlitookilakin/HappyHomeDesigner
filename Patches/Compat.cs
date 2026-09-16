using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Menus;
using static HappyHomeDesigner.Framework.ModUtilities;

namespace HappyHomeDesigner.Patches
{
	internal static class Compat
	{
		public static void Apply(HarmonyHelper harmony)
		{
			if (ModEntry.helper.ModRegistry.TryGetMod("Zexu2K.MagicStardew.C", out var mod) && mod.Manifest.TryGetType("MagicStardew.ManaBar", out var t))
				harmony.With(t, "OnRenderedHud").Prefix(SkipIfHidden);

			if (ModEntry.helper.ModRegistry.TryGetMod("moonslime.ManaBarAPI", out mod) && mod.Manifest.TryGetType("WizardryManaBar.Core.Events", out t))
				harmony.With(t, "OnRenderedHud").Prefix(SkipIfHidden);

			if (ModEntry.helper.ModRegistry.TryGetMod("lucaskfreitas.ImmersiveScarecrows", out mod))
			{
				if(mod.Manifest.TryGetType(
					"ImmersiveScarecrows.ModEntry+Utility_playerCanPlaceItemHere_Patch",
					out t
				))
				{
					harmony.With(t, "Prefix").Postfix(ModifyScarecrowCheck);
				}
			}
		}

		private static void ModifyScarecrowCheck(bool __result, ref bool __5)
		{
			// prefix not active, skip logic
			if (__result)
				return;

			// force range
			if (Catalog.ActiveMenu.Value != null)
				__5 = true;
		}

		private static bool SkipIfHidden()
		{
			return Catalog.ActiveMenu.Value == null;
		}
	}
}
