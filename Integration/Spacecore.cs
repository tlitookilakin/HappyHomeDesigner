using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace HappyHomeDesigner.Integration
{
	public static class Spacecore
	{
		public static Func<string, List<SpaceTab>> GetTabs = (s) => null;

		internal static void Init()
		{
			if (!ModEntry.helper.ModRegistry.IsLoaded("spacechase0.SpaceCore"))
				return;

			var asm = ModEntry.helper.ModRegistry.GetApi("spacechase0.SpaceCore")?.GetType()?.Assembly;
			if (asm is null)
				return;

			var type = asm.GetType("SpaceCore.VanillaAssetExpansion.ShopExtensionData");
			if (type is null)
				return;

			GetTabs = typeof(Spacecore)
				.GetMethod(nameof(GetTabsFor), BindingFlags.Static | BindingFlags.NonPublic)
				.MakeGenericMethod(type).CreateDelegate<Func<string, List<SpaceTab>>>();
		}

		private static List<SpaceTab> GetTabsFor<T>(string id)
		{
			var dict = ModEntry.helper.GameContent.Load<Dictionary<string, T>>("spacechase0.SpaceCore/ShopExtensionData");
			if (!dict.TryGetValue(id, out var data))
				return null;

			List<SpaceTab> ret = [];

			IEnumerable<dynamic> tabs = ((dynamic)data).CustomTabs;
			foreach (var tab in tabs)
			{
				ret.Add(new()
				{
					Id = tab.Id,
					IconTexture = tab.IconTexture,
					IconRect = tab.IconRect,
					FilterCondition = tab.FilterCondition
				});
			}

			try
			{
				foreach (var tab in ret)
					tab.texture = ModEntry.helper.GameContent.Load<Texture2D>(tab.IconTexture);
			}
			catch (Exception ex)
			{
				ModEntry.monitor.Log($"Error loading texture for custom tab: {ex}", LogLevel.Error);
				return null;
			}

			return ret;
		}
	}

	public class SpaceTab
	{
		public string Id { get; set; }
		public string IconTexture { get; set; }
		public Rectangle IconRect { get; set; }
		public string FilterCondition { get; set; }
		internal Texture2D texture;
	}
}
