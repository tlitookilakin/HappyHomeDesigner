using HappyHomeDesigner.Data;
using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Menus
{
	public class BlueprintMenu : IClickableMenu
	{
		private readonly GameLocation location;
		private readonly IList<RoomLayoutData> layouts;
		private readonly Texture2D uiTexture = Game1.content.Load<Texture2D>(AssetManager.UI_PATH);

		private readonly ClickableTextureComponent AddButton;
		private readonly List<ClickableTextureComponent> BottomButtons;
		private readonly List<ClickableTextureComponent> TopButtons;
		private readonly List<ClickableTextureComponent> RightButtons;
		private readonly List<ClickableTextureComponent> AllButtons;
		private readonly List<BlueprintEntry> Entries = [];
		private readonly ScrollBar Scroll = new();
		private readonly TextBox NameBox;

		public const string DROPBOX_ID = ModEntry.MOD_ID + "_BPDropbox";

		public int Selected
		{
			get => selectedIndex;
			set
			{
				if (value >= layouts.Count || value < -1)
					value = -1;

				if (value == selectedIndex)
					return;

				if (selectedIndex < 0 != value < 0)
				{
					bool selected = value >= 0;
					foreach (var c in TopButtons)
						c.visible = selected;

					BottomButtons[1].visible = selected;
				}
				selectedIndex = value;
			}
		}

		private string hoverText;
		private string overText;
		private Glyph[] overGlyphs;
		private int overTicks;
		private int selectedIndex;
		private readonly ListSlice<RoomLayoutData> visibleLayouts;

		private const int MAIN_WIDTH = 350;
		private const int PADDING = 8;
		private const int LEFT = 32;
		private const int MARGIN = 32;

		// TODO add image stuff
		public BlueprintMenu(GameLocation location) : base()
		{
			this.location = location;
			layouts = DataService.GetLayoutsFor(location);
			visibleLayouts = new(layouts, ..);
			initializeUpperRightCloseButton();

			NameBox = new BlankTextBox(null, Game1.smallFont, Game1.textColor) { TitleText = ModEntry.i18n.Get("ui.blueprint.name") };
			AddButton = new("add", new(150, 0, 56, 64), null, ModEntry.i18n.Get("ui.blueprint.add"), uiTexture, new(17, 80, 14, 17), 4f, false);
			BottomButtons = [
				new("paste", new(150, 0, 64, 64), null, ModEntry.i18n.Get("ui.blueprint.paste"), uiTexture, new(112, 80, 16, 16), 4f, true),
				new("copy", new(150, 0, 64, 64), null, ModEntry.i18n.Get("ui.blueprint.copy"), uiTexture, new(96, 80, 16, 16), 4f, true)
			];
			TopButtons = [
				new("apply", new(150, 0, 64, 64), null, ModEntry.i18n.Get("ui.blueprint.apply"), uiTexture, new(32, 80, 16, 16), 4f, true),
				new("save", new(150, 0, 64, 64), null, ModEntry.i18n.Get("ui.blueprint.save"), uiTexture, new(64, 80, 16, 16), 4f, true),
				new("delete", new(150, 0, 64, 64), null, ModEntry.i18n.Get("ui.blueprint.delete"), uiTexture, new(48, 80, 16, 16), 4f, true)
			];
			RightButtons = [
				new("clear", new(150, 0, 64, 64), null, ModEntry.i18n.Get("ui.blueprint.clear"), uiTexture, new(80, 80, 16, 16), 4f, true),
				new("dropbox", new(150, 0, 64, 64), null, ModEntry.i18n.Get("ui.blueprint.dropbox"), uiTexture, new(16, 48, 16, 16), 4f, true)
			];
			// loadimage 32, 97, 16, 16
			// saveimage 16, 97, 16, 16

			AllButtons = [.. BottomButtons, .. TopButtons, .. RightButtons];
			allClickableComponents = [upperRightCloseButton, AddButton, .. AllButtons];

			Selected = -1;
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

			AdjustSlotCount(ref height, BlueprintEntry.HEIGHT);

			xPositionOnScreen = MARGIN;
			yPositionOnScreen = bounds.Height / 2 - height / 2;

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

			x = bounds.Width - 32 - 64;
			y = yPositionOnScreen + height;
			foreach (var c in RightButtons)
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

			y = yPositionOnScreen + 64;
			for (int i = 0; i < Entries.Count; i++)
			{
				Entries[i].bounds = new(xPositionOnScreen + LEFT + PADDING + 4, y, MAIN_WIDTH - PADDING * 3, BlueprintEntry.HEIGHT);
				y += BlueprintEntry.HEIGHT;
			}
		}

		private void AdjustSlotCount(ref int height, int slotHeight)
		{
			var h = height - PADDING - 68;
			var count = h / slotHeight;
			height = count * slotHeight + PADDING + 68;

			if (Entries.Count > count)
				Entries.RemoveRange(count, Entries.Count - count);
			else if (Entries.Count < count)
				for (int i = Entries.Count; i < count; i++)
					Entries.Add(new(visibleLayouts, i, MAIN_WIDTH - PADDING * 3));

			Scroll.VisibleRows = count;
		}

		public override void performHoverAction(int x, int y)
		{
			base.performHoverAction(x, y);

			hoverText = null;

			foreach (var c in AllButtons)
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

			foreach (var e in Entries)
			{
				if (e.containsPoint(x, y))
				{
					Selected = e.Index + Scroll.Offset;
					if (Selected >= layouts.Count)
						Selected = -1;
				}
			}

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
				case "dropbox": DisplayDropbox(); break;
			}
		}

		public override void draw(SpriteBatch b)
		{
			visibleLayouts.Range = Scroll.VisibleRange;

			var view = Game1.uiViewport;
			b.Draw(
				uiTexture, new Rectangle(0, 0, xPositionOnScreen + width, view.Height),
				new Rectangle(32, 96, 96, 1), Color.White
			);
			b.Draw(
				uiTexture, new Rectangle(xPositionOnScreen + width, 0, view.Width - 128 - width - xPositionOnScreen, view.Height),
				new Rectangle(127, 96, 1, 1), Color.White
			);
			b.Draw(
				uiTexture, new Rectangle(view.Width - 128, 0, 128, view.Height),
				new Rectangle(32, 96, 96, 1), Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f
			);

			drawTextureBox(b, xPositionOnScreen + LEFT, yPositionOnScreen + 4, MAIN_WIDTH, height - 4, Color.White);
			Scroll.Draw(b);

			foreach (var c in AllButtons)
				c.draw(b);

			foreach (var e in Entries)
				e.Draw(b, Selected - Scroll.Offset);

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
			overGlyphs = Glyph.Layout(text, MAIN_WIDTH - 64, Glyph.Alignment.Center, Glyph.Alignment.Center);
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
			var panel = new Rectangle(xPositionOnScreen + LEFT, yPositionOnScreen, MAIN_WIDTH, height);

			drawTextureBox(
				b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), panel.X, 
				panel.Y, panel.Width, panel.Height, Color.Black * .5f * alpha, drawShadow: false
			);

			float amp = 16f * MathF.Max(80 - overTicks, 0) / 80f;
			var color = Utility.GetPrismaticColor(speedMultiplier: 4f);
			var position = panel.Center.ToVector2();

			for(int i = 0; i < overGlyphs.Length; i++)
			{
				var glyph = overGlyphs[i];
				b.Draw(
					glyph.Texture, 
					glyph.Position + new Vector2(0f, MathF.Sin(i * .7f + overTicks * .15f) * amp) + position, 
					glyph.Source, color, 0f, Vector2.Zero, glyph.Scale, SpriteEffects.None, 0f
				);
			}
		}

		public override bool readyToClose()
		{
			if (NameBox.Selected)
				return false;

			return base.readyToClose();
		}

		public void Add(bool playSound)
		{
			var name = NameBox.Text.Trim();
			if (name.Length is 0)
				return;

			NameBox.Text = "";

			layouts.Add(RoomLayoutData.CreateFrom(location, name));
			Selected = layouts.Count - 1;
			DataService.SaveLayoutsFor(location, layouts);

			ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.added"));
			if (playSound)
				Game1.playSound("newArtifact");
		}

		public void Apply(bool playSound)
		{
			if (Selected < 0)
				return;

			if (layouts[Selected].TryApply(location))
			{
				ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.applied"));
				if (playSound)
					Game1.playSound("newArtifact");
			}
			else
			{
				ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.failure"));
				if (playSound)
					Game1.playSound("cancel");
			}
		}

		public void Delete(bool playSound)
		{
			if (Selected < 0)
				return;

			layouts.RemoveAt(Selected);
			Selected = -1;
			DataService.SaveLayoutsFor(location, layouts);

			ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.deleted"));
			if (playSound)
				Game1.playSound("newArtifact");
		}

		public void Update(bool playSound)
		{
			if (Selected < 0)
				return;

			layouts[Selected] = RoomLayoutData.CreateFrom(location, layouts[Selected].Name, layouts[Selected].FarmerName);
			DataService.SaveLayoutsFor(location, layouts);

			ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.saved"));
			if (playSound)
				Game1.playSound("newArtifact");
		}

		public void Clear(bool playSound)
		{
			RoomLayoutData.Clear(location);

			ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.cleared"));
			if (playSound)
				Game1.playSound("newArtifact");
		}

		public void Copy(bool playSound)
		{
			if (Selected < 0)
				return;

			var s = JsonConvert.SerializeObject(layouts[Selected]);
			if (!DesktopClipboard.SetText(s))
				return;

			ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.copied"));
			if (playSound)
				Game1.playSound("newArtifact");
		}

		public void Paste(bool playSound)
		{
			string s = "";
			RoomLayoutData data = null;

			if (DesktopClipboard.GetText(ref s))
			{
				try
				{
					data = JsonConvert.DeserializeObject<RoomLayoutData>(s);
				}
				catch (Exception ex)
				{
					ModEntry.monitor.Log($"Error reading layout from clipboard:\n{ex}", StardewModdingAPI.LogLevel.Trace);
				}
			}

			if (data is null)
			{
				ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.empty"));
				if (playSound)
					Game1.playSound("cancel");
				return;
			}

			layouts.Add(data);
			Selected = layouts.Count - 1;
			DataService.SaveLayoutsFor(location, layouts);

			ShowOverlayText(ModEntry.i18n.Get("ui.blueprint.pasted"));
			if (playSound)
				Game1.playSound("newArtifact");
		}

		public void DisplayDropbox()
		{
			var inv = Game1.player.team.GetOrCreateGlobalInventory(DROPBOX_ID);
			var menu = new ItemGrabMenu(inv)
			{
				exitFunction = () => { Game1.activeClickableMenu = this; }
			};
			Game1.activeClickableMenu = menu;
		}
	}
}
