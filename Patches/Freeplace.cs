using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Menus;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using SObject = StardewValley.Object;

namespace HappyHomeDesigner.Patches
{
	// TODO fix weird fucking left click jank
	internal static class Freeplace
	{
		private static int layerOffset = 0;
		private static bool isInPlacement = false;
		private static bool isInRemove = false;

		internal static void Apply(HarmonyHelper harmony)
		{
			harmony
				.With<SObject>(nameof(SObject.drawPlacementBounds)).Transpiler(ModifyPreview)
				.With(nameof(SObject.placementAction)).Prefix(BeforePlace).Postfix(AfterPlace)
				.With<FishTankFurniture>(nameof(FishTankFurniture.GetTankBounds)).Postfix(ModifyBounds)
				.With<GameLocation>(nameof(GameLocation.GetFurnitureAt)).Postfix(HideIfOverlapped)
				.With<Furniture>(nameof(Furniture.GetSeatPositions)).Postfix(ModifySeats)
				.With(nameof(Furniture.OnAdded)).Postfix(NudgeBoundingBox)
				.With(nameof(Furniture.draw)).Transpiler(InjectDepth)
				.With(nameof(Furniture.AllowPlacementOnThisTile)).Postfix(ForcePlacement)
				.With(nameof(Furniture.canBeRemoved)).Prefix(BeforeRemove).Postfix(AfterRemove);

			ModEntry.helper.Events.Display.RenderingWorld += ResetLayer;

			if (ModEntry.helper.ModRegistry.TryGetMod("leroymilo.FurnitureFramework", out var mod))
			{
				if (mod.Manifest.TryGetType("FurnitureFramework.Data.FType.FType", out var type))
					harmony.With(type, "IsClicked", [typeof(Furniture), typeof(int), typeof(int)]).Transpiler(FixInteractionsFF);
				else
					ModEntry.monitor.Log("Failed to find assembly for Furniture Framework", LogLevel.Warn);
			}
			else
			{
				harmony.With<GameLocation>(nameof(GameLocation.checkAction)).Transpiler(FixInteractions);
			}
		}

		private static void ResetLayer(object sender, RenderingWorldEventArgs e)
		{
			layerOffset = 0;
			isInPlacement = false;
			isInRemove = false;
		}

		private static bool ForcePlacement(bool allowed, Furniture __instance)
		{
			return allowed || (
				ModEntry.config.AllowOverlap && 
				(Catalog.ActiveMenu.Value != null || Game1.activeClickableMenu is BlueprintMenu) &&
				(
					__instance.furniture_type.Value is 12 ||
					__instance.HasContextTag("ff_category_floor") ||
					__instance.IsWallItem()
				)
			);
		}

		private static IEnumerable<CodeInstruction> InjectDepth(IEnumerable<CodeInstruction> source)
		{
			var il = new CodeMatcher(source);

			ApplyDepthInjection(il);

			return il.InstructionEnumeration();
		}

		internal static void ApplyDepthInjection(CodeMatcher il)
		{
			il
				.MatchStartForward(
					new CodeMatch(OpCodes.Ldc_R4, 2E-09f)
				)
				.MatchStartForward(
					new CodeMatch(
						OpCodes.Callvirt,
						typeof(SpriteBatch).GetMethod(
							nameof(SpriteBatch.Draw),
							[
								typeof(Texture2D), typeof(Vector2), typeof(Rectangle), typeof(Color),
								typeof(float), typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float)
							]
						)
					)
				)
				.InsertAndAdvance(
					new(OpCodes.Ldarg_0),
					new(OpCodes.Call, typeof(Freeplace).GetMethod(nameof(ModifyDepth)))
				)
				.AddLabelsAt(il.Pos - 2, il.Instruction.ExtractLabels());
		}

		public static float ModifyDepth(float value, Furniture furn)
		{
			if (!ModEntry.config.AllowOverlap)
				return value;

			if (furn.IsWallItem(true))
			{
				value = (furn.defaultBoundingBox.Bottom - 48) / 10_000f + layerOffset / 1_000_000f;
				layerOffset++;
			}
			else if (furn.furniture_type.Value is 12)
			{
				value = (layerOffset / 100_000f) + 2E-09f;
				layerOffset++;
			}
			return value;
		}

		private static void BeforeRemove()
		{
			isInRemove = true;
		}

		private static void AfterRemove()
		{
			isInRemove = false;
		}

		private static Furniture HideIfOverlapped(Furniture furn)
		{
			if (!isInRemove || !ModEntry.config.AllowOverlap || furn is null)
				return furn;

			if (furn.IsWallItem() || furn.furniture_type.Value is 12 || furn.HasContextTag("ff_category_floor"))
				return null;

			return furn;
		}

		private static IEnumerable<CodeInstruction> ModifyPreview(IEnumerable<CodeInstruction> codes, ILGenerator gen)
		{
			var il = new CodeMatcher(codes, gen);
			var furnitureState = gen.DeclareLocal(typeof(bool));

			/* bool state = Furniture.isDrawingLocationFurniture;
			*  ApplyFreePlaceIfNeeded(this);
			*  this.draw(...);
			*  Furniture.isDrawingLocationFurniture = state;
			*/

			il
				.MatchStartForward(
					new CodeMatch(OpCodes.Callvirt, typeof(SObject).GetMethod(nameof(SObject.draw), [typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)]))
				)
				.Advance(1)
				.Insert(
					new(OpCodes.Ldloc, furnitureState),
					new(OpCodes.Stsfld, typeof(Furniture).GetField(nameof(Furniture.isDrawingLocationFurniture)))
				)
				.MatchEndBackwards(
					new(OpCodes.Ldarg_0),
					new(OpCodes.Ldarg_1)
				)
				.InsertAndAdvance(
					new(OpCodes.Ldsfld, typeof(Furniture).GetField(nameof(Furniture.isDrawingLocationFurniture))),
					new(OpCodes.Stloc, furnitureState),
					new(OpCodes.Call, typeof(Freeplace).GetMethod(nameof(FreePlacePreviewDraw))),
					new(OpCodes.Ldarg_0)
				);

