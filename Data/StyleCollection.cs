using Microsoft.Xna.Framework;
using StardewValley;

namespace HappyHomeDesigner.Data
{
	public class StyleCollection : IStyleSet
	{
		public string DisplayName { get; set; }
		public string Description { get; set; }
		public string UnlockItem { get; set; }
		public string RequiredTag { get; set; }
		public string RequiredPrefix { get; set; }
		public string RequiredCondition { get; set; }
		public string IconTexture { get; set; }
		public Rectangle IconSource { get; set; }

		public bool Contains(Item item)
		{
			if (RequiredPrefix != null && !item.ItemId.StartsWith(RequiredPrefix))
				return false;
			else if (RequiredTag != null && !item.HasContextTag(RequiredTag))
				return false;

			if (RequiredCondition is null)
				return true;

			return GameStateQuery.CheckConditions(RequiredCondition, Game1.currentLocation, Game1.player, null, item, Game1.random);
		}
	}
}
