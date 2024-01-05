using HappyHomeDesigner.Framework;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyHomeDesigner.Integration
{
	internal class CustomFurniture
	{
		// Why do I even need to do this? why doesn't plato just postfix the utility method?

		public static IEnumerable<ISalable> customFurniture;
		public static bool Installed;
		
		public static void Init(IModHelper helper)
		{
			Installed = false;
			if (!helper.ModRegistry.IsLoaded("Platonymous.CustomFurniture"))
				return;

			ModEntry.monitor.Log("Custom Furniture detected! Integrating...", LogLevel.Debug);

			if (!ModUtilities.TryFindAssembly("CustomFurniture", out var asm))
			{
				ModEntry.monitor.Log("Failed to find CF assembly, could not integrate.", LogLevel.Warn);
				return;
			}

			var type = asm.GetType("CustomFurnitureMod");
			var field = type?.GetField("furniture");
			var modInst = type?.GetField("instance")?.GetValue(null);
			var checker = 
				modInst is null ?
				(s) => true :
				type.GetMethod("meetsConditions").ToDelegate<Func<string, bool>>(modInst);

			if (field is null)
			{
				ModEntry.monitor.Log("Failed to find furniture list, could not integrate.", LogLevel.Warn);
				return;
			}
			if (modInst is null)
			{
				ModEntry.monitor.Log("Failed to capture Mod instance. Conditions for Custom Furniture will not be checked!", LogLevel.Warn);
			}

			customFurniture = 
				from furn
				in field.GetValue(null) as IEnumerable<dynamic>
				where furn.data.sellAtShop && (furn.data.conditions is "none" || checker(furn.data.conditions))
				select furn as ISalable;

			Installed = true;
			ModEntry.monitor.Log("Custom Furniture successfully integrated!", LogLevel.Debug);
		}
	}
}
