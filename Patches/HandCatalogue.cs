using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Menus;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Reflection;

namespace HappyHomeDesigner.Patches
{
	internal class HandCatalogue
	{
		private static readonly Vector2 menuOffset = new(32f, 32f);
		private static readonly Vector2 menuOrigin = new(8f, 8f);

		public static void Apply(Harmony harmony)
		{
			harmony.TryPatch(
				typeof(Tool).GetMethod(nameof(Tool.DoFunction)),
				postfix: new(typeof(HandCatalogue), nameof(OpenIfCatalogue))
			);
			harmony.TryPatch(
				typeof(Tool).GetMethod(nameof(Tool.drawInMenu), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
				postfix: new(typeof(HandCatalogue), nameof(DrawInMenu))
			);
		}

		private static void OpenIfCatalogue(Tool __instance, Farmer who)
		{
			if (__instance.ItemId != AssetManager.PORTABLE_ID)
				return;

			if (!who.IsLocalPlayer)
				return;

			var catalogues = 
				ModUtilities.CatalogType.Collector | 
				ModUtilities.CatalogType.Furniture | 
				ModUtilities.CatalogType.Wallpaper;

			Catalog.ShowCatalog(ModUtilities.GenerateCombined(catalogues), catalogues.ToString());
		}

		private static void DrawInMenu(Tool __instance, SpriteBatch spriteBatch, Vector2 location, 
			float scaleSize, float transparency, float layerDepth, Color color)
		{
			if (__instance.QualifiedItemId != "(T)" + AssetManager.PORTABLE_ID)
				return;

			var data = ItemRegistry.GetDataOrErrorItem("(T)" + AssetManager.PORTABLE_ID);
			var source = data.GetSourceRect();
			source.X = source.Right;

			spriteBatch.Draw(
				data.GetTexture(), location + menuOffset, source, 
				color.Mult(Utility.GetPrismaticColor(5)) * transparency, 
				0f, menuOrigin, scaleSize * 4f, SpriteEffects.None, layerDepth
			);
		}
	}
}
