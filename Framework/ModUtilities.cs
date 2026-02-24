using HappyHomeDesigner.Integration;
using HappyHomeDesigner.Patches;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Shops;
using StardewValley.Menus;
using StardewValley.Mods;
using StardewValley.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace HappyHomeDesigner.Framework
{
	public static class ModUtilities
	{
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

		public static bool TryFindAssembly(string name, [NotNullWhen(true)] out Assembly assembly)
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

		public static bool CountsAsCatalog(this ShopData shop, string id, bool ignore_config = false)
		{
			return id switch
			{
				"Furniture Catalogue" => (ignore_config || ModEntry.config.ReplaceFurnitureCatalog),
				"Catalogue" => (ignore_config || ModEntry.config.ReplaceWallpaperCatalog),
				_ =>
					(ignore_config || ModEntry.config.ReplaceRareCatalogs) &&
					shop.CustomFields is Dictionary<string, string> fields &&
					fields.ContainsKey("HappyHomeDesigner/Catalogue")
			};
		}

		public static bool ContainsAll<T>(this ICollection<T> source, IEnumerable<T> other)
		{
			if (other.TryGetNonEnumeratedCount(out var otherCount) && source.Count != otherCount)
				return false;

			int i = 0;
			foreach (var item in other)
			{
				if (i > source.Count)
					return false;

				if (!source.Contains(item))
					return false;

				i++;
			}

			return true;
		}

		public static List<string> GetCollectorShops(params IEnumerable<string> includeShops)
		{
			List<string> ret = [..includeShops];
			var shops = DataLoader.Shops(Game1.content);

			foreach ((var id, var sdata) in shops)
				if (
					sdata.CustomFields is Dictionary<string, string> fields &&
					fields.ContainsKey("HappyHomeDesigner/Catalogue")
				)
					ret.Add(id);

			return ret;
		}

		public static string GetShop(this Item obj, bool allowCustom)
		{
			return obj.QualifiedItemId switch
			{
				"(F)1308" => "Catalogue",
				"(F)1226" => "Furniture Catalogue",
				_ => null
			};
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

		public static IReadOnlyList<T> CopyWithout<T>(this IReadOnlyList<T> source, T target) where T : class
		{
			int ind = source.Find(target);
			if (ind == -1)
				return source;

			T[] output = new T[source.Count - 1];

			if (source is List<T> list)
			{
				if (ind > 0)
					list.CopyTo(0, output, 0, ind);

				if (output.Length - ind > 0)
					list.CopyTo(ind + 1, output, ind, output.Length - ind);
			}
			else
			{
                for (int i = 0; i < ind; i++)
					output[i] = source[i];

				for (int i = ind; i < output.Length; i++)
					output[i] = source[i + 1];
            }

			return output;
		}

		public static Dictionary<string, string> Get(this ModDataDictionary dict)
		{
			return new Dictionary<string, string>(dict.Pairs);
		}

		public static void tryHover(this ClickableTextureComponent c, int x, int y, ref string hoverText)
		{
			if (c.visible && c.containsPoint(x, y))
				hoverText = c.hoverText;
		}

		public static CodeInstruction GetStore(this CodeInstruction code)
		{
			return code.opcode.Value switch
			{
				0xFE or 0x0C or 0x0D => new(OpCodes.Stloc, code.operand),
				0x11 or 0x12 => new(OpCodes.Stloc_S, code.operand),
				0x06 => new(OpCodes.Stloc_0),
				0x07 => new(OpCodes.Stloc_1),
				0x08 => new(OpCodes.Stloc_2),
				0x09 => new(OpCodes.Stloc_3),
				_ => throw new InvalidOperationException("Opcode is not a local loader")
			};
		}

		public static string FormatReadable(this TimeSpan span)
		{
			var sb = new StringBuilder();

			if (span.Hours > 0)
				sb.Append(span.Hours).Append("h ");

			if (span.Minutes > 0)
				sb.Append(span.Minutes).Append("m ");

			if (span.Seconds > 0)
				sb.Append(span.Seconds).Append("s ");

			sb.Append(span.Milliseconds).Append("ms");
			return sb.ToString();
		}

		public static IEnumerable<IEnumerable<T>> TimeChunk<T>(this IEnumerable<T> source, int millis)
		{
			return new TimeChunker<T>(source, millis);
		}

		private class TimeChunker<T>(IEnumerable<T> source, int millis) : IEnumerable<IEnumerable<T>>
		{
			public IEnumerator<IEnumerable<T>> GetEnumerator()
			{
				return new TimeChunk(source.GetEnumerator(), millis);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			private class TimeChunk(IEnumerator<T> source, int millis) : IEnumerator<IEnumerable<T>>
			{
				private readonly Stopwatch timer = new();
				private bool disposed = false;
				private readonly long ticks = millis * TimeSpan.TicksPerMillisecond;
				private bool EndOfSequence = false;

				public IEnumerable<T> Current { get; private set; }

				object IEnumerator.Current => Current;

				public void Dispose()
				{
					if (disposed)
						return;

					disposed = true;
					source.Dispose();
				}

				public bool MoveNext()
				{
					if (disposed || EndOfSequence)
						return false;

					Current = GetItems();
					return !EndOfSequence;
				}

				private IEnumerable<T> GetItems()
				{
					EndOfSequence = true;
					timer.Restart();
					while (source.MoveNext())
					{
						EndOfSequence = false;
						if (timer.ElapsedTicks > ticks)
							break;

						yield return source.Current;
					}
				}

				public void Reset()
				{
					source.Reset();
					EndOfSequence = false;
				}
			}
		}
	}
}
