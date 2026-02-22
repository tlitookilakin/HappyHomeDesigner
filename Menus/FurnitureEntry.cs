using HappyHomeDesigner.Integration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;

namespace HappyHomeDesigner.Menus
{
	internal class FurnitureEntry : VariantEntry<Furniture>
	{
		bool inited = false;

		/// <inheritdoc/>
		public FurnitureEntry(Furniture Item, Season season, string seasonName, ICollection<string> favorites)
			: base(Item, season, seasonName, favorites, "Furniture_")
		{
		}

		/// <inheritdoc/>
		public FurnitureEntry(Furniture Item)
			: base(Item)
		{
			Item.currentRotation.Value = 0;
			Item.updateRotation();
		}

		/// <inheritdoc/>
		public override Furniture GetOne()
		{
			Init();
			var item = Item.getOne() as Furniture;
			item.Price = 0;
			item.currentRotation.Value = 0;
			item.updateRotation();
			return item;
		}

		/// <inheritdoc/>
		public override IReadOnlyList<VariantEntry<Furniture>> GetVariants()
		{
			if (!HasVariants)
				return [new FurnitureEntry(Item)];

			List<Furniture> skins = [Item];
			AlternativeTextures.VariantsOfFurniture(Item, season, skins);

			return skins.Select(f => new FurnitureEntry(f) as VariantEntry<Furniture>).ToList();
		}

		/// <inheritdoc/>
        public override void Draw(SpriteBatch b, int x, int y)
        {
			Init();
            base.Draw(b, x, y);
        }

		private void Init()
		{
			if (!inited)
			{
				inited = true;
				try
				{
					// fix default bounding box & texture
					Item.InitializeAtTile(Vector2.Zero);
				}
				catch (ContentLoadException ex)
				{
					ModEntry.monitor.Log($"Failed to load texture for furniture '{Item.ItemId}': {ex}", LogLevel.Warn);
				}
			}
		}
	}
}
