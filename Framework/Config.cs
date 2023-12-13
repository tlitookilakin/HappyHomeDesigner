using HappyHomeDesigner.Integration;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;

namespace HappyHomeDesigner.Framework
{
	public class Config
	{
		public bool CloseWithKey { get; set; }
		public KeybindList GiveModifier { get; set; }
		public KeybindList FavoriteModifier { get; set; }

		public Config()
		{
			Reset();
		}

		public void Register(IGMCM gmcm, IManifest man)
		{
			gmcm.Register(man, Reset, Save);

			gmcm.AddQuickBool(man, this, nameof(CloseWithKey));
			gmcm.AddQuickKeybindList(man, this, nameof(GiveModifier));
			gmcm.AddQuickKeybindList(man, this, nameof(FavoriteModifier));
		}

		private void Reset()
		{
			CloseWithKey = true;
			GiveModifier = new(SButton.LeftShift);
			FavoriteModifier = new(SButton.LeftControl);
		}
		private void Save()
		{
			ModEntry.helper.WriteConfig(this);
		}
	}
}
