using HappyHomeDesigner.Integration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Menus
{
    internal class FurnitureEntry
    {
        internal const int CELL_SIZE = 80;

        public Furniture Item;
        public bool HasVariants;
        private readonly string season = string.Empty;

        // 384 396 15 15 cursors
        // 256 256 10 10 cursors
        // 128 128 64 64 menus

        private static readonly Rectangle background = new(128, 128, 64, 64);
        private static readonly Rectangle star = new(346, 400, 8, 8);

        /// <summary>Standard constructor. Used for main catalog page.</summary>
        /// <param name="Item">The contained furniture item.</param>
        /// <param name="season">The local season. Required to accurately check for AT variants.</param>
        public FurnitureEntry(Furniture Item, string season)
        {
            this.Item = Item;
            this.season = season;
            HasVariants = AlternativeTextures.Installed && AlternativeTextures.HasVariant("Furniture_" + Item.Name, season);
        }

        /// <summary>Used for AT variant entries.</summary>
        /// <param name="Item">The contained furniture item, with AT tags applied.</param>
        public FurnitureEntry(Furniture Item)
		{
			this.Item = Item;
            HasVariants = false;
        }

        public IList<Furniture> GetVariants()
        {
            if (!HasVariants)
                return new[] {Item};

            List<Furniture> skins = new() { Item };
            AlternativeTextures.VariantsOf(Item, season, skins);
            return skins;
        }

        public void Draw(SpriteBatch b, int x, int y)
        {
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, background, x, y, CELL_SIZE, CELL_SIZE, Color.White, 1f, false);
            Item?.drawInMenu(b, new(x + 8, y + 8), 1f);
            if (HasVariants)
                b.Draw(Game1.mouseCursors, new Rectangle(x + CELL_SIZE - 32, y + 8, 24, 24), star, Color.White);
        }

        public Furniture GetOne()
        {
            var item = Item.getOne() as Furniture;
            item.Price = 0;
            item.updateRotation();
            return item;
        }
    }
}
