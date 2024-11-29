using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace HappyHomeDesigner.Menus
{
	public class InventoryWrapper : InventoryMenu
	{
		public const int BORDER_WIDTH = 16;
		public const int MARGIN_BOTTOM = 8;
		private static readonly Rectangle BackgroundSource = new(384, 373, 18, 18);

		private bool wasInside = false;
		private Item? hovered = null;

		public InventoryWrapper() : base(0, 0, true)
		{

		}

		public bool Visible { get; set; }

		public override void draw(SpriteBatch b)
		{
			(int mx, int my) = Game1.getMousePosition();

			bool isInside = isWithinBounds(mx, my) && Visible;
			if (isInside != wasInside)
			{
				wasInside = isInside;

				if (isInside)
				{
					if (Game1.player.TemporaryItem is Item held && Game1.player.CursorSlotItem is null && held.isPlaceable())
					{
						held?.onDetachedFromParent();
						Game1.player.ActiveItem = null;
						Game1.player.CursorSlotItem = held;
					}
				}
				else
				{
					// clear tooltip when outside
					hovered = null;
					hoverText = null;

					if (Game1.player.CursorSlotItem is Item held && held.isPlaceable())
					{
						held?.onDetachedFromParent();
						Game1.player.CursorSlotItem = null;
						Game1.player.TemporaryItem = held;
					}
				}
			}

			if (!Visible)
				return;

			drawTextureBox(b, Game1.mouseCursors, BackgroundSource, xPositionOnScreen - BORDER_WIDTH,
				yPositionOnScreen - BORDER_WIDTH * 3 - 4, width + BORDER_WIDTH * 2,
				height + BORDER_WIDTH * 3 + 8, Color.White, 4f, false);

			base.draw(b);

			if (Game1.player.CursorSlotItem is Item grabbed)
				grabbed.drawInMenu(b, new(mx + 16, my + 16), 1f);
			else if (Game1.player.ActiveItem is Item active && isInside)
				active.drawInMenu(b, new(mx + 16, my + 16), 1f);

			if (hoverText != null)
			{
				drawToolTip(b, hoverText, hoverTitle, hovered, Game1.player.CursorSlotItem != null);
			}
		}

		public override void performHoverAction(int x, int y)
		{
			hovered = hover(x, y, Game1.player.CursorSlotItem);
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			setHeldItem(leftClick(x, y, takeHeldItem(), true));
		}

		public override void receiveRightClick(int x, int y, bool playSound = true)
		{
			setHeldItem(rightClick(x, y, takeHeldItem()));
		}

		public override bool isWithinBounds(int x, int y)
		{
			return
				x >= xPositionOnScreen - BORDER_WIDTH &&
				x <= xPositionOnScreen + width + BORDER_WIDTH &&
				y >= yPositionOnScreen - BORDER_WIDTH * 3 - 4 &&
				y <= yPositionOnScreen + height + BORDER_WIDTH * 2 + 8;
		}

		private void setHeldItem(Item held)
		{
			Game1.player.CursorSlotItem = held;
		}

		private static Item? takeHeldItem()
		{
			if (Game1.player.CursorSlotItem is Item cursorItem)
			{
				cursorItem?.onDetachedFromParent();
				Game1.player.CursorSlotItem = null;
				return cursorItem;
			}
			if (Game1.player.ActiveItem is Item activeItem)
			{
				activeItem.onDetachedFromParent();
				Game1.player.ActiveItem = null;
				return activeItem;
			}
			return null;
		}
	}
}
