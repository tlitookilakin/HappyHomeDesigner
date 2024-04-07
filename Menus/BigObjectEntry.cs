using HappyHomeDesigner.Integration;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace HappyHomeDesigner.Menus
{
	internal class BigObjectEntry : VariantEntry<SObject>
	{
		/// <inheritdoc/>
		public BigObjectEntry(SObject Item) : base(Item){}

		/// <inheritdoc/>
		public BigObjectEntry(SObject Item, Season season, string seasonName, ICollection<string> favorites) : 
			base(Item, season, seasonName, favorites, "Craftable_"){}

		public override bool CanPlace()
			=> true;

		public override SObject GetOne()
			=> Item.getOne() as SObject;

		public override IList<VariantEntry<SObject>> GetVariants()
		{
			if (!HasVariants)
				return new[] { new BigObjectEntry(Item) };

			List<SObject> skins = new() { Item };
			AlternativeTextures.VariantsOfCraftable(Item, season, skins);

			return skins.Select(f => new BigObjectEntry(f) as VariantEntry<SObject>).ToList();
		}
	}
}
