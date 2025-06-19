using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Widgets;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace HappyHomeDesigner.Patches
{
    internal class SearchFocusFix
	{
		public static void Apply(HarmonyHelper helper)
		{
			helper
				.WithProperty<Game1>(nameof(Game1.IsChatting), true).Postfix(IsActive)
				.With("UpdateChatBox").Prefix(SkipChat);
		}

		public static bool IsActive(bool chatActive)
			=> chatActive || Game1.keyboardDispatcher.Subscriber is SearchBox;

		public static bool SkipChat()
		{
			bool run = Game1.keyboardDispatcher.Subscriber is not SearchBox;

			if (run)
				return true;

			KeyboardState keyState = Game1.input.GetKeyboardState();
			GamePadState padState = Game1.input.GetGamePadState();

			if (Game1.input.GetMouseState().RightButton is ButtonState.Pressed)
				(Game1.keyboardDispatcher.Subscriber as SearchBox)?.Reset();

			if (keyState.IsKeyDown(Keys.Escape) || padState.IsButtonDown(Buttons.B) || padState.IsButtonDown(Buttons.Back))
			{
				Game1.oldKBState = keyState;
				Game1.oldPadState = padState;
				Game1.keyboardDispatcher.Subscriber = null;
			}

			return false;
		}
	}
}
