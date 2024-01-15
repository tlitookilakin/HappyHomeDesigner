﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HappyHomeDesigner.Menus
{
	public class SearchBox : TextBox
	{
		private static ConditionalWeakTable<IReadOnlyList<IGridItem>, string[]> mapCache = new();

		public IReadOnlyList<IGridItem> Filtered => filtered ?? source;
		public event Action OnTextChanged;

		public IReadOnlyList<IGridItem> Source
		{
			get => source;
			set
			{
				source = value;
				Filter(true);
			}
		}

		private string[] source_map;
		private IReadOnlyList<IGridItem> source;
		private IReadOnlyList<string> filtered_map;
		private IReadOnlyList<IGridItem> filtered;
		private string LastValue;

		public SearchBox(Texture2D textBoxTexture, Texture2D caretTexture, SpriteFont font, Color textColor) 
			: base(textBoxTexture, caretTexture, font, textColor)
		{
			LastValue = Text;
		}

		public void Reset()
		{
			Text = string.Empty;
			Filter(true);
		}

		public void Refresh()
		{
			Filter(true);
		}

		public bool ContainsPoint(int x, int y)
			=> x >= X && y >= Y && x <= X + Width && y <= Y + Height;

		public override void RecieveTextInput(char input)
		{
			base.RecieveTextInput(input);
			TextChanged();
		}
		public override void RecieveTextInput(string text)
		{
			base.RecieveTextInput(text);
			TextChanged();
		}
		public override void RecieveCommandInput(char command)
		{
			base.RecieveCommandInput(command);
			TextChanged();
		}

		private void TextChanged()
		{
			var search = Text.Replace(" ", null);
			if (search != LastValue)
			{
				Filter(false, search);
				LastValue = search;
			}
			OnTextChanged?.Invoke();
		}

		private void Filter(bool refresh, string search = null)
		{
			if (source.Count is 0)
				return;

			search ??= Text.Replace(" ", null);

			if (search.Length is 0)
			{
				filtered_map = null;
				filtered = null;
				return;
			}

			IReadOnlyList<IGridItem> source_items = filtered;
			IReadOnlyList<string> source_names = filtered_map;

			if (refresh || filtered_map is null || !search.StartsWith(LastValue, StringComparison.OrdinalIgnoreCase))
			{
				if (!mapCache.TryGetValue(source, out source_map))
					mapCache.Add(source, source_map = GetNames(source));

				source_items = source;
				source_names = source_map;
			}

			var result = new List<IGridItem>();
			var result_map = new List<string>();
			int count = source_names.Count;

			for (int i = 0; i < count; i++)
			{
				if (source_names[i].Contains(search, StringComparison.OrdinalIgnoreCase))
				{
					result.Add(source_items[i]);
					result_map.Add(source_names[i]);
				}
			}

			filtered_map = result_map; 
			filtered = result;
		}

		private static string[] GetNames(IReadOnlyList<IGridItem> items)
		{
			int count = items.Count;
			var names = new string[count];

			for(int i = 0; i < count; i++)
				names[i] = items[i].GetName().Replace(" ", null);

			return names;
		}
	}
}
