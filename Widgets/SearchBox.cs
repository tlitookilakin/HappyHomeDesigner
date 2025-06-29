﻿using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HappyHomeDesigner.Widgets
{
    public class SearchBox : BlankTextBox
    {
        private static readonly ConditionalWeakTable<IReadOnlyList<IGridItem>, string[]> mapCache = new();
        private static readonly Rectangle FrameSource = new(0, 256, 60, 60);
        private static readonly Rectangle Spyglass = new(0, 48, 16, 16);

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

        public int FullWidth
        {
            get => fullWidth;
            set
            {
                fullWidth = value;
                if (iconOpacity == 0f)
                    Width = fullWidth;
            }
        }
        public int SmallWidth
        {
            get => smallWidth;
            set
            {
                smallWidth = value;
                if (iconOpacity == 1f)
                    Width = smallWidth;
            }
        }

        private string[] source_map;
        private IReadOnlyList<IGridItem> source;
        private IReadOnlyList<string> filtered_map;
        private IReadOnlyList<IGridItem> filtered;
        private string LastValue;
        private float iconOpacity = 1f;
        private int fullWidth = 0;
        private int smallWidth = 0;

        public SearchBox(Texture2D caretTexture, SpriteFont font, Color textColor)
            : base(caretTexture, font, textColor)
        {
            LastValue = Text;
        }

        public void Reset()
        {
            if (Text == string.Empty)
                return;

            Text = string.Empty;
            TextChanged();
        }

        public void Refresh()
        {
            Filter(true);
        }

        public bool ContainsPoint(int x, int y)
            => x >= X - 16 &&
                y >= Y - 16 &&
                x <= X + Width + 16 &&
                y <= Y + Height + 16;

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

        public override void Draw(SpriteBatch b, bool drawShadow = true)
        {
            if (Selected)
            {
                if (iconOpacity is not 0f)
                {
                    iconOpacity = MathF.Max(0f, iconOpacity - .07f);
                    if (ModEntry.config.ExpandSearch)
                        Width = (int)Utility.Lerp(SmallWidth, FullWidth, 1f - iconOpacity);
                }
            }
            else
            {
                if (iconOpacity is not 1f)
                {
                    iconOpacity = MathF.Min(1f, iconOpacity += .07f);
                    if (ModEntry.config.ExpandSearch)
                        Width = (int)Utility.Lerp(SmallWidth, FullWidth, 1f - iconOpacity);
                }
            }

            if (drawShadow)
                IClickableMenu.drawTextureBox(b, Game1.menuTexture, FrameSource, X - 4, Y - 4, Width + 8, Height + 16, Color.Black * .4f, 1f, false);

            //outline
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, FrameSource, X, Y - 8, Width + 8, Height + 16, Color.White, 1f, false);

            // box
            base.Draw(b);
            //base.Draw(b, false);

            // icon
            b.Draw(Catalog.MenuTexture, new Vector2(X + Width - 40, Y + 8), Spyglass, Color.White * iconOpacity, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
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

            for (int i = 0; i < count; i++)
                names[i] = items[i].GetName().Replace(" ", null);

            return names;
        }
    }
}
