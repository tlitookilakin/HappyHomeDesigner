using HappyHomeDesigner.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

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
			if (furniturePatcher is null)
				return;
			var objectPatcher = asm.GetType("AlternativeTextures.Framework.Patches.StandardObjects.ObjectPatch");
			if (objectPatcher is null)
				return;

			var flag = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			harmony.Patch(furniturePatcher.GetMethod("DrawInMenuPrefix", flag), transpiler: new(typeof(AltTex), nameof(menuDraw)));
			harmony.Patch(objectPatcher.GetMethod("DrawPlacementBoundsPrefix", flag), prefix: new(typeof(AltTex), nameof(skipNameCaching)));
			harmony.Patch(furniturePatcher.GetMethod("DrawPrefix", flag), transpiler: new(typeof(AltTex), nameof(fixFurniturePreview)));
			harmony.Patch(objectPatcher.GetMethod("PlacementActionPostfix", flag), prefix: new(typeof(AltTex), nameof(preventRandomVariant)));
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
			__result = true;
			if (__0.modData.TryGetValue("AlternativeTextureName", out var name))
				__0.modData["AlternativeTextureNameCached"] = name;
			return !forcePreviewDraw;
		}

		private static IEnumerable<CodeInstruction> fixFurniturePreview(IEnumerable<CodeInstruction> source)
		{
			var il = new CodeMatcher(source)
				.MatchEndForward(
					new(OpCodes.Ldarg_1),
					new(OpCodes.Callvirt, typeof(NetFieldBase<int, NetInt>).GetProperty("Value").GetMethod),
					new(OpCodes.Stloc_S)
				);
			var offset = il.Instruction.operand;
			il.MatchStartForward(
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
		private static bool preventRandomVariant(StardewValley.Object __0)
		{
			return __0 is not Furniture || !forcePreviewDraw;
		}
	}
}