			return il.InstructionEnumeration();
		}


		public static void FreePlacePreviewDraw(SObject obj)
		{
			if (obj is not Furniture furn || !IsFreePlacing())
				return;

			Furniture.isDrawingLocationFurniture = true;
			var pos = Game1.GetPlacementGrabTile() * 64f;
			var yOffset = furn.sourceRect.Height * 4 - furn.boundingBox.Height;

			pos = new(MathF.Floor((pos.X - 32f) / 4f) * 4f, MathF.Floor((pos.Y - 32f - yOffset) / 4f) * 4f); // snap to world pixel

			var bounds = furn.boundingBox.Value;
			bounds.X = (int)pos.X;
			bounds.Y = (int)pos.Y + yOffset;
			furn.boundingBox.Value = bounds;
			furn.updateDrawPosition();
		}

		public static bool IsFreePlacing()
		{
			return
				!Game1.isCheckingNonMousePlacement &&
				ModEntry.config.EnableFreePlace &&
				Catalog.ActiveMenu.Value != null &&
				ModEntry.config.FreePlaceKeys.IsDown();
		}

		private static void BeforePlace()
			=> isInPlacement = true;
		private static void AfterPlace()
			=> isInPlacement = false;

		private static void NudgeBoundingBox(Furniture __instance, Vector2 tilePos)
		{
			if (!isInPlacement || !IsFreePlacing())
				return;

			var furn = __instance;
			var raw = Game1.GetPlacementGrabTile() * 64f - new Vector2(32f, 32f);

			int x = ((int)raw.X) - (((int)raw.X) % 4);
			int y = ((int)raw.Y) - (((int)raw.Y) % 4);

			// already grid-aligned
			if (x % 64 == 0 && y % 64 == 0 && y == tilePos.Y * 64f)
				return;

			var bounds = furn.boundingBox.Value;

			furn.defaultBoundingBox.Value = bounds;
			furn.boundingBox.Value = new(x, y, bounds.Width, bounds.Height);
			furn.updateDrawPosition();
		}

		public static Rectangle ModifyBounds(Rectangle original, FishTankFurniture __instance)
		{
			var offset = GetTileOffset(__instance);

			// already snapped
			if (offset.X == 0 && offset.Y == 0)
				return original;

			return new(original.X + (int)(offset.X * 64), original.Y + (int)(offset.Y * 64), original.Width, original.Height);
		}

		public static List<Vector2> ModifySeats(List<Vector2> seats, Furniture __instance)
		{
			var offset = GetTileOffset(__instance);

			// already snapped
			if (offset.X == 0 && offset.Y == 0)
				return seats;

			for (int i = 0; i < seats.Count; i++)
				seats[i] = seats[i] + offset;

			return seats;
		}

		public static Vector2 GetTileOffset(Furniture f)
		{
			var bounds = f.boundingBox.Value;
			var tile = f.TileLocation;
			return new Vector2(bounds.X / 64f - tile.X, bounds.Y / 64f - tile.Y);
		}

		public static IEnumerable<CodeInstruction> FixInteractions(IEnumerable<CodeInstruction> codes, ILGenerator gen)
		{
			var il = new CodeMatcher(codes, gen);

				il
					.MatchStartForward(
						new(OpCodes.Ldarg_0),
						new(OpCodes.Ldfld, typeof(GameLocation).GetField(nameof(GameLocation.furniture)))
					);

			ModifyCheck(il, 63);

			return il.InstructionEnumeration();
		}

		public static IEnumerable<CodeInstruction> FixInteractionsFF(IEnumerable<CodeInstruction> codes, ILGenerator gen)
		{
			var il = new CodeMatcher(codes, gen);

			ModifyCheck(il, 63);

			return il.InstructionEnumeration();
		}

		private static void ModifyCheck(CodeMatcher il, int size)
		{
			il
				.MatchStartForward(
					new(OpCodes.Ldfld, typeof(SObject).GetField(nameof(SObject.boundingBox))),
					new(OpCodes.Callvirt, typeof(NetFieldBase<Rectangle, NetRectangle>).GetProperty(nameof(NetRectangle.Value)).GetMethod)
				)
				.MatchStartForward(
					new CodeMatch(OpCodes.Call, typeof(Rectangle).GetMethod(nameof(Rectangle.Contains), [typeof(int), typeof(int)]))
				);

			if (il.IsInvalid)
				return;

			il
				.RemoveInstruction()
				.InsertAndAdvance(
					new(OpCodes.Ldc_I4, size),
					new(OpCodes.Ldc_I4, size),
					new(OpCodes.Newobj, typeof(Rectangle).GetConstructor([typeof(int), typeof(int), typeof(int), typeof(int)])),
					new(OpCodes.Call, typeof(Rectangle).GetMethod(nameof(Rectangle.Intersects), [typeof(Rectangle)]))
				);
		}
	}
}
