using HappyHomeDesigner.Menus;
using System.Collections.Generic;
using System.Linq;

namespace HappyHomeDesigner.Framework
{
    public class API : IHomeDesignerAPI
    {
        private readonly HashSet<IHomeDesignerAPI.ICatalogueProvider> providers = [];
        private Dictionary<string, string> shopsByFurniture;
        private Dictionary<string, string> furnitureByShops;

        public Dictionary<string, string> ShopsByFurniture
        {
            get
            {
                if (shopsByFurniture is null)
                    ReloadCache();
                return shopsByFurniture;
            }
        }
        public Dictionary<string, string> FurnitureByShops
        {
            get
            {
                if (furnitureByShops is null)
                    ReloadCache();
                return furnitureByShops;
            }
        }

        public bool IsCatalogOpen => Catalog.ActiveMenu.Value != null;

        public void AddCatalogueProvider(IHomeDesignerAPI.ICatalogueProvider provider)
        {
            providers.Add(provider);
        }

        public void InvalidateProviderCache()
        {
            shopsByFurniture = null;
            furnitureByShops = null;
        }

        public bool TryOpenCatalogue(params IEnumerable<string> catalogues)
        {
            if (!catalogues.Any())
                catalogues = ModUtilities.GetCollectorShops("Furniture Catalogue", "Catalogue");

            Catalog.ShowCatalog(catalogues);

            return Catalog.ActiveMenu.Value != null;
        }

        private void ReloadCache()
        {
            var pairs = providers.SelectMany(p => p.GetCatalogues());

            shopsByFurniture = new(pairs);
            furnitureByShops = new(pairs.Select(p => new KeyValuePair<string, string>(p.Value, p.Key)));
        }
    }
}
