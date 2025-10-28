using HappyHomeDesigner.Framework;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HappyHomeDesigner.Patches
{
	internal class InventoryCombine
	{
		private static readonly (string slot, string held, string result)[] Fusions = [ 
			("(F)1226", "(F)1308", "(F)" + AssetManager.CATALOGUE_ID),
			("(F)" + AssetManager.CATALOGUE_ID, "(F)" + AssetManager.COLLECTORS_ID, "(F)" + AssetManager.DELUXE_ID)
		];

		public static void Apply(HarmonyHelper helper)
		{
			helper.With<InventoryMenu>(nameof(InventoryMenu.rightClick)).Transpiler(InsertCombineCheck);
		}

		private static IEnumerable<CodeInstruction> InsertCombineCheck(IEnumerable<CodeInstruction> source, ILGenerator gen)
		{
			var il = new CodeMatcher(source, gen);
			var slot = gen.DeclareLocal(typeof(Item));
			var held = gen.DeclareLocal(typeof(Item));

			LocalBuilder ret;

			// find return point
			il.End()
			.MatchStartBackwards(new CodeMatch(OpCodes.Ret))
			.MatchStartBackwards(new CodeMatch(i => i.operand is LocalBuilder));
			ret = (LocalBuilder)il.Operand;
			il.Start();

			il.MatchStartForward(
				new CodeMatch(OpCodes.Callvirt, typeof(Tool).GetMethod(nameof(Tool.attach)))
			).MatchStartForward(
				new CodeMatch(OpCodes.Leave)
			);

			if (!il.AssertValid("Fusion patch failed. Could not find match point 1."))
				return null;

			var leaveTarget = il.Instruction.operand;

			if (ModEntry.ANDROID)
			{
				il.MatchEndBackwards(
					new(OpCodes.Ldarg_0),
					new(OpCodes.Ldfld),
					new(OpCodes.Ldloc_2),
					new(OpCodes.Callvirt),
					new(OpCodes.Brfalse)
				);
			}
			else
			{
				il.MatchEndBackwards(
					new(OpCodes.Ldloc_2),
					new(OpCodes.Brfalse)
				);
			}

			if (!il.AssertValid("Fusion patch failed. Could not find match point 2."))
				return null;

			il.Advance(1)
			.CreateLabel(out var jump);

			// slot = actualInventory[i];
			if (ModEntry.ANDROID)
			{
				il.InsertAndAdvance(

					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, typeof(InventoryMenu).GetField(nameof(InventoryMenu.actualInventory))),
					new CodeInstruction(OpCodes.Ldloc_2),
					new CodeInstruction(OpCodes.Callvirt, typeof(IList<Item>).GetMethod("get_Item")),
					new CodeInstruction(OpCodes.Stloc, slot)
				);
			}
			else
			{
				il.InsertAndAdvance(
					new(OpCodes.Ldloc_2),
					new(OpCodes.Stloc, slot)
				);
			}

			il.InsertAndAdvance(

				// held = toAddTo;
				new(OpCodes.Ldarg_3),
				new(OpCodes.Stloc, held),
				
				// if (TryCombine(ref slot, ref held, playSound))
				new(OpCodes.Ldloca, slot),
				new(OpCodes.Ldloca, held),
				new(OpCodes.Ldarg_S, 4),
				new(OpCodes.Call, typeof(InventoryCombine).GetMethod(nameof(TryCombine))),
				new(OpCodes.Brfalse, jump),

				// actualInventory[i] = slot;
				new(OpCodes.Ldarg_0),
				new(OpCodes.Ldfld, typeof(InventoryMenu).GetField(nameof(InventoryMenu.actualInventory))),
				new(ModEntry.ANDROID ? OpCodes.Ldloc_2 : OpCodes.Ldloc_1),
				new(OpCodes.Ldloc, slot),
				new(OpCodes.Callvirt, typeof(IList<Item>).GetMethod("set_Item")),

				// return held;
				new(OpCodes.Ldloc, held),
				new(OpCodes.Stloc, ret),
				new(OpCodes.Leave, leaveTarget)
			);

			//var d = il.InstructionEnumeration().ToList();

			return il.InstructionEnumeration();
		}

		public static bool TryCombine(ref Item slot, ref Item held, bool playSound)
		{
			foreach (var fusion in Fusions)
			{
				if (held is null)
				{
					if (slot.QualifiedItemId == fusion.result)
					{
						slot = ItemRegistry.Create(fusion.slot);
						held = ItemRegistry.Create(fusion.held);

						if (playSound)
							Game1.playSound("pickUpItem");

						return true;
					}
				} 
				else
				{
					if ((slot.QualifiedItemId == fusion.slot && held.QualifiedItemId == fusion.held) ||
						(slot.QualifiedItemId == fusion.held && held.QualifiedItemId == fusion.slot))
					{
						slot = ItemRegistry.Create(fusion.result);
						held = null;

						if (playSound)
							Game1.playSound("axe");

						return true;
					}
				}
			}

			return false;
		}
	}
}
