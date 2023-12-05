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
		}

		private static IEnumerable<CodeInstruction> menuDraw(IEnumerable<CodeInstruction> source)
		{
			var il = new CodeMatcher(source);
			il.MatchStartForward(
				new CodeMatch(OpCodes.Brtrue_S)
			);
			var target = (Label)il.Instruction.operand;
			il.Advance(1);
			il.InsertAndAdvance(
				new(OpCodes.Ldsfld, typeof(AltTex).GetField(nameof(forceMenuDraw))),
				new(OpCodes.Brtrue, target)
			);
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
				.MatchStartForward(
					new CodeMatch(OpCodes.Ldsfld, typeof(Furniture).GetField(nameof(Furniture.isDrawingLocationFurniture)))
				).MatchStartBackwards(
					new CodeMatch((i) => i.opcode == OpCodes.Ldloca_S)
				);
			var sourceRect = il.Instruction.operand;
			il	.MatchStartForward(
					new(OpCodes.Ldarg_0),
					new(OpCodes.Ldfld, typeof(Furniture).GetField(nameof(Furniture.sourceRect))),
					new(OpCodes.Call, typeof(NetFieldBase<Rectangle, NetRectangle>).GetMethod("op_Implicit"))
				).RemoveInstructions(3)
				.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldloc_S, sourceRect)
				);
			return il.InstructionEnumeration();
		}
	}
}
