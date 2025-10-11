using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Patches;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Mods;
using StardewValley.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

		public static Func<string, int, bool> IsVariationDisabled;
		public static Action<Furniture, Season, List<Furniture>> VariantsOfFurniture;
		public static Action<StardewValley.Object, Season, List<StardewValley.Object>> VariantsOfCraftable;
		private static Func<HashSet<string>> CreateIndexImpl;
		private static HashSet<string> TextureIndex = [];

		public delegate void TextureSourceGetter(ModDataDictionary data, Rectangle defaultRect, ref Texture2D texture, ref Rectangle source);
		public static TextureSourceGetter GetTextureSource;

		internal static void Init(IModHelper helper)
		{
			Installed = false;
			if (!helper.ModRegistry.IsLoaded("PeacefulEnd.AlternativeTextures") || !AltTex.IsApplied)
				return;

			ModEntry.monitor.Log("Alternative Textures detected! Integrating...", LogLevel.Debug);

			const BindingFlags STATIC = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			string error = null;
			Type entry;
			FieldInfo manager;


			// Find assembly
			if (!ModUtilities.TryFindAssembly("AlternativeTextures", out var asm))
				error = "Failed to find AT assembly, could not integrate.";

			// Find mod entry
			else if ((entry = asm.GetType("AlternativeTextures.AlternativeTextures")) is null)
				error = "Failed to find entry point for Alternative Textures.";

			// Get handle for texture manager
			else if ((manager = entry.GetField("textureManager", STATIC)) is null)
				error = "Failed to find texture manager.";

			// Bind IsDisabled
			else if (!BindVariantDisabled(entry))
				error = "Failed to bind IsVariantDisabled.";

			// bind variant checker
			else if (!BindIndexBuilder(manager))
				error = "Failed to bind index builder.";

			// bind furniture variant factory
			else if (!TryBindVariantsOf(manager, "Furniture_", out VariantsOfFurniture))
				error = "Failed to bind Furniture variants.";

			// bind craftable variant factory
			else if (!TryBindVariantsOf(manager, "Craftable_", out VariantsOfCraftable))
				error = "Failed to bind Craftable variants.";

			// bind texture getter
			else if (!BindTextureGetter(manager))
				error = "Failed to bind alternate texture getter";


			if (error is null)
			{
				ModEntry.monitor.Log("Integration successful.", LogLevel.Debug);
				Installed = true;
			} 
			else
			{
				ModEntry.monitor.Log("Error integrating Alternative Textures: " + error, LogLevel.Error);
				Installed = false;
			}
		}

		public static void UpdateIndex()
		{
			if (!Installed)
				return;

			var timer = Stopwatch.StartNew();
			TextureIndex = CreateIndexImpl();
			timer.Stop();

			ModEntry.monitor.Log($"Indexed {TextureIndex.Count} alternative textures in {timer.ElapsedMilliseconds} ms.", LogLevel.Debug);
		}

		public static bool HasVariant(string id, string name, string season)
		{
			return
				TextureIndex.Contains(id) || TextureIndex.Contains(name) ||
				TextureIndex.Contains($"{id}_{season}") || TextureIndex.Contains($"{name}_{season}");
		}

		private static bool BindVariantDisabled(Type entry)
		{
			var cfg = entry.GetField("modConfig", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
			if (cfg is null)
				return false;

			var method = cfg.GetType().GetMethod(
				"IsTextureVariationDisabled", 
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				[typeof(string), typeof(int)]
			);
			if (method is null)
				return false;

			IsVariationDisabled = method.CreateDelegate<Func<string, int, bool>>(cfg);
			return true;
		}

		private static bool BindIndexBuilder(FieldInfo manager)
		{
			/*
			* BuildIndex() {
			*	HashSet<string> index = new(StringComparer.OrdinalIgnoreCase);
			*	var list = manager._alternativeTextures;
			*	for (int i = list.Count; i >= 0; i--)
			*		index.Add(list[i].GetNameWithSeason());
			*	return index;
			* }
			*/

			var field = manager.FieldType.GetField("_alternativeTextures", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (field is null)
				return false;

			var model = field.FieldType.GenericTypeArguments[0];
			var getName = model.GetMethod("GetNameWithSeason", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (getName is null)
				return false;

			var set = Expression.Variable(typeof(HashSet<string>));
			var loopBreak = Expression.Label();
			var i = Expression.Variable(typeof(int));
			var list = Expression.Variable(field.FieldType);
			var ret = Expression.Label(typeof(HashSet<string>));

			var body = Expression.Block([set, i, list],
				Expression.Assign(set,
					Expression.New(
						typeof(HashSet<string>).GetConstructor([typeof(IEqualityComparer<string>)]),
						Expression.Constant(StringComparer.OrdinalIgnoreCase)
					)
				),
				Expression.Assign(list, Expression.Field(Expression.Field(null, manager), field)),
				Expression.Assign(i, Expression.Subtract(
					Expression.Call(list, field.FieldType.GetProperty("Count").GetMethod),
					Expression.Constant(1)
				)),
				Expression.Loop(
					Expression.IfThenElse(
						Expression.GreaterThanOrEqual(i, Expression.Constant(0)),
						Expression.Block(
							Expression.Call(set, typeof(HashSet<string>).GetMethod("Add"),
								Expression.Call(
									Expression.Call(list, field.FieldType.GetMethod("get_Item"), i),
									getName
								)
							),
							Expression.PostDecrementAssign(i)
						),
						Expression.Break(loopBreak)
					),
					loopBreak
				),
				Expression.Return(ret, set),
				Expression.Label(ret, set)
			);

			try
			{
				CreateIndexImpl = Expression.Lambda<Func<HashSet<string>>>(body).Compile();
			}

			catch (Exception ex)
			{
				ModEntry.monitor.Log(ex.ToString(), LogLevel.Trace);
				return false;
			}

			return true;
		}

		private static bool BindTextureGetter(FieldInfo manager)
		{
			/*
			* GetTextureSource(data, defaultRect, ref texture, ref source) {
			*	if (data.TryGetValue("AlternativeTextureName", out var id)) {
			*		var model = manager.GetSpecificTextureModel(id);
			*		if (model is not null) {
			*			var variant = int.Parse(data["AlternativeTextureVariation"]);
			*			source.X -= defaultRect.X;
			*			source.Y = model.GetTextureOffset(textureVariation);
			*			texture = textureModel.GetTexture(textureVariation);
			*		}
			*	}
			* }
			*/

			var data = Expression.Parameter(typeof(ModDataDictionary));
			var defaultRect = Expression.Parameter(typeof(Rectangle));
			var texture = Expression.Parameter(typeof(Texture2D).MakeByRefType());
			var source = Expression.Parameter(typeof(Rectangle).MakeByRefType());

			var getter = manager.FieldType.GetMethod("GetSpecificTextureModel");

			var id = Expression.Variable(typeof(string));
			var model = Expression.Variable(getter.ReturnType);
			var variant = Expression.Variable(typeof(int));

			var body = Expression.Block([id, model, variant], [
				Expression.IfThen(
					Expression.Call(
						data, typeof(ModDataDictionary).GetMethod(nameof(ModDataDictionary.TryGetValue)),
						Expression.Constant("AlternativeTextureName"), id
					),
					Expression.IfThen(
						Expression.NotEqual(
							Expression.Assign(model, 
								Expression.Call(Expression.Field(null, manager), getter, id)
							),
							Expression.Constant(null)
						),
						Expression.Block(
							Expression.Assign(variant, 
								Expression.Call(typeof(int).GetMethod(nameof(int.Parse), [typeof(string)]), 
									Expression.Call(
										data, 
										typeof(ModDataDictionary).GetMethod("get_Item"),
										Expression.Constant("AlternativeTextureVariation")
									)
								)
							),
							Expression.AddAssign(
								Expression.Field(source, nameof(Rectangle.X)),
								Expression.Negate(Expression.Field(defaultRect, nameof(Rectangle.X)))
							),
							Expression.Assign(
								Expression.Field(source, nameof(Rectangle.Y)),
								Expression.Call(model, model.Type.GetMethod("GetTextureOffset", [typeof(int)]), variant)
							),
							Expression.Assign(
								texture,
								Expression.Call(model, model.Type.GetMethod("GetTexture", [typeof(int)]), variant)
							)
						)
					)
				)
			]);

			try
			{
				GetTextureSource = Expression.Lambda<TextureSourceGetter>(body, data, defaultRect, texture, source).Compile();
			} catch (Exception ex)
			{
				ModEntry.monitor.Log(ex.ToString(), LogLevel.Trace);
				return false;
			}

			return true;
		}

		private static Expression HasVariantCheck(FieldInfo manager, MethodInfo getter, Expression name, Expression season, Expression isId)
		{
			return Expression.Or(
				Expression.Call(Expression.Field(null, manager), getter, name, isId),
				Expression.Call(Expression.Field(null, manager), getter,
					Expression.Call(typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string), typeof(string)]),
					name, Expression.Constant("_"), season),
					isId
				)
			);
		}

		internal static bool TryBindVariantsOf<T>(FieldInfo manager, string prefix, out Action<T, Season, List<T>> variantsOf)
			where T : Item
		{
			variantsOf = null;

			var mg = manager.FieldType.GetMethod("GetAvailableTextureModels", [ typeof(string), typeof(string), typeof(Season) ]);
			if (!mg.ReturnType.TryGetGenericOf(0, out var modelType))
				return false;

			var seasonGetter = modelType.GetProperty("Season", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetMethod;
			variantsOf = (source, season, list) => {
				var tm = manager.GetValue(null);
				IList models = mg.Invoke(tm, [prefix + source.ItemId, prefix + source.Name, season]) as IList;
				for (int i = 0; i < models.Count; i++)
				{
					var m = models[i] as dynamic;
					IList manualVariants = m.ManualVariations;
					List<int> manualIndices = new(manualVariants.Count);

					for (int j = 0; j < manualVariants.Count; j++)
					{
						int index = (manualVariants[j] as dynamic).Id;
						if (index is not -1)
							manualIndices.Add(index);
					}

					if (manualIndices.Count is not 0)
					{
						for (int j = 0; j < manualIndices.Count; j++)
						{
							var furn = source.getOne() as T;
							GetVariant(manualIndices[j], m, furn, seasonGetter);

							if (!IsVariationDisabled(furn.modData[KEY_NAME], manualIndices[j]))
								list.Add(furn);
						}
					} else
					{
						int count = m.Variations;
						for (int j = 0; j < count; j++)
						{
							var furn = source.getOne() as T;
							GetVariant(j, m, furn, seasonGetter);
							if (!IsVariationDisabled(furn.modData[KEY_NAME], j))
								list.Add(furn);
						}
					}
				}
			};

			return true;
		}

		private static void GetVariant(int variant, dynamic model, Item furn, MethodBase SeasonGetter)
		{
			furn.modData[KEY_OWNER] = model.Owner;
			furn.modData[KEY_NAME] = model.GetId();
			furn.modData[KEY_VARIATION] = variant.ToString();
			furn.modData[KEY_SEASON] = SeasonGetter.Invoke(model, null);
			furn.modData[KEY_DISPLAY_NAME] = string.Empty;
		}
	}
}
