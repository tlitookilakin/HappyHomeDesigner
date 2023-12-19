using HappyHomeDesigner.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using SObject = StardewValley.Object;

namespace HappyHomeDesigner.Patches
{
	internal class AltTex
	{
		public static bool forceMenuDraw = false;
		public static bool forcePreviewDraw = false;

		internal static void Apply(Harmony harmony)
		{
			if (!ModUtilities.TryFindAssembly("AlternativeTextures", out var asm))
				return;

			var furniturePatcher = asm.GetType("AlternativeTextures.Framework.Patches.StandardObjects.FurniturePatch");
			var objectPatcher = asm.GetType("AlternativeTextures.Framework.Patches.StandardObjects.ObjectPatch");
			var bedPatcher = asm.GetType("AlternativeTextures.Framework.Patches.StandardObjects.BedFurniturePatch");

			if (furniturePatcher is null || objectPatcher is null || bedPatcher is null)
			{
				ModEntry.monitor.Log("Failed to find one or more Alternative Textures patch targets", LogLevel.Warn);
				return;
			}

			var flag = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			harmony.Patch(furniturePatcher.GetMethod("DrawInMenuPrefix", flag), transpiler: new(typeof(AltTex), nameof(menuDraw)));
			harmony.Patch(objectPatcher.GetMethod("DrawPlacementBoundsPrefix", flag), prefix: new(typeof(AltTex), nameof(skipNameCaching)));
			harmony.Patch(furniturePatcher.GetMethod("DrawPrefix", flag), transpiler: new(typeof(AltTex), nameof(fixFurniturePreview)));
			harmony.Patch(objectPatcher.GetMethod("PlacementActionPostfix", flag), prefix: new(typeof(AltTex), nameof(preventRandomVariant)));
			harmony.Patch(bedPatcher.GetMethod("DrawPrefix", flag), transpiler: new(typeof(AltTex), nameof(fixBedPreview)));
		}

		private static IEnumerable<CodeInstruction> menuDraw(IEnumerable<CodeInstruction> source, ILGenerator gen)
		{
			var skipRotation = gen.DefineLabel();
			var skipOffset = gen.DefineLabel();

			var il = new CodeMatcher(source)
				.MatchStartForward(
					new CodeMatch(OpCodes.Brtrue_S)
				);
			var target = (Label)il.Instruction.operand;

			il.Advance(1)
				.InsertAndAdvance(
					new(OpCodes.Ldsfld, typeof(AltTex).GetField(nameof(forceMenuDraw))),
					new(OpCodes.Brtrue, target)
				).MatchStartForward(
					new(OpCodes.Ldarg_0),
					new(OpCodes.Ldfld, typeof(Furniture).GetField(nameof(Furniture.rotations)))
				).InsertAndAdvance(
					new(OpCodes.Ldsfld, typeof(AltTex).GetField(nameof(forceMenuDraw))),
					new(OpCodes.Brtrue, skipRotation)
				).MatchStartForward(
					new(OpCodes.Ldarg_0),
					new(OpCodes.Ldfld, typeof(Furniture).GetField(nameof(Furniture.defaultSourceRect)))
				);
			il.Instruction.labels.Add(skipRotation);

			il.MatchStartForward(
					new(OpCodes.Ldc_I4_0),
					new(OpCodes.Ldarg_0),
					new(OpCodes.Ldfld, typeof(Furniture).GetField(nameof(Furniture.sourceRect)))
				)
				.InsertAndAdvance(
					new(OpCodes.Ldc_I4_0),
					new(OpCodes.Ldsfld, typeof(AltTex).GetField(nameof(forceMenuDraw))),
					new(OpCodes.Brtrue, skipOffset),
					new(OpCodes.Pop)
				)
				.MatchStartForward(
					new CodeMatch(OpCodes.Stfld, typeof(Rectangle).GetField(nameof(Rectangle.X)))
				);
			il.Instruction.labels.Add(skipOffset);

			return il.InstructionEnumeration();
		}

		private static bool skipNameCaching(ref bool __result, StardewValley.Object __0)
		{
			// don't ditch at name, so we can see it in previews
			__result = true;
			if (__0.modData.TryGetValue("AlternativeTextureName", out var name))
				__0.modData["AlternativeTextureNameCached"] = name;
			return !forcePreviewDraw;
		}

		private static IEnumerable<CodeInstruction> fixFurniturePreview(IEnumerable<CodeInstruction> source)
		{
			// when not drawing in-world, still use AT sourcerect instead of default sourcerect
			var il = new CodeMatcher(source)
				.MatchStartForward(
					new CodeMatch(OpCodes.Ldsfld, typeof(Furniture).GetField(nameof(Furniture.isDrawingLocationFurniture)))
				).MatchStartBackwards(
					new CodeMatch(OpCodes.Ldloca_S)
				);
			var sourceRect = il.Instruction.operand;
			il.MatchStartForward(
					new(OpCodes.Ldarg_0),
					new(OpCodes.Ldfld, typeof(Furniture).GetField(nameof(Furniture.sourceRect))),
					new(OpCodes.Call, typeof(NetFieldBase<Rectangle, NetRectangle>).GetMethod("op_Implicit"))
				).RemoveInstructions(3)
				.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldloc_S, sourceRect)
				);
			return il.InstructionEnumeration();
		}
		private static IEnumerable<CodeInstruction> fixBedPreview(IEnumerable<CodeInstruction> source, ILGenerator gen)
		{
			LocalBuilder bounds = gen.DeclareLocal(typeof(Vector2));
			// just remove location furniture check completely and fix the offset
			var il = new CodeMatcher(source)
				.MatchStartForward(
					new CodeMatch(OpCodes.Ldsfld, typeof(Furniture).GetField(nameof(Furniture.isDrawingLocationFurniture)))
				).MatchStartForward(
					new CodeMatch(OpCodes.Brfalse)
				)
				.RemoveInstruction()
				.InsertAndAdvance(
					new(OpCodes.Ldarg_1),
					new(OpCodes.Call, typeof(NetFieldBase<Vector2, NetVector2>).GetProperty(nameof(NetVector2.Value)).GetMethod),
					new(OpCodes.Ldarg_3),
					new(OpCodes.Ldarg_S, 4),
					new(OpCodes.Ldarg_0),
					new(OpCodes.Call, typeof(AltTex).GetMethod(nameof(AdjustBedPosition))),
					new(OpCodes.Stloc, bounds)
				);

			while (true)
			{
				il.MatchStartForward(
					new(OpCodes.Ldarg_1),
					new(OpCodes.Call, typeof(NetFieldBase<Vector2, NetVector2>).GetMethod("op_Implicit"))
				);
				if (il.IsInvalid)
					break;
				il.RemoveInstructions(2)
					.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldloc, bounds)
					);
			}

			return il.InstructionEnumeration();
		}
		public static Vector2 AdjustBedPosition(bool placed, Vector2 DrawPosition, int x, int y, BedFurniture __instance)
		{
			if (placed)
				return DrawPosition;
			return new(x * 64, y * 64 - (__instance.sourceRect.Height * 4 - __instance.boundingBox.Height));
		}
		private static bool preventRandomVariant(StardewValley.Object __0)
		{
			return __0 is not Furniture || !forcePreviewDraw;
		}
	}
}
