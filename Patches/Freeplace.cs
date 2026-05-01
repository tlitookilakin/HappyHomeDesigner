using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Menus;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using SObject = StardewValley.Object;

namespace HappyHomeDesigner.Patches
{
	internal class Freeplace
	{
		private static readonly AccessTools.FieldRef<Furniture, NetVector2> DrawPosition
			= AccessTools.FieldRefAccess<Furniture, NetVector2>("drawPosition");

		internal static void Apply(HarmonyHelper harmony)
		{
			harmony
				.With<SObject>(nameof(SObject.drawPlacementBounds)).Transpiler(ModifyPreview)
				.With(nameof(SObject.placementAction)).Transpiler(ModifyPlacement)
				.With<FishTankFurniture>(nameof(FishTankFurniture.GetTankBounds)).Postfix(ModifyBounds)
				.With<Furniture>(nameof(Furniture.GetSeatPositions)).Postfix(ModifySeats);

			if (ModEntry.helper.ModRegistry.IsLoaded("leroymilo.FurnitureFramework"))
			{
				if (ModUtilities.TryFindAssembly("FurnitureFramework", out var asm))
				{
					try
					{
						var type = asm.GetType("FurnitureFramework.Data.FType.FType");
						harmony.Patcher.Patch(
							type.GetMethod("IsClicked", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, [typeof(Furniture), typeof(int), typeof(int)]),
							transpiler: new(typeof(Freeplace), nameof(FixInteractionsFF))
						);
					}
					catch (Exception ex)
					{
						ModEntry.monitor.Log($"Error patching Furniture Framework: {ex}", StardewModdingAPI.LogLevel.Warn);
					}
				}
				else
				{
					ModEntry.monitor.Log("Failed to find assembly for Furniture Framework");
				}
			}
			else
			{
				harmony.With<GameLocation>(nameof(GameLocation.checkAction)).Transpiler(FixInteractions);
			}
		}

		public static void ApplyFreePlaceIfNeeded(SObject instance)
		{
			if (instance is not Furniture furn || !IsFreePlacing())
				return;

			Furniture.isDrawingLocationFurniture = true;
			var pos = Game1.GetPlacementGrabTile() * 64f;
			var yOffset = furn.sourceRect.Height * 4 - furn.boundingBox.Height;

			pos = new(MathF.Floor((pos.X - 32f) / 4f) * 4f, MathF.Floor((pos.Y - 32f - yOffset) / 4f) * 4f); // snap to world pixel

			var bounds = furn.boundingBox.Value;
			bounds.X = (int)pos.X;
			bounds.Y = (int)pos.Y + yOffset;
			furn.boundingBox.Value = bounds;

			DrawPosition(furn).Value = pos;
		}

		public static bool IsFreePlacing()
		{
			return
				!Game1.isCheckingNonMousePlacement &&
				ModEntry.config.EnableFreePlace &&
				Catalog.ActiveMenu.Value != null &&
				ModEntry.config.FreePlaceKeys.IsDown();
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
					new(OpCodes.Call, typeof(Freeplace).GetMethod(nameof(ApplyFreePlaceIfNeeded))),
					new(OpCodes.Ldarg_0)
				);

			return il.InstructionEnumeration();
		}

		private static IEnumerable<CodeInstruction> ModifyPlacement(IEnumerable<CodeInstruction> codes, ILGenerator gen)
		{
			var il = new CodeMatcher(codes, gen);

			il
				.MatchStartForward(
					new CodeMatch(OpCodes.Call, typeof(Furniture).GetMethod(nameof(Furniture.GetFurnitureInstance)))
				)
				.MatchStartForward(
					new CodeMatch(OpCodes.Callvirt, typeof(NetCollection<Furniture>).GetMethod(nameof(NetCollection<>.Add)))
				)
				.InsertAndAdvance(
					new CodeInstruction(OpCodes.Call, typeof(Freeplace).GetMethod(nameof(NudgeBeforePlacement)))
				)
				.Advance(1)
				.MatchStartForward(
					new CodeMatch(OpCodes.Callvirt, typeof(NetCollection<Furniture>).GetMethod(nameof(NetCollection<>.Add)))
				)
				.InsertAndAdvance(
					new CodeInstruction(OpCodes.Call, typeof(Freeplace).GetMethod(nameof(NudgeBeforePlacement)))
				);

			return il.InstructionEnumeration();
		}

		public static Furniture NudgeBeforePlacement(Furniture furn)
		{
			if (furn is null || !IsFreePlacing())
				return furn;

			var raw = Game1.GetPlacementGrabTile() * 64f - new Vector2(32f, 32f);

			int x = ((int)raw.X) - (((int)raw.X) % 4);
			int y = ((int)raw.Y) - (((int)raw.Y) % 4);

			// already grid-aligned
			if (x % 64 == 0 && y % 64 == 0)
				return furn;

			furn.removeLights();
			furn.RemoveLightGlow();

			var bounds = furn.boundingBox.Value;
			furn.boundingBox.Value = new(x, y, bounds.Width, bounds.Height);
			furn.updateDrawPosition();

			return furn;
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
