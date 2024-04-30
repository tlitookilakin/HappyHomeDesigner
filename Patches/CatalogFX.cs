using HappyHomeDesigner.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Reflection;

namespace HappyHomeDesigner.Patches
{
	internal class CatalogFX
	{
		private static AccessTools.FieldRef<Furniture, NetVector2> drawPosition;
		private static Func<Furniture, float> getScaleSize;
		private static readonly Vector2 menuOffset = new Vector2(32f, 32f);

		internal static void Apply(Harmony harmony)
		{
			drawPosition = AccessTools.FieldRefAccess<Furniture, NetVector2>("drawPosition");
			getScaleSize = typeof(Furniture)
				.GetMethod("getScaleSize", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic)
				.CreateDelegate<Func<Furniture, float>>();

			harmony.Patch(
				typeof(Furniture).GetMethod(nameof(Furniture.draw), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
				postfix: new(typeof(CatalogFX), nameof(DrawPatch))
			);
			harmony.Patch(
				typeof(Furniture).GetMethod(nameof(Furniture.drawAtNonTileSpot)),
				postfix: new(typeof(CatalogFX), nameof(DrawOnSpot))
			);
			harmony.Patch(
				typeof(Furniture).GetMethod(nameof(Furniture.drawInMenu), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
				postfix: new(typeof(CatalogFX), nameof(DrawMenu))
			);
		}

		private static void DrawPatch(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
		{
			switch(__instance.QualifiedItemId)
			{
				case "(F)" + AssetManager.COLLECTORS_ID:
					GetVars(__instance, x, y, out var pos, out var effect, out var depth);
					DrawCollectorFX(__instance, spriteBatch, pos, Color.White * alpha, depth, 4f, effect, Vector2.Zero);
					break;
			}
		}

		private static void DrawMenu(Furniture __instance, SpriteBatch spriteBatch, Vector2 location, 
			float scaleSize, float transparency, float layerDepth, Color color)
		{
			switch(__instance.QualifiedItemId)
			{
				case "(F)" + AssetManager.COLLECTORS_ID:
					DrawCollectorFX(
						__instance, spriteBatch, location + menuOffset, color * transparency, 
						layerDepth, scaleSize * getScaleSize(__instance), SpriteEffects.None, 
						new(__instance.sourceRect.Width / 2, __instance.sourceRect.Height / 2)
					); break;
			}
		}

		private static void DrawOnSpot(Furniture __instance, SpriteBatch spriteBatch, Vector2 location, float layerDepth, float alpha)
		{
			switch(__instance.QualifiedItemId)
			{
				case "(F)" + AssetManager.COLLECTORS_ID:
					var flipped = __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
					DrawCollectorFX(__instance, spriteBatch, location, Color.White * alpha, layerDepth, 4f, flipped, Vector2.Zero);
					break;
			}
		}

		private static void GetVars(Furniture furn, int x, int y, out Vector2 location, out SpriteEffects effect, out float depth)
		{
			location =
				Game1.GlobalToLocal(Game1.viewport, 
				Furniture.isDrawingLocationFurniture ?
				drawPosition(furn).Value :
				new(x * 64, y * 64 - (furn.sourceRect.Height * 4 - furn.boundingBox.Height))
			);
			effect = furn.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			depth =
				Furniture.isDrawingLocationFurniture ?
				(furn.TileLocation.Y + furn.boundingBox.Height) * (64f / 10000f) :
				0f;
		}

		private static void DrawCollectorFX(Furniture furn, SpriteBatch batch, Vector2 pos, Color color, float depth, float scale, SpriteEffects effect, Vector2 origin)
		{
			var data = ItemRegistry.GetData(furn.QualifiedItemId);
			pos = new(pos.X, pos.Y + (float)Math.Sin(Game1.ticks * (Math.Tau / 100.0)) * 6f);
			var source = furn.sourceRect.Value;
			source.X += source.Width;

			batch.Draw(data.GetTexture(), pos, source, color, 0f, origin, scale, effect, depth + (1f / 10000f));
		}
	}
}
