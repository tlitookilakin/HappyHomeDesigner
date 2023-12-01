using Microsoft.Xna.Framework;
using StardewValley.Menus;
using System;

namespace HappyHomeDesigner.Menus
{
	public class ScreenPage : IClickableMenu
	{
		public IClickableMenu Parent;

		public virtual void Resize(Rectangle region)
		{
			width = region.Width;
			height = region.Height;
			xPositionOnScreen = region.X;
			yPositionOnScreen = region.Y;
		}
	}
}
