using Microsoft.Xna.Framework.Graphics;

namespace HappyHomeDesigner.Menus
{
	public interface IGridItem
	{
		public void DrawBackground(SpriteBatch batch, int x, int y);

		public void Draw(SpriteBatch batch, int x, int y);

		public bool ToggleFavorite(bool playSound);

		/// <returns>A string used mainly to search for this entry</returns>
		public string GetName();
	}
}
