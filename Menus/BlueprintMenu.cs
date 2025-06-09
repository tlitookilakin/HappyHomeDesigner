using HappyHomeDesigner.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Menus
{
	public class BlueprintMenu : IClickableMenu
	{
		internal static readonly Color BLUE_TINT = new(0xD6FAFFFF);

		private readonly GameLocation location;
		private readonly IList<RoomLayoutData> layouts;
		private readonly IList<HoverComponent> hoverComponents;
		private readonly HoverComponent AddButton;

		public BlueprintMenu(GameLocation location) : base()
		{
			this.location = location;
			layouts = RoomLayoutManager.GetLayoutsFor(location);

			var texture = AssetManager.BlueprintUI;

			AddButton = new("add", null, default, new(32, 0, 15, 15), new(48, 0, 15, 15), 3f);
			hoverComponents = [
				new("apply", ModEntry.i18n.Get("ui.blueprint.apply"), new(48, 48), new(0, 16, 16, 16), new(0, 32, 16, 16)),
				new("delete", ModEntry.i18n.Get("ui.blueprint.delete"), new(48, 48), new(32, 16, 16, 16), new(32, 32, 16, 16)),
				new("save", ModEntry.i18n.Get("ui.blueprint.save"), new(48, 48), new(16, 16, 16, 16), new(16, 32, 16, 16)),
				new("clear", ModEntry.i18n.Get("ui.blueprint.clear"), new(48, 48), new(48, 16, 16, 16), new(48, 32, 16, 16)),
				AddButton
			];
			allClickableComponents = [.. hoverComponents];

			Resize(Game1.uiViewport.ToRect());
		}

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			base.gameWindowSizeChanged(oldBounds, newBounds);
			Resize(newBounds);
		}

		private void Resize(Rectangle bounds)
		{
			width = Math.Min(bounds.Width, 500);
			height = 800; // calculate later

			xPositionOnScreen = (bounds.Width - width) / 2;
			yPositionOnScreen = (bounds.Height - height) / 2;

			int x = xPositionOnScreen + width;
			int y = yPositionOnScreen + height - (12 + 48);
			foreach (var hover in hoverComponents)
			{
				x -= 12 + 48;
				hover.SetPosition(x, y);
			}
			x += 48;

			AddButton.bounds = new(xPositionOnScreen + 12, y, x - xPositionOnScreen - 12, 48);
		}

		public override void performHoverAction(int x, int y)
		{
			base.performHoverAction(x, y);
			foreach (var hover in  hoverComponents)
				hover.Hover(x, y);
		}

		public override void draw(SpriteBatch b)
		{
			var bp = AssetManager.BlueprintUI;
			DrawPanel(b, bp);

			base.draw(b);

			foreach (var hover in hoverComponents)
				hover.Draw(b, bp);

			drawMouse(b);

		}

		private void DrawPanel(SpriteBatch b, Texture2D bp)
		{
			var src = new Rectangle(0, 0, 16, 16);
			int right = width + xPositionOnScreen;
			int bottom = height + yPositionOnScreen;

			for (int x = xPositionOnScreen; x < right; x += 48)
				for (int y = yPositionOnScreen; y < bottom; y += 48)
					b.Draw(bp, new Rectangle(x, y, Math.Min(right - x, 48), Math.Min(bottom - y, 48)), src, Color.White);

			drawTextureBox(b, bp, new(16, 0, 15, 15), xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f, false);
		}
	}
}
