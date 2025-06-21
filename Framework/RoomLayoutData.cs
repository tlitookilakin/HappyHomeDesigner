using HappyHomeDesigner.Menus;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Framework
{
	public class RoomLayoutData
	{
		public string Name { get; set; }
		public string ID { get; set; }
		public string Location { get; set; }
		public Dictionary<Vector2, FurnitureItem> Furniture { get; set; } = new();
		public Dictionary<string, string> Walls { get; set; }
		public Dictionary<string, string> Floors { get; set; }
		public string Date { get; set; } = "";
		public string FarmerName { get; set; } = "";

		public record class FurnitureItem(string id, Dictionary<string, string> modData, int rotation, FurnitureItem held)
		{
			public bool CanPlace(GameLocation where, Vector2 tile)
			{
				const CollisionMask mask = CollisionMask.Buildings | CollisionMask.Flooring | CollisionMask.TerrainFeatures;

				var placer = ItemRegistry.Create<Furniture>(id);

				bool ground = placer.isGroundFurniture();

				if (!where.CanPlaceThisFurnitureHere(placer))
					return false;
				if (!ground)
					tile.Y = placer.GetModifiedWallTilePosition(where, (int)tile.X, (int)tile.Y);

				int width = placer.getTilesWide();
				int height = placer.getTilesHigh();
				bool passable = placer.isPassable();
				int type = placer.furniture_type.Value;

				for (int x = 0; x < width; x++)
				{
					for (int y = 0; y < height; y++)
					{
						Vector2 pos = new(x + tile.X, y + tile.Y);
						if (!where.isTilePlaceable(tile, passable))
							return false;
						if (!passable && where.objects.TryGetValue(pos, out var obj) && !obj.isPassable())
							return false;

						if (!ground)
							if (where.IsTileOccupiedBy(pos, mask))
								return false;
							else
								continue;

						if (type is 15 && y is 0)
							if (where.IsTileOccupiedBy(pos, mask))
								return false;
							else
								continue;

						if (where.IsTileBlockedBy(pos, mask))
							return false;

						if (where.terrainFeatures.GetValueOrDefault(pos) is HoeDirt dirt && dirt.crop != null)
							return false;
					}
				}

				if (placer.GetAdditionalFurniturePlacementStatus(where, (int)tile.X * 64, (int)tile.Y * 64) != 0)
					return false;

				return true;
			}

			public void Place(GameLocation where, Vector2 tile)
			{
				var placer = Create();
				placer.TileLocation = tile;
				placer.currentRotation.Value = rotation;
				placer.updateRotation();
				where.furniture.Add(placer);
			}

			public Furniture Create()
			{
				var f = ItemRegistry.Create<Furniture>(id);
				f.modData.CopyFrom(modData);
				if (held != null)
					f.heldObject.Value = held.Create();
				return f;
			}
		}

		public bool TryApply(GameLocation where)
		{
			if (where.Name != Location)
				return false;

			foreach ((var tile, var furn) in Furniture)
				if (!furn.CanPlace(where, tile))
					return false;

			Clear(where);

			foreach ((var tile, var furn) in Furniture)
				furn.Place(where, tile);

			if (where is DecoratableLocation deco)
			{
				if (Walls != null)
					foreach (var wall in Walls)
						deco.SetWallpaper(wall.Key, wall.Value);

				if (Floors != null)
					foreach (var floor in Floors)
						deco.SetWallpaper(floor.Key, floor.Value);
			}

			return true;
		}

		public static void Clear(GameLocation where)
		{
			var inv = Game1.player.team.GetOrCreateGlobalInventory(BlueprintMenu.DROPBOX_ID);

			var preserve = new List<Furniture>();
			foreach (var furn in where.furniture)
			{
				if (furn is StorageFurniture storage)
					foreach (var item in storage.heldItems)
						inv.Add(item);

				if (!furn.AllowLocalRemoval)
					preserve.Add(furn);

				if (furn.heldObject.Value is not StardewValley.Object obj)
					continue;

				if (obj is Chest chest)
					foreach (var item in chest.Items)
						inv.Add(item);

				else if (obj.QualifiedItemId == "(O)" + AssetManager.PORTABLE_ID)
					inv.Add(ItemRegistry.Create("(T)" + AssetManager.PORTABLE_ID));

				else if (obj is not StardewValley.Objects.Furniture)
					inv.Add(furn.heldObject.Value);
			}

			inv.RemoveEmptySlots();
			where.furniture.Clear();

			foreach (var furn in preserve)
				where.furniture.Add(furn);
		}

		public static RoomLayoutData CreateFrom(GameLocation location, string name, string oldAuthor = null)
		{
			var now = DateTime.Now.Date;
			var data = new RoomLayoutData()
			{
				Name = name,
				ID = name.SanitizeFilename(),
				Location = location.Name,
				FarmerName = 
					oldAuthor is not string auth ? Game1.player.Name :
					auth.Contains(Game1.player.Name) ? auth : auth + ", " + Game1.player.Name,
				Date = $"{now.Day}/{now.Month}/{now.Year}"
			};

			foreach (var furn in location.furniture)
				data.Furniture.Add(furn.TileLocation, CreateFrom(furn));

			if (location is DecoratableLocation deco)
			{
				data.Walls = [];
				foreach (var wall in deco.appliedWallpaper.Pairs)
					if (wall.Value is not null)
						data.Walls[wall.Key] = wall.Value;

				data.Floors = [];
				foreach (var floor in deco.appliedFloor.Pairs)
					if (floor.Value is not null)
						data.Floors[floor.Key] = floor.Value;
			}

			return data;
		}

		private static FurnitureItem CreateFrom(Furniture furn)
		{
			return new(furn.QualifiedItemId, furn.modData.Get(), furn.currentRotation.Value, furn.heldObject.Value is Furniture held ? CreateFrom(held) : null);
		}
	}
}
