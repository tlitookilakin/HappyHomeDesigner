﻿using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Menus;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Shops;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace HappyHomeDesigner.Patches
{
	internal class ReplaceShop
	{
		internal static void Apply(Harmony harmony)
		{
			var patch = new HarmonyMethod(typeof(ReplaceShop), nameof(PatchOpenShop));

			foreach (var method in typeof(Utility).GetMethodsNamed(nameof(Utility.TryOpenShopMenu), 
				BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static))
				harmony.Patch(method, transpiler: patch);
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
			return menu.ShopId switch
			{
				"Catalogue" => 
					ModEntry.config.ReplaceWallpaperCatalog && 
					ShowCatalog(menu.ShopId, menu) 
					? null : menu,

				"Furniture Catalogue" => 
					ModEntry.config.ReplaceFurnitureCatalog && 
					ShowCatalog(menu.ShopId, menu) 
					? null : menu,

				"Happy Home Designer" =>
					ShowCatalog("Combined", null) ? null : menu,

				_ => 
					menu.ShopData is ShopData data && 
					data.CustomFields is Dictionary<string, string> fields &&
					fields.ContainsKey("HappyHomeDesigner/Catalogue") &&
					ModEntry.config.ReplaceModCatalogs &&
					ShowCatalog(menu.ShopId, menu)
					? null : menu
			};
		}

		private static bool ShowCatalog(string id, ShopMenu menu)
		{
			if (Catalog.TryShowCatalog(id, menu))
				ModEntry.monitor.Log("Table activated!", LogLevel.Debug);
			else
				ModEntry.monitor.Log("Failed to display UI", LogLevel.Debug);
			return true;
		}
	}
}
