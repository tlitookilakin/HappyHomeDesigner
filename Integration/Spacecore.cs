using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace HappyHomeDesigner.Integration
{
	public class Spacecore : IHomeDesignerAPI.ICatalogueProvider
	{
		public static Func<string, List<SpaceTab>> GetTabs = (s) => null;
		private static Func<IEnumerable<KeyValuePair<string, string>>> GetCataloguesImpl;
		private static IAssetName FurnitureDataName;

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

			type = asm.GetType("SpaceCore.VanillaAssetExpansion.FurnitureExtensionData");
			if (type is null)
				return;

			GetCataloguesImpl = typeof(Spacecore)
				.GetMethod(nameof(ReadFurnitureActions), BindingFlags.Static | BindingFlags.NonPublic)
				.MakeGenericMethod(type).CreateDelegate<Func<IEnumerable<KeyValuePair<string, string>>>>();

			ModEntry.api.AddCatalogueProvider(new Spacecore());

			FurnitureDataName = ModEntry.helper.GameContent.ParseAssetName("spacechase0.SpaceCore/FurnitureExtensionData");
			ModEntry.helper.Events.Content.AssetsInvalidated += Invalidated;
		}

		private static void Invalidated(object sender, AssetsInvalidatedEventArgs e)
		{
			if (e.NamesWithoutLocale.Contains(FurnitureDataName))
				ModEntry.api.InvalidateProviderCache();
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

		public IEnumerable<KeyValuePair<string, string>> GetCatalogues()
		{
			return GetCataloguesImpl();
		}

		private static IEnumerable<KeyValuePair<string, string>> ReadFurnitureActions<T>()
		{
			var dict = (IEnumerable)Game1.content.Load<Dictionary<string, T>>("spacechase0.SpaceCore/FurnitureExtensionData");
			List<KeyValuePair<string, string>> pairs = [];

			foreach (dynamic entry in dict)
			{
				Dictionary<Vector2, Dictionary<string, Dictionary<string, string>>> props = entry.Value.TileProperties;
				foreach (var prop in props.Values) 
				{
					if (!prop.TryGetValue("Buildings", out var layer) || !layer.TryGetValue("Action", out var action))
						continue;

					var split = ArgUtility.SplitBySpaceQuoteAware(action);
					if (split[0] != "OpenShop")
						continue;

					pairs.Add(new(entry.Key, split[1]));
					break;
				}
			}

			return pairs;
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
