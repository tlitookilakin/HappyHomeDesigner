using Microsoft.Xna.Framework.Graphics;
using System;

namespace HappyHomeDesigner.Menus
{
	public interface IGridItem
	{
		public void Draw(SpriteBatch batch, int x, int y);

		public bool ToggleFavorite(bool playSound);
	}
}
