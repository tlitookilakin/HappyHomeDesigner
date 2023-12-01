using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHomeDesigner.Menus
{
	internal class WallFloorPage : ScreenPage
	{
		public override void draw(SpriteBatch b)
		{
			base.draw(b);
			b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height), Color.Blue);
		}
	}
}
