using HappyHomeDesigner.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;

namespace HappyHomeDesigner.Widgets
{
	internal class BlueprintEntry : ClickableComponent
	{
		public const int HEIGHT = 96;

		public readonly int Index;

		private readonly IList<RoomLayoutData> layouts;

		public BlueprintEntry(IList<RoomLayoutData> layouts, int which, int width) : base(new(0, 0, width, which * HEIGHT), "layout_item", null)
		{
			Index = which;
			this.layouts = layouts;
		}

		public void Draw(SpriteBatch b, int selectedIndex)
		{
			if (Index >= layouts.Count)
				return;

			var current = layouts[Index];

			var col = selectedIndex == Index ? Color.Wheat : Color.White;

			IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new(384, 396, 15, 15),  bounds.X, bounds.Y, bounds.Width, bounds.Height, col, 4f, false);

			Utility.drawTextWithShadow(b, current.Name, Game1.dialogueFont, new(bounds.X + 20, bounds.Y + 12), Game1.textColor);

			b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 16, bounds.Y + 12 + 48, bounds.Width - 32, 3), Game1.textColor);

			Utility.drawBoldText(b, current.FarmerName, Game1.smallFont, new Vector2(bounds.X + 18, bounds.Bottom - 34), Game1.textColor, .6f);
			var swidth = Game1.smallFont.MeasureString(current.Date);
			Utility.drawBoldText(b, current.Date, Game1.smallFont, new(bounds.Right - 18 - swidth.X * .6f, bounds.Bottom - 34), Game1.textColor, .6f);
		}
	}
}
