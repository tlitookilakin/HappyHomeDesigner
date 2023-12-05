using HappyHomeDesigner.Integration;
using HappyHomeDesigner.Patches;
using HarmonyLib;
using StardewModdingAPI;
using System;
using System.IO;

namespace HappyHomeDesigner
{
	public class ModEntry : Mod
	{
		internal static IMonitor monitor;
		internal static IModHelper helper;

		public override void Entry(IModHelper helper)
		{
			monitor = Monitor;
			ModEntry.helper = helper;
			helper.Events.GameLoop.GameLaunched += Launched;
		}

		private void Launched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
		{
			Patch(new(ModManifest.UniqueID));

			if (Helper.ModRegistry.IsLoaded("spacechase0.DynamicGameAssets"))
			{
				IDynamicGameAssets.API = Helper.ModRegistry.GetApi<IDynamicGameAssets>("spacechase0.DynamicGameAssets");
				IDynamicGameAssets.API.AddEmbeddedPack(ModManifest, Path.Combine(Helper.DirectoryPath, "assets"));
			}

			AlternativeTextures.Init(Helper);
		}

		private static void Patch(Harmony harmony)
		{
			TileAction.Apply(harmony);
			ItemCloneFix.Apply(harmony);
			AltTex.Apply(harmony);
		}
	}
}
