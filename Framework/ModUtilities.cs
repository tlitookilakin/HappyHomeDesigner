using HappyHomeDesigner.Integration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace HappyHomeDesigner.Framework
{
	public static class ModUtilities
	{
		private static readonly FieldInfo OldValueBackingField =
			typeof(MouseWheelScrolledEventArgs).GetField("<OldValue>k__BackingField", 
				BindingFlags.Instance | BindingFlags.NonPublic);

		public static bool TryFindAssembly(string name, [NotNullWhen(true)] out Assembly assembly)
		{
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (asm.GetName().Name == name)
				{
					assembly = asm;
					return true;
				}
			}
			assembly = null;
			return false;
		}

		public static void Suppress(this MouseWheelScrolledEventArgs e)
		{
			// suppress game
			Game1.oldMouseState = Game1.input.GetMouseState();

			// suppress event
			OldValueBackingField.SetValue(e, e.NewValue);
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

		public static void DrawFrame(this SpriteBatch b, Texture2D texture, Rectangle dest, Rectangle source, int padding, int scale, Color color, int top = 0)
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

		public static T ToDelegate<T>(this MethodInfo method) where T : Delegate
			=> (T)Delegate.CreateDelegate(typeof(T), method);

		public static T ToDelegate<T>(this MethodInfo method, object target) where T : Delegate
			=> (T)Delegate.CreateDelegate(typeof(T), target, method);

		public static void AddQuickBool(this IGMCM gmcm, IManifest manifest, object config, string name)
		{
			var prop = config.GetType().GetProperty(name);
			if (prop is null || prop.PropertyType != typeof(bool))
				throw new ArgumentException($"Could not find property '{name}' of type 'bool' on config.");

			var title = $"config.{prop.Name}.name";
			var desc = $"config.{prop.Name}.desc";

			gmcm.AddBoolOption(
				manifest,
				prop.GetMethod.ToDelegate<Func<bool>>(config),
				prop.SetMethod.ToDelegate<Action<bool>>(config),
				() => ModEntry.i18n.Get(title),
				() => ModEntry.i18n.Get(desc)
				);
		}

		public static void AddQuickKeybindList(this IGMCM gmcm, IManifest manifest, object config, string name)
		{
			var prop = config.GetType().GetProperty(name);
			if (prop is null || prop.PropertyType != typeof(KeybindList))
				throw new ArgumentException($"Could not find property '{name}' of type 'KeybindList' on config.");

			var title = $"config.{prop.Name}.name";
			var desc = $"config.{prop.Name}.desc";

			gmcm.AddKeybindList(
				manifest,
				prop.GetMethod.ToDelegate<Func<KeybindList>>(config),
				prop.SetMethod.ToDelegate<Action<KeybindList>>(config),
				() => ModEntry.i18n.Get(title),
				() => ModEntry.i18n.Get(desc)
				);
		}
	}
}
