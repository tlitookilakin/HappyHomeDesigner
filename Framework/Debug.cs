using HappyHomeDesigner.Menus;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static HappyHomeDesigner.Framework.ModUtilities;

namespace HappyHomeDesigner.Framework
{
    internal static class Debug
    {
        [Conditional("DEBUG")]
        internal static void Init(IModHelper helper)
        {
            helper.Events.Input.ButtonReleased += Input_ButtonReleased;
        }

        private static void Input_ButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (e.Button is SButton.Home)
                Benchmark();
        }

        [Conditional("DEBUG")]
        internal static void Benchmark()
        {
            List<TimeSpan> batches = [];
            var timer = Stopwatch.StartNew();
            var total = Stopwatch.StartNew();

            var shops = ModUtilities.GetCollectorShops();
            shops.Add("Furniture Catalogue");
            shops.Add("Catalogue");

			var batcher = new ShopBatcher(shops);
            var tSetup = timer.Elapsed;
            timer.Restart();

            while (batcher.DoBatch(out _))
            {
                batches.Add(timer.Elapsed);
                timer.Restart();
            }

            timer.Stop();
            var tTotal = total.Elapsed;
            total.Stop();

            ModEntry.monitor.Log($"Batched shop processing:\n\tTotal: {tTotal}\tSetup: {tSetup}\n", LogLevel.Info);
            foreach (var times in batches.Chunk(8))
                ModEntry.monitor.Log($"\t{string.Join('\t', times)}", LogLevel.Info);

			const CatalogType Deluxe = CatalogType.Collector | CatalogType.Furniture | CatalogType.Wallpaper;
			total.Restart();
			Catalog.ShowCatalog(GenerateCombined(Deluxe), Deluxe.ToString());
            total.Stop();

            ModEntry.monitor.Log($"Old Method time: {total.Elapsed}", LogLevel.Info);
		}
    }
}
