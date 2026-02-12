using HarmonyLib;
using StardewValley;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HappyHomeDesigner.Framework
{
	internal static class LazyItemResolver
	{
		private static bool IsLazy = false;
		private static IEnumerable<ItemQueryResult> results;

		internal static void Apply(HarmonyHelper harmony)
		{
			harmony.Patcher.Patch(
				typeof(ItemQueryResolver).GetMethod(
					nameof(ItemQueryResolver.TryResolve),
					BindingFlags.Public | BindingFlags.Static,
					typeof(LazyItemResolver).GetMethod(nameof(TryResolve)).GetParameters().Select(p => p.ParameterType).ToArray()
				),
				transpiler: new(typeof(LazyItemResolver), nameof(ModifyReturn))
			);
		}

		public static IEnumerable<ItemQueryResult> TryResolve(string query, ItemQueryContext context, ItemQuerySearchMode filter = ItemQuerySearchMode.All, string perItemCondition = null, int? maxItems = null, bool avoidRepeat = false, HashSet<string> avoidItemIds = null, Action<string, string> logError = null)
		{
			IsLazy = true;
			results = null;
			try
			{
				var output = ItemQueryResolver.TryResolve(query, context, filter, perItemCondition, maxItems, avoidRepeat, avoidItemIds, logError);
				return results ?? output;
			}
			finally
			{
				IsLazy = false; 
				results = null;
			}
		}

		private static IEnumerable<CodeInstruction> ModifyReturn(IEnumerable<CodeInstruction> codes, ILGenerator gen)
		{

			var il = new CodeMatcher(codes, gen);

			// find ToArray()
			il
				.End()
				.MatchStartBackwards(
					new CodeMatch(OpCodes.Call, typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray)).MakeGenericMethod(typeof(ItemQueryResult)))
				)
				.Advance(-1);

			var src = il.Instruction.Clone();
			var jump = gen.DefineLabel();

			// if (TryLazify(enumerable)) return []; 
			il
				.Advance(1)
				.InsertAndAdvance(
					new(OpCodes.Call, typeof(LazyItemResolver).GetMethod(nameof(TryLazify), BindingFlags.NonPublic | BindingFlags.Static)),
					new(OpCodes.Brfalse, jump),
					new(OpCodes.Call, typeof(Array).GetMethod(nameof(Array.Empty)).MakeGenericMethod(typeof(ItemQueryResult))),
					new(OpCodes.Ret),
					src.WithLabels(jump)
				);

			return il.InstructionEnumeration();
		}

		private static bool TryLazify(IEnumerable<ItemQueryResult> items)
		{
			if (!IsLazy)
				return false;

			results = items;
			return true;
		}
	}
}
