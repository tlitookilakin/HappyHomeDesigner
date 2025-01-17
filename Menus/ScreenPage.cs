using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Integration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;

namespace HappyHomeDesigner.Menus
{
	public abstract class ScreenPage : IClickableMenu
	{
		internal const int CELL_SIZE = 80;
		internal const int FILTER_WIDTH = 72;
		internal const int FILTER_HEIGHT = 42;
		internal const int FILTER_SCALE = 3;

		protected int filter_count;
		protected int current_filter;
		public List<SpaceTab> custom_tabs;

		/// <returns>The tab representing this page</returns>
		public abstract ClickableTextureComponent GetTab();

		/// <summary>Called when the page is destroyed</summary>
		public virtual void Exit() { }

		/// <returns>The number of items this page contains</returns>
		public abstract int Count();

		protected ClickableTextureComponent InventoryButton = new(new(0, 0, 64, 64), Catalog.MenuTexture, new(16, 48, 16, 16), 4f, true);

		public virtual void Resize(Rectangle region)
		{
			width = region.Width;
			height = region.Height;
			xPositionOnScreen = region.X;
			yPositionOnScreen = region.Y;
		}

		/// <summary>Draws on top of the whole menu.</summary>
		public virtual void DrawTooltip(SpriteBatch batch)
		{

		}

		/// <summary>Draw the magnifier preview for an item</summary>
		protected static void DrawMagnified(SpriteBatch b, Item hovered)
		{
			float scale = ModEntry.config.MagnifyScale;
			int boxSize = (int)(64f * scale);
			int itemOffset = (int)(32f * (scale - 1f));
			const int BORDER = 16;
			const int CURSOR = 48;

			var mouse = Game1.getMousePosition(true);

			if (mouse.X < boxSize)
			{
				mouse.X += CURSOR;
				mouse.Y -= boxSize + BORDER - 24;
			} 
			else
			{
				mouse.X -= boxSize + BORDER;
				mouse.Y += CURSOR;
			}

			drawTextureBox(b, mouse.X - BORDER, mouse.Y - BORDER, boxSize + (BORDER * 2), boxSize + (BORDER * 2), Color.White);
			hovered.drawInMenu(b, new(mouse.X + itemOffset, mouse.Y + itemOffset), scale);
		}

		/// <summary>Activate the clicked filter if possible</summary>
		/// <param name="x">Mouse X</param>
		/// <param name="y">Mouse Y</param>
		/// <param name="playSound">Whether or not to play sound</param>
		/// <returns>True if a filter was selected, otherwise False</returns>
		public bool TrySelectFilter(int x, int y, bool playSound)
		{
			int relX = x - xPositionOnScreen;
			int relY = y - yPositionOnScreen;

			if (relX is > FILTER_WIDTH)
				return false;

			int which = relY / (FILTER_HEIGHT - FILTER_SCALE);

			if (which >= filter_count || which == current_filter)
				return false;

			if (playSound)
				Game1.playSound("shwip");

			current_filter = which;
			return true;
		}

		/// <summary>Draws the filter tabs for this page. Icons are treated as a row of 24x24 sprites in the UI texture.</summary>
		/// <param name="textureRow">The Y pixel coordinate in the UI texture to use for the tab icons</param>
		/// <param name="ribbonCount">How many of the filters should use the ribbon (favorites) background</param>
		public void DrawFilters(SpriteBatch batch, int textureRow, int ribbonCount, int x, int y)
		{
			bool use_custom = custom_tabs is not null;

			int sx = 0;
			int fcount = use_custom ? custom_tabs.Count + ribbonCount : filter_count;
			int i = 0;
			var shadow = Color.Black * .4f;
			while(i < fcount - ribbonCount)
			{
				int nx = i == current_filter ? x - 16 : x;

				// shadow
				batch.Draw(Catalog.MenuTexture,
					new Rectangle(nx - 4, y + 4, FILTER_WIDTH, FILTER_HEIGHT),
					new Rectangle(0, 24, FILTER_WIDTH / FILTER_SCALE, FILTER_HEIGHT / FILTER_SCALE),
					shadow);

				// standard bg
				batch.Draw(Catalog.MenuTexture,
					new Rectangle(nx, y, FILTER_WIDTH, FILTER_HEIGHT),
					new Rectangle(0, 24, FILTER_WIDTH / FILTER_SCALE, FILTER_HEIGHT / FILTER_SCALE),
					Color.White);

				// icon
				if (use_custom)
				{
					var t = custom_tabs[i];
					batch.Draw(t.texture,
						new Rectangle(nx + (i == current_filter ? 6 * FILTER_SCALE : 3 * FILTER_SCALE), y + 3 * FILTER_SCALE, 30, 24),
						t.IconRect, Color.White
					);
				}
				else
				{
					batch.Draw(
						Catalog.MenuTexture,
						new Rectangle(nx + (i == current_filter ? 6 * FILTER_SCALE : 3 * FILTER_SCALE), y + 3 * FILTER_SCALE, 30, 24),
						new Rectangle(sx, textureRow, 10, 8),
						Color.White);
				}

				y += FILTER_HEIGHT - FILTER_SCALE;
				sx += 10;
				i++;
			}
			sx = (filter_count - ribbonCount) * 10;
			while(i < filter_count)
			{
				int nx = i == current_filter ? x - 16 : x;

				// shadow
				batch.Draw(Catalog.MenuTexture,
					new Rectangle(nx - 4, y + 4, FILTER_WIDTH, FILTER_HEIGHT),
					new Rectangle(24, 24, FILTER_WIDTH / FILTER_SCALE, FILTER_HEIGHT / FILTER_SCALE),
					shadow);

				// ribbon bg
				batch.Draw(Catalog.MenuTexture,
					new Rectangle(nx, y, FILTER_WIDTH, FILTER_HEIGHT),
					new Rectangle(24, 24, FILTER_WIDTH / FILTER_SCALE, FILTER_HEIGHT / FILTER_SCALE),
					Color.White);

				// icon
				batch.Draw(
					Catalog.MenuTexture,
					new Rectangle(nx + (i == current_filter ? 16 : 8), y + 8, 30, 24),
					new Rectangle(sx, textureRow, 10, 8),
					Color.White);

				y += FILTER_HEIGHT - FILTER_SCALE;
				sx += 10;
				i++;
			}
		}

		public override void draw(SpriteBatch b)
		{
			InventoryButton.draw(b);
		}

		/// <inheritdoc/>
		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);
			if (InventoryButton.containsPoint(x, y))
			{
				var menu = Catalog.ActiveMenu.Value;
				menu.InventoryOpen = !menu.InventoryOpen;
				if (playSound)
					Game1.playSound(menu.InventoryOpen ? "bigSelect" : "bigDeSelect");
			}
		}

		/// <summary>Do something when a keyboard, mouse, or controller button is pressed or released</summary>
		/// <returns>True if it was handled and should be suppressed, otherwise false.</returns>
		public abstract bool TryApplyButton(SButton button, bool IsPressed);

		protected void DeleteActiveItem(bool playSound, ICollection<string> whitelist)
		{
			if (!Game1.player.ActiveObject.CanDelete(whitelist))
				return;

			if (Game1.player.ActiveObject == Game1.player.TemporaryItem)
				Game1.player.TemporaryItem = null;
			else
				Game1.player.removeItemFromInventory(Game1.player.ActiveObject);

			if (playSound)
				Game1.playSound("trashcan");
		}

		/// <summary>Delete the held item if possible</summary>
		public abstract void DeleteActiveItem(bool playSound);
	}
}
