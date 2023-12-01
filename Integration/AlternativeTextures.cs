using HappyHomeDesigner.Framework;
using StardewModdingAPI;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

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
			if (!BindVariantsOf(manager))
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

		private static bool BindVariantsOf(FieldInfo manager)
		{
			/*
			* void VariantsOf(Furniture source, string season, List<Furniture> list) {
			*	List<TextureModel> models = AlternativeTextures.textureManager.GetAvailableTextureModels("Furniture_" + source.Name, season);
			*	for (int i = 0; i < models.Count; i++) {
			*		TextureModel m = models[i];
			*		List<Variant> variants = m.Variations;
			*		for (int j = 0; j < variants.Count; j++) {
			*			var new_furn = source.getOne();
			*			new_furn.modData[KEY_OWNER] = m.Owner;
			*			new_furn.modData[KEY_NAME] = m.GetId();
			*			new_furn.modData[KEY_VARIATION] = variants[i].ToString();
			*			new_furn.modData[KEY_SEASON] = m.Season;
			*			new_furn.modData[KEY_DISPLAY_NAME] = string.Empty;
			*			list.Add(new_furn);
			*		}
			*	}
			* }
			*/

			var getter = manager.FieldType.GetMethod("GetAvailableTextureModels");
			if (getter is null)
				return false;
			if (!getter.ReturnType.TryGetGenericOf(0, out var texModel))
				return false;

			var variantProp = texModel.GetProperty("Variations");
			if (variantProp is null)
				return false;
			if (!variantProp.PropertyType.TryGetGenericOf(0, out var variantModel))
				return false;

			var source = Expression.Parameter(typeof(Furniture));
			var season = Expression.Parameter(typeof(string));
			var list = Expression.Parameter(typeof(IList<Furniture>));

			var i = Expression.Variable(typeof(int));
			var j = Expression.Variable(typeof(int));
			var m = Expression.Variable(texModel);
			var models = Expression.Variable(getter.ReturnType);
			var variants = Expression.Variable(variantProp.PropertyType);
			var furn = Expression.Variable(typeof(Furniture));
			var furnData = Expression.Field(furn, typeof(Furniture).GetField(nameof(Furniture.modData)));
			var dataIndex = typeof(Dictionary<string, string>).GetProperty("Item");

			var breakOuter = Expression.Label("outerLoop");
			var breakInner = Expression.Label("innerLoop");

			var body = Expression.Block(
				Expression.Assign(models, Expression.Call(Expression.Field(null, manager), getter, 
					Expression.Call(typeof(string).GetMethod(nameof(string.Concat), new[] {typeof(string), typeof(string)}),
						Expression.Constant("Furniture_"), Expression.Property(source, typeof(Furniture).GetProperty(nameof(Furniture.Name)))
					),
					season
				)),
				Expression.Assign(i, Expression.Constant(0)),
				Expression.Loop(
					Expression.IfThenElse(
						Expression.LessThan(i, Expression.Property(models, getter.ReturnType.GetProperty("Count"))),
						Expression.Block(
							Expression.Assign(m, Expression.Property(models, getter.ReturnType.GetProperty("Item"), i)),
							Expression.Assign(variants, Expression.Property(m, variantProp)),
							Expression.Assign(j, Expression.Constant(0)),
							Expression.Loop(
								Expression.IfThenElse(
									Expression.LessThan(j, Expression.Property(variants, variantProp.PropertyType.GetProperty("Count"))),
									Expression.Block(
										Expression.Assign(furn, Expression.Call(source, typeof(Furniture).GetMethod(nameof(Furniture.getOne)))),
										AssignData(furnData, dataIndex, KEY_OWNER, Expression.Property(m, texModel.GetProperty("Owner"))),
										AssignData(furnData, dataIndex, KEY_NAME, Expression.Call(m, texModel.GetMethod("GetId"))),
										AssignData(furnData, dataIndex, KEY_VARIATION, Expression.Call(
											Expression.Property(variants, variantProp.PropertyType.GetProperty("Item"), j),
											variantModel.GetMethod("ToString")
										)),
										AssignData(furnData, dataIndex, KEY_SEASON, Expression.Property(m, texModel.GetProperty("Season"))),
										AssignData(furnData, dataIndex, KEY_DISPLAY_NAME, Expression.Constant(string.Empty)),
										Expression.Call(list, typeof(List<Furniture>).GetMethod(nameof(List<Furniture>.Add)), furn),
										Expression.Increment(j)
									),
									Expression.Break(breakInner)
								)
							),
							Expression.Label(breakInner),
							Expression.Increment(i)
						),
						Expression.Break(breakOuter)
					)
				),
				Expression.Label(breakOuter)
			);

			return true;
		}

		private static Expression AssignData(Expression source, PropertyInfo prop, string key, Expression value)
			=> Expression.Assign(
				Expression.Property(source, prop, Expression.Constant(key)),
				value
			);
	}
}
