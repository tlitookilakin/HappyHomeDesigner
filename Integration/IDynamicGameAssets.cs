using StardewModdingAPI;

namespace HappyHomeDesigner.Integration
{
	public interface IDynamicGameAssets
	{
		internal static IDynamicGameAssets API;

		/// <summary>
		/// Register a DGA pack embedded in another mod.
		/// Needs the standard DGA fields in the manifest. (See documentation.)
		/// Probably shouldn't use config-schema.json for these, because if you do it will overwrite your mod's config.json.
		/// </summary>
		/// <param name="manifest">The mod manifest.</param>
		/// <param name="dir">The absolute path to the directory of the pack.</param>
		void AddEmbeddedPack(IManifest manifest, string dir);
	}
}
