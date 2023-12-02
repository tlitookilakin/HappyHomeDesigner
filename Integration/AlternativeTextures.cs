using HappyHomeDesigner.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using static StardewValley.Menus.CharacterCustomization;

namespace HappyHomeDesigner.Integration
{
	internal class AlternativeTextures
	{
		const string KEY_OWNER = "AlternativeTextureOwner";
		const string KEY_NAME = "AlternativeTextureName";
		const string KEY_VARIATION = "AlternativeTextureVariation";
		const string KEY_SEASON = "AlternativeTextureSeason";
		const string KEY_DISPLAY_NAME = "AlternativeTextureDisplayName";

		public static bool Installed;

		public static Func<string, string, bool> HasVariant;
		public static Action<Furniture, string, List<Furniture>> VariantsOf;

		internal static void Init(IModHelper helper)
		{
			Installed = false;
			if (!helper.ModRegistry.IsLoaded("PeacefulEnd.AlternativeTextures"))
				return;

			ModEntry.monitor.Log("Alternative Textures detected! Integrating...", LogLevel.Debug);

			if (!ModUtilities.TryFindAssembly("AlternativeTextures", out var asm))
			{
				ModEntry.monitor.Log("Failed to find AT assembly, could not integrate.");
				return;
			}
			var entry = asm.GetType("AlternativeTextures.AlternativeTextures");
			if (entry is null)
			{
				ModEntry.monitor.Log("Failed to find entry point for Alternative Textures.", LogLevel.Warn);
				return;
			}
			var manager = entry.GetField("textureManager", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (manager is null)
			{
				ModEntry.monitor.Log("Failed to find texture manager.", LogLevel.Warn);
				return;
			}
			if (!BindHasVariant(manager))
			{
				ModEntry.monitor.Log("Failed to bind HasVariant", LogLevel.Warn);
				return;
			}
			if (!AltVariantsOf(manager))
			{
				ModEntry.monitor.Log("Failed to bind VariantOf", LogLevel.Warn);
				return;
			}
			ModEntry.monitor.Log("Integration successful.", LogLevel.Debug);
			Installed = true;
		}

		private static bool BindHasVariant(FieldInfo manager)
		{
			/*
			* HasVariant(name, season)
			*	=>	AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(name) || 
			*		AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(name + "_" + season)
			*/

			var getter = manager.FieldType.GetMethod("DoesObjectHaveAlternativeTexture", new[] { typeof(string) });
			var name = Expression.Parameter(typeof(string));
			var season = Expression.Parameter(typeof(string));
			var body = Expression.Or(
				Expression.Call(Expression.Field(null, manager), getter, name),
				Expression.Call(Expression.Field(null, manager), getter, 
					Expression.Call(typeof(string).GetMethod(nameof(string.Concat), new[] {typeof(string), typeof(string), typeof(string)}),
					name, Expression.Constant("_"), season)
				)
			);
			try
			{
				HasVariant = Expression.Lambda<Func<string, string, bool>>(body, name, season).Compile();
			} catch (Exception ex)
			{
				ModEntry.monitor.Log(ex.ToString(), LogLevel.Trace);
				return false;
			}
			return true;
		}

		internal static bool AltVariantsOf(FieldInfo manager)
		{
			var mg = manager.FieldType.GetMethod("GetAvailableTextureModels");
			if (!mg.ReturnType.TryGetGenericOf(0, out var modelType))
				return false;
			var seasonGetter = modelType.GetProperty("Season", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				.GetMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(modelType, typeof(string))) as Func<object, string>;

			VariantsOf = (source, season, list) => {
				var tm = manager.GetValue(null);
				IList models = mg.Invoke(tm, new[] { "Furniture_" + source.Name, season }) as IList;
				for (int i = 0; i < models.Count; i++)
				{
					var m = models[i] as dynamic;
					IList manVariants = m.ManualVariations;

					if (manVariants.Count is 0)
					{
						int count = m.Variations;
						for (int j = 0; j < count; j++)
						{
							var furn = source.getOne() as Furniture;
							GetVariant(j, m, furn, seasonGetter);
							list.Add(furn);
						}
						return;
					}
					for (int j = 0; j < manVariants.Count; j++)
					{
						if ((manVariants[j] as dynamic).Id is -1)
						{
							int count = m.Variations;
							for (j = 0; j < count; j++)
							{
								var furn = source.getOne() as Furniture;
								GetVariant(j, m, furn, seasonGetter);
								list.Add(furn);
							}
							return;
						}
					}
					for (int j = 0; j < manVariants.Count; j++)
					{
						var furn = source.getOne() as Furniture;
						GetVariant((manVariants[j] as dynamic).Id, m, furn, seasonGetter);
						list.Add(furn);
					}
				}
			};

			return true;
		}

		private static void GetVariant(int variant, dynamic model, Furniture furn, Func<object, string> SeasonGetter)
		{
			furn.modData[KEY_OWNER] = model.Owner;
			furn.modData[KEY_NAME] = model.GetId();
			furn.modData[KEY_VARIATION] = variant.ToString();
			furn.modData[KEY_SEASON] = SeasonGetter(model);
			furn.modData[KEY_DISPLAY_NAME] = string.Empty;
		}

		private static bool BindVariantsOf(FieldInfo manager)
		{
			/*
			* void VariantsOf(Furniture source, string season, List<Furniture> list) {
			*	List<TextureModel> models = AlternativeTextures.textureManager.GetAvailableTextureModels("Furniture_" + source.Name, season);
			*	for (int i = 0; i < models.Count; i++) {
			*		TextureModel m = models[i];
			*		List<Variation> manVariants = m.ManualVariations;
			*		for (int j = 0; j < manVariants.Count; j++) {
			*			if (manVariants[j].Id == -1) {
			*				int variants = m.Variations;
			*				<loop>
			*				return;
			*			}
			*		}
			*		<loop>
			*	}
			* }
			* 
			* loop:
			*		for (int j = 0; j < variants.Count; j++) {
			*			var new_furn = source.getOne();
			*			new_furn.modData[KEY_OWNER] = m.Owner;
			*			new_furn.modData[KEY_NAME] = m.GetId();
			*			new_furn.modData[KEY_VARIATION] = variants[i].ToString();
			*			new_furn.modData[KEY_SEASON] = m.Season;
			*			new_furn.modData[KEY_DISPLAY_NAME] = string.Empty;
			*			list.Add(new_furn);
			*		}
			*/

			var getter = manager.FieldType.GetMethod("GetAvailableTextureModels");
			if (getter is null)
				return false;
			if (!getter.ReturnType.TryGetGenericOf(0, out var texModel))
				return false;

			var variantProp = texModel.GetProperty("ManualVariations");
			if (variantProp is null)
				return false;
			if (!variantProp.PropertyType.TryGetGenericOf(0, out var variantModel))
				return false;

			var source = Expression.Parameter(typeof(Furniture));
			var season = Expression.Parameter(typeof(string));
			var list = Expression.Parameter(typeof(List<Furniture>));

			var i = Expression.Variable(typeof(int));
			var j = Expression.Variable(typeof(int));
			var m = Expression.Variable(texModel);
			var models = Expression.Variable(getter.ReturnType);
			var variants = Expression.Variable(variantProp.PropertyType);
			var variantCount = Expression.Variable(typeof(int));

			var breakOuter = Expression.Label("outerLoop");
			var breakInner = Expression.Label("innerLoop");

			var furn = Expression.Variable(typeof(Furniture));
			var furnData = Expression.Field(furn, typeof(Furniture).GetField(nameof(Furniture.modData)));
			var modData = Expression.Variable(typeof(ModDataDictionary));

			var body = Expression.Block(
				new[] { models, i },
				Expression.Assign(models, Expression.Call(Expression.Field(null, manager), getter, 
					Expression.Call(typeof(string).GetMethod(nameof(string.Concat), new[] {typeof(string), typeof(string)}),
						Expression.Constant("Furniture_"), Expression.Property(source, typeof(Furniture).GetProperty(nameof(Furniture.Name)))
					),
					season
				)),
				ForInLoop(i, Expression.LessThan(i, Expression.Property(models, getter.ReturnType.GetProperty("Count"))), 
					Expression.Block(
						new[] { j, variants, m, variantCount, modData, furn },
						Expression.Assign(m, Expression.Property(models, getter.ReturnType.GetProperty("Item"), i)),
						Expression.Assign(variants, Expression.Property(m, variantProp)),
						Expression.Assign(variantCount, Expression.Property(variants, variantProp.PropertyType.GetProperty("Count"))),
						ForInLoop(j, Expression.LessThan(j, variantCount), 
							Expression.IfThen(
								Expression.Equal(
									Expression.Property(
										Expression.Property(variants, variantProp.PropertyType.GetProperty("Item"), j),
										variantModel.GetProperty("Id")
									),
									Expression.Constant(-1)
								),
								Expression.Block(
									Expression.Assign(variantCount, Expression.Property(m, texModel.GetProperty("Variations"))),
									ForInLoop(j, Expression.LessThan(j, variantCount), 
										Expression.Block(
											Expression.Assign(furn, 
												Expression.Convert(Expression.Call(source, typeof(Furniture).GetMethod(nameof(Furniture.getOne))), typeof(Furniture))
											),
											Expression.Assign(modData, Expression.Field(furn, typeof(Furniture).GetField(nameof(Furniture.modData)))),
											AssignVariant(j, m, modData, texModel),
											Expression.Call(list, typeof(List<Furniture>).GetMethod(nameof(List<Furniture>.Add)), furn)
										)
									),
									Expression.Goto(breakOuter)
								)
							)
						),
						ForInLoop(j, Expression.LessThan(j, variantCount),
							Expression.Block(
								Expression.Assign(furn, 
									Expression.Convert(Expression.Call(source, typeof(Furniture).GetMethod(nameof(Furniture.getOne))), typeof(Furniture))
								),
								Expression.Assign(modData, Expression.Field(furn, typeof(Furniture).GetField(nameof(Furniture.modData)))),
								AssignVariant(
									Expression.Property(Expression.Property(variants, variantProp.PropertyType.GetProperty("Item"), j), variantModel.GetProperty("Id")), 
									m, modData, texModel),
								Expression.Call(list, typeof(List<Furniture>).GetMethod(nameof(List<Furniture>.Add)), furn)
							)
						)
					)
				),
				Expression.Label(breakOuter)
			);

			VariantsOf = Expression.Lambda<Action<Furniture, string, List<Furniture>>>(body, source, season, list).Compile();

			return true;
		}

		private static Expression AssignVariant(Expression variant, Expression model, Expression data, Type modelType)
		{
			var dataIndex = typeof(ModDataDictionary).GetProperty("Item");
			return Expression.Block(
				Expression.Assign(Expression.Property(data, dataIndex, Expression.Constant(KEY_OWNER)), 
				Expression.Property(model, modelType.GetProperty("Owner"))),
				Expression.Assign(Expression.Property(data, dataIndex, Expression.Constant(KEY_NAME)), 
				Expression.Call(model, modelType.GetMethod("GetId"))),
				Expression.Assign(Expression.Property(data, dataIndex, Expression.Constant(KEY_VARIATION)), 
				Expression.Call(variant, typeof(int).GetMethod(nameof(int.ToString), Array.Empty<Type>()))),
				Expression.Assign(Expression.Property(data, dataIndex, Expression.Constant(KEY_SEASON)), 
				Expression.Property(model, modelType.GetProperty("Season", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))),
				Expression.Assign(Expression.Property(data, dataIndex, Expression.Constant(KEY_DISPLAY_NAME)), 
				Expression.Constant(string.Empty))
			);
		}

		private static Expression ForInLoop(Expression i, Expression condition, Expression Block)
		{
			var exit = Expression.Label();
			return Expression.Block(
				Expression.Assign(i, Expression.Constant(0)),
				Expression.Loop(
					Expression.IfThenElse(condition, Expression.Block(Block, Expression.Increment(i)), Expression.Break(exit))
				),
				Expression.Label(exit)
			);
		}
	}
}
