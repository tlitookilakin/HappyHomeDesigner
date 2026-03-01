using HappyHomeDesigner.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace HappyHomeDesigner.Integration
{
	internal class Calcifer : IHomeDesignerAPI.ICatalogueProvider
	{
		public static bool Active { get; private set; } = false;
		private const string CALCIFER_ID = "sophie.Calcifer";
		private const string ACTIONS_PATH = $"{CALCIFER_ID}/FurnitureActions";

		private static IAssetName ActionsName;
		private static Func<object> LoadData;

        internal static void Init(IModHelper helper)
		{
			// already initialized
			if (Active)
				return;

			Active = helper.ModRegistry.IsLoaded(CALCIFER_ID);

			// mod not loaded
			if (!Active)
				return;

			ActionsName = helper.GameContent.ParseAssetName(ACTIONS_PATH);
			helper.Events.Content.AssetsInvalidated += Invalidated;

			if(!ModUtilities.TryFindAssembly("Calcifer", out var asm))
			{
				ModEntry.monitor.Log("Could not find Calcifer assembly. Calcifer integration failed.", LogLevel.Warn);
				Active = false;
				return;
			}

			var type = asm.GetType("Calcifer.Features.FurnitureActionData");
			if (type is null)
			{
				ModEntry.monitor.Log("Could not find Calcifer data model. Calcifer integration failed.", LogLevel.Warn);
				Active = false;
				return;
			}

			LoadData = typeof(Calcifer)
				.GetMethod(nameof(LoadAsset), BindingFlags.Static | BindingFlags.NonPublic)
				.MakeGenericMethod(type)
				.CreateDelegate<Func<object>>(helper.GameContent);

			ModEntry.api.AddCatalogueProvider(new Calcifer());
		}

		private static void Invalidated(object sender, AssetsInvalidatedEventArgs e)
		{
			if (e.Equals(ActionsName))
			{
				ModEntry.api.InvalidateProviderCache();
			}
		}

		public IEnumerable<KeyValuePair<string, string>> GetCatalogues()
		{
			List<KeyValuePair<string, string>> pairs = [];
			var shops = ModUtilities.GetCollectorShops().ToHashSet();

			foreach(dynamic pair in (IEnumerable)LoadData())
			{
				string key = pair.Key;
				string value = ((IEnumerable<dynamic>)pair.Value.TileActions)
					.FirstOrDefault(static a => a.TileAction.StartsWith("OpenShop")).TileAction;

				var split = value.Split(' ');
				if (split.Length < 2)
					continue;

				var shopId = split[1];
				if (!shops.Contains(shopId))
					continue;

				pairs.Add(new(key, shopId));
			}

			return pairs;
		}

		private static object LoadAsset<T>(IGameContentHelper helper) where T : class
		{
			return helper.Load<Dictionary<string, T>>(ActionsName);
		}
	}
}
