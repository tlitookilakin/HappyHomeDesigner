using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HappyHomeDesigner.Framework;

public record struct Glyph(Vector2 Position, Rectangle Source, Texture2D Texture, float Scale)
{
	public enum Alignment { Start, Center, End };

	public static Glyph[] Layout(string text, int width, Alignment halign, Alignment valign)
	{
		if (text is null || text.Trim().Length == 0)
		{
			ModEntry.monitor.Log("Attempted to display empty text! Your language file is broken!", StardewModdingAPI.LogLevel.Warn);
			ModEntry.monitor.Log("Your translation is bad and you should feel bad.");
			return [];
		}

		var glyphs = new Glyph[text.Length];
		List<string> lines = [];

		if (LocalizedContentManager.CurrentLanguageCode is
			LocalizedContentManager.LanguageCode.zh or
			LocalizedContentManager.LanguageCode.ja or
			LocalizedContentManager.LanguageCode.th
		)
		{
			string cum = "";

			foreach (var match in (IList<Match>)Game1.asianSpacingRegex.Matches(text))
			{
				var s = match.Value;
				if (SpriteText.getWidthOfString(cum + s) > width)
				{
					lines.Add(cum);
					cum = s;
				}
				else
				{
					cum += ' ' + s;
				}
			}
			if (lines[^1] != cum)
				lines.Add(cum);
		}
		else
		{
			int last = 0;
			string cum = "";

			for (int i = 0; i <= text.Length; i++)
			{
				if (i == text.Length || text[i] is ' ' or '^')
				{
					if (last < i)
					{
						var s = text[last..i];
						if ((i < text.Length && text[i] is '^') || SpriteText.getWidthOfString(cum + s) > width)
						{
							lines.Add(cum);
							cum = s;
						}
						else
						{
							if (cum.Length is 0)
								cum = s;
							else
								cum += ' ' + s;
						}
					}

					if (i == text.Length)
						lines.Add(cum);

					last = i + 1;
				}
			}
		}

		int c = 0;
		int lineHeight = (int)(18f * SpriteText.FontPixelZoom);
		int y = valign switch {
			Alignment.Start => 0,
			Alignment.End => -(lineHeight * lines.Count),
			Alignment.Center => -(lineHeight * lines.Count) / 2,
			_ => throw new ArgumentException("Must be a valid alignment", nameof(valign))
		};

		int ind = 0;
		foreach (var line in lines)
		{
			LayoutLine(line, y, width, halign, ref ind, glyphs);
			c += line.Length;
			y += lineHeight;
		}
		return glyphs[..ind];
	}

	private static void LayoutLine(string line, int y, int width, Alignment halign, ref int ind, Glyph[] glyphs)
	{
		var w = SpriteText.getWidthOfString(line);
		int x = halign switch
		{
			Alignment.Start => 0,
			Alignment.End => -w,
			Alignment.Center => -w / 2,
			_ => throw new ArgumentException("Must be a valid alignment", nameof(halign))
		};


		// mostly copied from SpriteText, with some simplification
		Vector2 pos = new(x, y);

		if (SpriteText.FontPixelZoom < 4f &&
			LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.ko &&
			LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.zh
		)
			pos.Y += (int)((4f - SpriteText.FontPixelZoom) * 4f);

		if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko)
			pos.Y -= 8;
		else if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.zh)
			pos.Y += 4;

		int accum = 0;

		for (int i = 0; i < line.Length; i++)
		{
			var c = line[i];

			if (
				LocalizedContentManager.CurrentLanguageLatin || SpriteText.forceEnglishFont ||
				(LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru && !Game1.options.useAlternateFont)
			)
			{
				float tempzoom = SpriteText.fontPixelZoom;
				if (SpriteText.forceEnglishFont)
					SpriteText.fontPixelZoom = 3f;

				accum = 0;
				bool upper = char.IsUpper(c) || c is 'ß';
				Vector2 spriteFontOffset = new(0f, -1 + (upper ? (-3) : 0));
				if (c is 'Ç')
					spriteFontOffset.Y += 2f;

				Rectangle srcRect = SpriteTextSourceRect(c, false);
				glyphs[ind] = new(pos + spriteFontOffset * SpriteText.FontPixelZoom, srcRect, SpriteText.coloredTexture, SpriteText.FontPixelZoom);
				if (i < line.Length - 1)
					pos.X += 8f * SpriteText.FontPixelZoom + accum + SpriteText.getWidthOffsetForChar(line[i + 1]) * SpriteText.FontPixelZoom;

				SpriteText.fontPixelZoom = tempzoom;
			}
			else if (SpriteText.characterMap.TryGetValue(c, out var fc))
			{
				Rectangle sourcerect = new(fc.X, fc.Y, fc.Width, fc.Height);
				Texture2D _texture = SpriteText.fontPages[fc.Page];
				if (SpriteText.positionOfNextSpace(line, i, (int)pos.X, accum) >= x + width - 4)
				{
					pos.Y += (SpriteText.FontFile.Common.LineHeight + 2) * SpriteText.FontPixelZoom;
					accum = 0;
					pos.X = x;
				}
				Vector2 position2 = new(pos.X + fc.XOffset * SpriteText.FontPixelZoom, pos.Y + fc.YOffset * SpriteText.FontPixelZoom);

				glyphs[ind] = new(position2, sourcerect, _texture, SpriteText.FontPixelZoom);
				pos.X += fc.XAdvance * SpriteText.FontPixelZoom;
			}

			ind++;
		}
	}

	private static readonly Func<char, bool, Rectangle> SpriteTextSourceRect
		= typeof(SpriteText).GetMethod("getSourceRectForChar", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
		.CreateDelegate<Func<char, bool, Rectangle>>();
}
