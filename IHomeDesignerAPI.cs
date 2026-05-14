using System;
using System.Collections.Generic;

namespace HappyHomeDesigner;

#nullable enable
public interface IHomeDesignerAPI
{
	/// <summary>Whether or not a catalogue is open on the current screen</summary>
	public bool IsCatalogOpen { get; }

	/// <summary>Try to open the menu</summary>
	/// <param name="catalogues">The list of catalogue shops to display in the menu. Leave this empty to show all of them.</param>
	/// <returns>If the catalogue was sucessfully opened</returns>
	public bool TryOpenCatalogue(params IEnumerable<string> catalogues);

	/// <summary>Register a mod which attaches catalogue shops to furniture items</summary>
	public void AddCatalogueProvider(ICatalogueProvider provider);

	/// <summary>Call this when a provider's catalogue list changes</summary>
	public void InvalidateProviderCache();

	/// <summary>The currently enabled sharing service, if any.</summary>
	public IShareService? CurrentSharingService { get; set; }

	/// <summary>
	/// Provides information about which objects are linked to which catalogues. <br/>
	/// Use this if your mod provides a framework for turning objects into furniture catalogues.
	/// </summary>
	public interface ICatalogueProvider
	{
		/// <summary>Returns a sequence of pairs, where the key is the qualified item ID and the value is the shop id associated with the catalogue item.</summary>
		public IEnumerable<KeyValuePair<string, string>> GetCatalogues();
	}

	/// <summary>Entry point for a sharing service for blueprints</summary>
	public interface IShareService
	{
		/// <summary>Shows a blueprint save GUI</summary>
		/// <param name="jstring">The json string represnting the selected blueprint</param>
		/// <param name="onSelected">The callback used when sharing is selected/completed</param>
		public void ShowSave(bool playSound, string jstring, Action<bool> onSelected);

		/// <summary>Shows a blueprint load gui</summary>
		/// <param name="onSelected">
		/// The callback used when a blueprint is selected.<br/> 
		/// The string should be a valid json blueprint string.
		/// </param>
		public void ShowLoad(bool playSound, Action<string?> onSelected);
	}
}