using BmFont;
using HappyHomeDesigner.Integration;
using HappyHomeDesigner.Patches;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData.Shops;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.Mods;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace HappyHomeDesigner.Framework
{
	public static class ModUtilities
	{
		private static readonly Func<char, bool, Rectangle> SpriteTextSourceRect
			= typeof(SpriteText).GetMethod("getSourceRectForChar", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			.CreateDelegate<Func<char, bool, Rectangle>>();

		[Flags]
		public enum CatalogType {None = 0, Furniture = 1, Wallpaper = 2, Collector = 4};

		private static readonly AccessTools.FieldRef<MouseWheelScrolledEventArgs, int> ScrollOldValue =
			GetDirect<MouseWheelScrolledEventArgs, int>(nameof(MouseWheelScrolledEventArgs.OldValue));

		public static void Suppress(this SButton button)
		{
			ModEntry.helper.Input.Suppress(button);
		}

		public static AccessTools.FieldRef<T, F> GetDirect<T, F>(string PropertyName)
		{
			if (typeof(T).GetProperty(PropertyName, typeof(F)) is null)
				throw new InvalidOperationException($"Property '{PropertyName}' with value type '{nameof(F)}' does not exist on type '{nameof(T)}'");

			var field = typeof(T).GetField($"<{PropertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
				?? throw new InvalidOperationException($"A backing field could not be found for property '{PropertyName}' on type '{nameof(T)}'");

			return AccessTools.FieldRefAccess<T, F>(field);
		}

		public static bool CanDelete(this Item item, ICollection<string> knownIDs)
		{
			return
				item is not null && item is not Tool && item.canBeTrashed() && 
				(item is not StorageFurniture storage || storage.heldItems.Count == 0) &&
				!item.modData.ContainsKey(CraftablePlacement.UNIQUE_ITEM_FLAG) &&
				knownIDs is not null && knownIDs.Contains(item.QualifiedItemId);
		}

		public static bool TryFindAssembly(string name, [NotNullWhen(true)] out Assembly? assembly)
		{
			assembly = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == name).FirstOrDefault();
			return assembly is not null;
		}

		public static void Suppress(this MouseWheelScrolledEventArgs e)
		{
			// suppress game
			Game1.oldMouseState = Game1.input.GetMouseState();

			// suppress event
			ScrollOldValue(e) = e.NewValue;
		}

		public static bool TryGetGenericOf(this Type type, int index, [NotNullWhen(true)] out Type generic)
		{
			generic = null;
			if (!type.IsGenericType)
				return false;

			var generics = type.GetGenericArguments();
			if (generics.Length <= index)
				return false;

			generic = generics[index];
			return true;
		}

		public static void DrawFrame(this SpriteBatch b, Texture2D texture, Rectangle dest, Rectangle source, 
			int padding, int scale, Color color, int top = 0)
		{
			int destPad = padding * scale;
			int dTop = top * scale + destPad;
			int sTop = top + padding;

			// top
			int dy = dest.Y;
			int sy = source.Y;
			b.Draw(texture,
				new Rectangle(dest.X, dy, destPad, dTop),
				new Rectangle(source.X, sy, padding, sTop),
				color);
			b.Draw(texture,
				new Rectangle(dest.X + destPad, dy, dest.Width - destPad * 2, dTop),
				new Rectangle(source.X + padding, sy, source.Width - padding * 2, sTop),
				color);
			b.Draw(texture,
				new Rectangle(dest.X + dest.Width - destPad, dy, destPad, dTop),
				new Rectangle(source.X + source.Width - padding, sy, padding, sTop),
				color
				);

			// mid
			dy += dTop;
			sy += sTop;
			b.Draw(texture,
				new Rectangle(dest.X, dy, destPad, dest.Height - destPad * 2),
				new Rectangle(source.X, sy, padding, source.Height - padding * 2),
				color);
			b.Draw(texture,
				new Rectangle(dest.X + dest.Width - destPad, dy, destPad, dest.Height - destPad * 2),
				new Rectangle(source.X + source.Width - padding, sy, padding, source.Height - padding * 2),
				color
				);

			// bottom
			dy = dest.Y + dest.Height - destPad;
			sy = source.Y + source.Height - padding;
			b.Draw(texture,
				new Rectangle(dest.X, dy, destPad, destPad),
				new Rectangle(source.X, sy, padding, padding),
				color);
			b.Draw(texture,
				new Rectangle(dest.X + destPad, dy, dest.Width - destPad * 2, destPad),
				new Rectangle(source.X + padding, sy, source.Width - padding * 2, padding),
				color);
			b.Draw(texture,
				new Rectangle(dest.X + dest.Width - destPad, dy, destPad, destPad),
				new Rectangle(source.X + source.Width - padding, sy, padding, padding),
				color
				);
		}

		public static T ToDelegate<T>(this MethodInfo method, object target) where T : Delegate
			=> (T)Delegate.CreateDelegate(typeof(T), target, method);

		public static void QuickBind(this IGMCM gmcm, IManifest manifest, object config, string name, 
			bool titleOnly = false, string[] allowedValues = null, Func<string, string> formatValue = null)
		{
			var prop = config.GetType().GetProperty(name) ??
				throw new ArgumentException($"Public property of name '{name}' not found on config.");

			if (titleOnly)
				gmcm.SetTitleScreenOnlyForNextOptions(manifest, true);

			var title = $"config.{prop.Name}.name";
			var desc = $"config.{prop.Name}.desc";
			var type = prop.PropertyType;

			if (type == typeof(bool))
				gmcm.AddBoolOption(manifest,
				prop.GetMethod!.ToDelegate<Func<bool>>(config),
				prop.SetMethod!.ToDelegate<Action<bool>>(config),
				() => ModEntry.i18n.Get(title),
				() => ModEntry.i18n.Get(desc));

			else if (type == typeof(KeybindList))
				gmcm.AddKeybindList(manifest,
				prop.GetMethod!.ToDelegate<Func<KeybindList>>(config),
				prop.SetMethod!.ToDelegate<Action<KeybindList>>(config),
				() => ModEntry.i18n.Get(title),
				() => ModEntry.i18n.Get(desc));

			else if (type == typeof(string))
				gmcm.AddTextOption(manifest,
				prop.GetMethod!.ToDelegate<Func<string>>(config),
				prop.SetMethod!.ToDelegate<Action<string>>(config),
				() => ModEntry.i18n.Get(title),
				() => ModEntry.i18n.Get(desc),
				allowedValues, formatValue);

			else
				throw new ArgumentException($"Config property '{name}' is of unsupported type '{type.FullName}'.");

			if (titleOnly)
				gmcm.SetTitleScreenOnlyForNextOptions(manifest, false);
		}

		public static void QuickPage(this IGMCM gmcm, IManifest manifest, string name, string owner = "")
		{
			gmcm.AddPage(manifest, owner);
			gmcm.AddPageLink(manifest, name,
				() => ModEntry.i18n.Get($"config.{name}.name"),
				() => ModEntry.i18n.Get($"config.{name}.desc")
			);
			gmcm.AddPage(manifest, name, () => ModEntry.i18n.Get($"config.{name}.name"));
		}

		public static bool AssertValid(this CodeMatcher matcher, string message, LogLevel level = LogLevel.Debug)
		{
			if (message is not null && matcher.IsInvalid)
				ModEntry.monitor.Log(message, level);
			return matcher.IsValid;
		}

		public static void Log(this ITranslationHelper helper, string key, object? args = null, LogLevel level = LogLevel.Debug)
		{
			ModEntry.monitor.Log(helper.Get(key, args), level);
		}

		public static int Find<T>(this IReadOnlyList<T> items, T which) where T : class
		{
			int count = items.Count;
			for (int i = 0; i < count; i++)
				if (items[i] == which)
					return i;
			return -1;
		}

		public static Rectangle ToRect(this xTile.Dimensions.Rectangle rect)
			=> new(rect.X, rect.Y, rect.Width, rect.Height);

		public static IEnumerable<ISalable> GetAdditionalCatalogItems(this IEnumerable<ISalable> original, string ID)
		{
			return original;
		}

		public static bool CountsAsCatalog(this ShopMenu shop, bool ignore_config = false)
		{
			return shop.ShopId switch
			{
				"Furniture Catalogue" => (ignore_config || ModEntry.config.ReplaceFurnitureCatalog),
				"Catalogue" => (ignore_config || ModEntry.config.ReplaceWallpaperCatalog),
				_ =>
					(ignore_config || ModEntry.config.ReplaceRareCatalogs) &&
					shop.ShopData is ShopData data &&
					data.CustomFields is Dictionary<string, string> fields &&
					fields.ContainsKey("HappyHomeDesigner/Catalogue")
			};
		}

		public static IEnumerable<ISalable> GenerateCombined(CatalogType catalog)
		{
			IEnumerable<ISalable> output = [];
			var shopData = DataLoader.Shops(Game1.content);

			if (ModEntry.config.EarlyDeluxe && catalog.HasFlag(CatalogType.Furniture) && catalog.HasFlag(CatalogType.Wallpaper))
				catalog |= CatalogType.Collector;

			if (catalog.HasFlag(CatalogType.Furniture) && shopData.TryGetValue("Furniture Catalogue", out var data))
				output = output.Concat(ShopBuilder.GetShopStock("Furniture Catalogue", data).Keys);

			if (catalog.HasFlag(CatalogType.Wallpaper) && shopData.TryGetValue("Catalogue", out data))
				output = output.Concat(ShopBuilder.GetShopStock("Catalogue", data).Keys);

			if (catalog.HasFlag(CatalogType.Collector))
				foreach ((var id, var sdata) in shopData)
					if (
						sdata.CustomFields is Dictionary<string, string> fields && 
						fields.ContainsKey("HappyHomeDesigner/Catalogue")
					)
						output = output.Concat(ShopBuilder.GetShopStock(id, sdata).Keys);

			return output;
		}

		public static bool TryPatch(this Harmony harmony, MethodInfo? method, HarmonyMethod? prefix = null, 
			HarmonyMethod? postfix = null, HarmonyMethod? transpiler = null, HarmonyMethod? finalizer = null, 
			[CallerMemberName] string? source = null)
		{
			try
			{
				harmony.Patch(method, prefix, postfix, transpiler, finalizer);
			} 
			catch (Exception e)
			{
				ModEntry.monitor.Log($"Failed to patch {method?.Name ?? "NULL"} from {source ?? "NULL"}:\t {e}", LogLevel.Error);
				return false;
			}
			return true;
		}

		public static Color Mult(this Color a, Color b)
			=> new(
				(a.R / 255f) * (b.R / 255f),
				(a.G / 255f) * (b.G / 255f),
				(a.B / 255f) * (b.B / 255f),
				(a.A / 255f) * (b.A / 255f)
			);

		public static bool TryGetModInfo(this string ID, out IModInfo info)
		{
			for (int i = 0; i < ID.Length; i++)
			{
				if (ID[i] == '_')
				{
					var id = ID[..i];
					if (ModEntry.helper.ModRegistry.IsLoaded(id))
					{
						info = ModEntry.helper.ModRegistry.Get(id);
						return true;
					}
				}
			}
			info = null;
			return false;
		}

		public static string SanitizeFilename(this string fname)
		{
			return string.Create(fname.Length, fname, static (o, s) =>
			{
				for (int i = 0; i < o.Length; i++) {
					char c = s[i];
					o[i] = c switch
					{
						>= '0' and <= '9' => c,
						>= 'a' and <= 'z' => c,
						>= 'A' and <= 'Z' => c,
						'.' or '_' or '-' => c,
						_ => '_'
					};
				}
			});
		}

		public static Dictionary<string, string> Get(this ModDataDictionary dict)
		{
			return new Dictionary<string, string>(dict.Pairs);
		}

		public static void tryHover(this ClickableTextureComponent c, int x, int y, ref string hoverText)
		{
			if (c.containsPoint(x, y))
				hoverText = c.hoverText;
		}

		public static void DrawStringOffset(this SpriteBatch batch, string text, int x, int y, int width, Color color, Vector2[] offsets, bool center)
		{
			List<string> lines = [];

			if (LocalizedContentManager.CurrentLanguageCode is
				LocalizedContentManager.LanguageCode.zh or
				LocalizedContentManager.LanguageCode.ja or
				LocalizedContentManager.LanguageCode.th
			)
			{
				string cum = "";

				foreach (var match in (IList<Match>)Game1.asianSpacingRegex.Matches(text))
				{
					var s = match.Value;
					if (SpriteText.getWidthOfString(cum + s) > width)
					{
						lines.Add(cum);
						cum = s;
					}
					else
					{
						cum += ' ' + s;
					}
				}
				if (lines[^1] != cum)
					lines.Add(cum);
			}
			else
			{
				int last = 0;
				string cum = "";

				for (int i = 0; i <= text.Length; i++)
				{
					if (i == text.Length || text[i] is ' ' or '^')
					{
						if (last < i)
						{
							var s = text[last..i];
							if ((i < text.Length && text[i] is '^') || SpriteText.getWidthOfString(cum + s) > width)
							{
								lines.Add(cum);
								cum = s;
							}
							else
							{
								if (cum.Length is 0)
									cum = s;
								else
									cum += ' ' + s;
							}
						}

						if (i == text.Length)
							lines.Add(cum);

						last = i + 1;
					}
				}
			}

			int c = 0;
			int lineHeight = (int)(18f * SpriteText.FontPixelZoom);
			y -= (lineHeight * lines.Count) / 2;
			foreach (var line in lines)
			{
				DrawStringOffsetLine(batch, line, x, y, width, color, offsets, center, c);
				c += line.Length;
				y += lineHeight;
			}
		}

		private static void DrawStringOffsetLine(SpriteBatch batch, string text, int x, int y, int width, Color color, Vector2[] offsets, bool center, int index)
		{
			var w = SpriteText.getWidthOfString(text);
			if (center)
				x -= w / 2;


			// mostly copied from SpriteText, with some simplification
			Vector2 pos = new(x, y);

			if (SpriteText.FontPixelZoom < 4f && 
				LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.ko && 
				LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.zh
			)
				pos.Y += (int)((4f - SpriteText.FontPixelZoom) * 4f);

			if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko)
				pos.Y -= 8;
			else if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.zh)
				pos.Y += 4;

			int accum = 0;

			for (int i = 0; i < text.Length; i++)
			{
				var c = text[i];

				if (
					LocalizedContentManager.CurrentLanguageLatin || SpriteText.forceEnglishFont ||
					(LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru && !Game1.options.useAlternateFont)
				)
				{
					float tempzoom = SpriteText.fontPixelZoom;
					if (SpriteText.forceEnglishFont)
						SpriteText.fontPixelZoom = 3f;

					accum = 0;
					bool upper = char.IsUpper(c) || c is 'ß';
					Vector2 spriteFontOffset = new(0f, -1 + (upper ? (-3) : 0));
					if (c is 'Ç')
						spriteFontOffset.Y += 2f;

					Rectangle srcRect = SpriteTextSourceRect(c, false);
					batch.Draw(
						SpriteText.coloredTexture, 
						pos + spriteFontOffset * SpriteText.FontPixelZoom + offsets[(i + index) % offsets.Length], 
						srcRect, color, 0f, Vector2.Zero, SpriteText.FontPixelZoom, SpriteEffects.None, 0f
					);

					if (i < text.Length - 1)
						pos.X += 8f * SpriteText.FontPixelZoom + accum + SpriteText.getWidthOffsetForChar(text[i + 1]) * SpriteText.FontPixelZoom;

					SpriteText.fontPixelZoom = tempzoom;
					continue;
				}

				if (SpriteText.characterMap.TryGetValue(c, out var fc))
				{
					Rectangle sourcerect = new(fc.X, fc.Y, fc.Width, fc.Height);
					Texture2D _texture = SpriteText.fontPages[fc.Page];
					if (SpriteText.positionOfNextSpace(text, i, (int)pos.X, accum) >= x + width - 4)
					{
						pos.Y += (SpriteText.FontFile.Common.LineHeight + 2) * SpriteText.FontPixelZoom;
						accum = 0;
						pos.X = x;
					}
					Vector2 position2 = new(pos.X + fc.XOffset * SpriteText.FontPixelZoom, pos.Y + fc.YOffset * SpriteText.FontPixelZoom);
					position2 += offsets[(i + index) % offsets.Length];
					if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru)
					{
						Vector2 offset = new Vector2(-1f, 1f) * SpriteText.FontPixelZoom;
						batch.Draw(_texture, position2 + offset, sourcerect, color * SpriteText.shadowAlpha, 0f, Vector2.Zero, SpriteText.FontPixelZoom, SpriteEffects.None, 0f);
						batch.Draw(_texture, position2 + new Vector2(0f, offset.Y), sourcerect, color * SpriteText.shadowAlpha, 0f, Vector2.Zero, SpriteText.FontPixelZoom, SpriteEffects.None, 0f);
						batch.Draw(_texture, position2 + new Vector2(offset.X, 0f), sourcerect, color * SpriteText.shadowAlpha, 0f, Vector2.Zero, SpriteText.FontPixelZoom, SpriteEffects.None, 0f);
					}
					batch.Draw(_texture, position2, sourcerect, color, 0f, Vector2.Zero, SpriteText.FontPixelZoom, SpriteEffects.None, 0f);
					pos.X += fc.XAdvance * SpriteText.FontPixelZoom;
				}
			}
		}
	}
}
