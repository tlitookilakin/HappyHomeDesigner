using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;

namespace HappyHomeDesigner.Widgets
{
	public class BlankTextBox : TextBox
	{
		public BlankTextBox(Texture2D caretTexture, SpriteFont font, Color textColor) : base(null, caretTexture, font, textColor)
		{

		}

		public override void Draw(SpriteBatch spriteBatch, bool drawShadow = true)
		{
			float caretAlpha = MathF.Sin((float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds * MathF.Tau / 1000f);
			bool isEmpty = string.IsNullOrEmpty(Text);
			var toDraw = isEmpty ? TitleText ?? "" : Text;
			var alpha = isEmpty ? .5f : 1f;
			var size = _font.MeasureString(toDraw);

			while (size.X > Width)
			{
				toDraw = toDraw[1..];
				size = _font.MeasureString(toDraw);
			}

			if (isEmpty)
				size = default;

			if (Selected)
				spriteBatch.Draw(Game1.staminaRect, new Rectangle(X + 16 + (int)size.X + 2, Y + 8, 4, 32), _textColor * caretAlpha);

			if (drawShadow && !isEmpty)
				Utility.drawTextWithShadow(spriteBatch, toDraw, _font, new Vector2(X + 16, Y + (_textBoxTexture != null ? 12 : 8)), _textColor);
			else
				spriteBatch.DrawString(_font, toDraw, new Vector2(X + 16, Y + (_textBoxTexture != null ? 12 : 8)), _textColor * alpha, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.99f);
		}
	}
}
