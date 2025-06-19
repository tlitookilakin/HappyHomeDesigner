using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
		private readonly Texture2D uiTexture = Game1.content.Load<Texture2D>(AssetManager.UI_PATH);

		private readonly ClickableTextureComponent AddButton;
		private readonly List<ClickableTextureComponent> BottomButtons;
		private readonly List<ClickableTextureComponent> TopButtons;
		private readonly ScrollBar Scroll = new();
		private readonly TextBox NameBox;

		private string hoverText;
		private string overText;
		private int overTicks;

		private const int MAIN_WIDTH = 350;
		private const int PADDING = 8;
		private const int LEFT = 32;
		private const int MARGIN = 32;

		public BlueprintMenu(GameLocation location) : base()
		{
			this.location = location;
			layouts = RoomLayoutManager.GetLayoutsFor(location);

			NameBox = new BlankTextBox(null, Game1.smallFont, Game1.textColor) { TitleText = ModEntry.i18n.Get("ui.blueprint.name") };
			AddButton = new("add", new(150, 0, 56, 64), null, ModEntry.i18n.Get("ui.blueprint.add"), uiTexture, new(17, 80, 14, 17), 4f, false);
			BottomButtons = [
				new("clear", new(150, 0, 64, 64), null, ModEntry.i18n.Get("ui.blueprint.clear"), uiTexture, new(80, 80, 16, 16), 4f, true),
				new("copy", new(150, 0, 64, 64), null, ModEntry.i18n.Get("ui.blueprint.copy"), uiTexture, new(96, 80, 16, 16), 4f, true),
				new("paste", new(150, 0, 64, 64), null, ModEntry.i18n.Get("ui.blueprint.paste"), uiTexture, new(112, 80, 16, 16), 4f, true)
			];
			TopButtons = [
				new("apply", new(150, 0, 64, 64), null, ModEntry.i18n.Get("ui.blueprint.apply"), uiTexture, new(32, 80, 16, 16), 4f, true),
				new("save", new(150, 0, 64, 64), null, ModEntry.i18n.Get("ui.blueprint.save"), uiTexture, new(64, 80, 16, 16), 4f, true),
				new("delete", new(150, 0, 64, 64), null, ModEntry.i18n.Get("ui.blueprint.delete"), uiTexture, new(48, 80, 16, 16), 4f, true)
			];
			allClickableComponents = [AddButton, .. BottomButtons, ..TopButtons];

			Resize(Game1.uiViewport.ToRect());
		}

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			base.gameWindowSizeChanged(oldBounds, newBounds);
			Resize(newBounds);
		}

		private void Resize(Rectangle bounds)
		{
			width = MAIN_WIDTH + LEFT + PADDING + MARGIN * 2;
			height = bounds.Height - MARGIN * 2;

			xPositionOnScreen = MARGIN;
			yPositionOnScreen = MARGIN;

			int x = xPositionOnScreen + MAIN_WIDTH + PADDING + LEFT;
			int y = yPositionOnScreen;
			AddButton.setPosition(x + 8, y);
			y += 68 + PADDING;

			foreach (var c in TopButtons)
			{
				c.setPosition(x, y);
				y += 64 + PADDING;
			}

			y = yPositionOnScreen + height;
			foreach (var c in BottomButtons)
			{
				y -= 64;
				c.setPosition(x, y);
				y -= PADDING;
			}

			NameBox.X = xPositionOnScreen + LEFT / 2 + PADDING;
			NameBox.Y = yPositionOnScreen + PADDING;
			NameBox.Width = AddButton.bounds.Left - NameBox.X;
			NameBox.Height = 24;

			Scroll.Resize(height - (24 + PADDING), xPositionOnScreen, yPositionOnScreen + (24 + PADDING));
		}

		public override void performHoverAction(int x, int y)
		{
			base.performHoverAction(x, y);

			hoverText = null;

			foreach (var c in TopButtons)
				c.tryHover(x, y, ref hoverText);

			foreach (var c in BottomButtons)
				c.tryHover(x, y, ref hoverText);

			AddButton.tryHover(x, y, ref hoverText);

			NameBox.Hover(x, y);
			NameBox.Update();

			Scroll.Hover(x, y);
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);

			foreach (var c in allClickableComponents)
				if (c.containsPoint(x, y))
					HandleButton(c.name, playSound);

			Scroll.Click(x, y);
		}

		private void HandleButton(string which, bool sound)
		{
			switch (which)
			{
				case "add": Add(sound); break;
				case "clear": Clear(sound); break;
				case "copy": Copy(sound); break;
				case "paste": Paste(sound); break;
				case "apply": Apply(sound); break;
				case "save": Update(sound); break;
				case "delete": Delete(sound); break;
			}
		}

		public override void draw(SpriteBatch b)
		{
			drawTextureBox(b, xPositionOnScreen + LEFT, yPositionOnScreen + 4, MAIN_WIDTH, height - 4, Color.White);

			Scroll.Draw(b);

			foreach (var c in TopButtons)
				c.draw(b);

			foreach (var c in BottomButtons)
				c.draw(b);

			DrawOverlay(b);

			b.Draw(uiTexture, new Rectangle(xPositionOnScreen + LEFT / 2, yPositionOnScreen, 64, 68), new Rectangle(0, 80, 16, 17), Color.White);
			b.Draw(
				uiTexture,
				new Rectangle(xPositionOnScreen + LEFT / 2 + 64, yPositionOnScreen, AddButton.bounds.Left - (xPositionOnScreen + LEFT / 2 + 64), 68),
				new Rectangle(16, 80, 1, 17), Color.White
			);

			AddButton.draw(b);
			NameBox.Draw(b);
			AfterDraw(b);
		}

		public void ShowOverlayText(string text)
		{
			overTicks = 0;
			overText = text;
		}

		private void AfterDraw(SpriteBatch b)
		{
			drawMouse(b);

			if (hoverText != null)
				drawHoverText(b, hoverText, Game1.smallFont);
		}

		private void DrawOverlay(SpriteBatch b)
		{
			if (overText is null)
				return;

			overTicks++;

			if (overTicks >= 90)
			{
				overText = null;
				return;
			}

			float alpha = (45 - Math.Max(Math.Abs(overTicks - 45), 30)) / 15f;

			drawTextureBox(
				b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), xPositionOnScreen + LEFT, 
				yPositionOnScreen, MAIN_WIDTH, height, Color.Black * .5f * alpha, drawShadow: false
			);

			Vector2[] offsets = new Vector2[overText.Length];
			float amp = 16f * MathF.Max(80 - overTicks, 0) / 80f;
			for (int i = 0; i < offsets.Length; i++)
				offsets[i] = new(0f, MathF.Sin(i * .7f + overTicks * .15f) * amp);

			b.DrawStringOffset(
				overText, xPositionOnScreen + MAIN_WIDTH / 2 + LEFT, yPositionOnScreen + height / 2, 
				MAIN_WIDTH - 64, Utility.GetPrismaticColor(speedMultiplier: 4f), offsets, true
			);
		}

		public override bool readyToClose()
		{
			if (NameBox.Selected)
				return false;

			return base.readyToClose();
		}

		public void Add(bool playSound)
		{
			ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.added"));
		}

		public void Apply(bool playSound)
		{
			ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.applied"));
		}

		public void Delete(bool playSound)
		{
			ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.deleted"));
		}

		public void Update(bool playSound)
		{
			ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.saved"));
		}

		public void Clear(bool playSound)
		{
			ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.cleared"));
		}

		public void Copy(bool playSound)
		{
			ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.copied"));
		}

		public void Paste(bool playSound)
		{
			ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.pasted"));
		}
	}
}
