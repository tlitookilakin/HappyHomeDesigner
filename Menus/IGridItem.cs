using Microsoft.Xna.Framework.Graphics;

namespace HappyHomeDesigner.Menus
{
	public interface IGridItem
	{
		public void Draw(SpriteBatch batch, int x, int y);

		public bool ToggleFavorite(bool playSound);

		public string GetName();
	}
}
