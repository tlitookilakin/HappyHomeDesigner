using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace HappyHomeDesigner.Menus
{
	public class HoverComponent : ClickableComponent
	{
		private readonly Rectangle sNormal;
		private readonly Rectangle sHover;
		private bool hovered = false;
		private readonly bool patch;
		private readonly float pScale;

		public HoverComponent(string name, string tooltip, Point size, Rectangle sourceNormal, Rectangle sourceHover, float patchScale = 0f) 
			: base(new(0, 0, size.X, size.Y), name, tooltip)
		{
			sNormal = sourceNormal;
			sHover = sourceHover;
			patch = patchScale > 0f;
			pScale = patchScale;
		}

		public void Hover(int x, int y)
		{
			hovered = containsPoint(x, y);
		}

		public void Draw(SpriteBatch batch, Texture2D texture)
		{
			var src = hovered ? sHover : sNormal;
			if (patch)
				IClickableMenu.drawTextureBox(batch, texture, src, bounds.X, bounds.Y, bounds.Width, bounds.Height, Color.White, pScale, false);
			else
				batch.Draw(texture, bounds, src, Color.White);
		}

		public void SetPosition(int x, int y)
		{
			bounds.Location = new(x, y);
		}
	}
}
