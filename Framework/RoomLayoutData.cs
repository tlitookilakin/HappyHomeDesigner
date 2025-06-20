using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Framework
{
	// TODO: add global inv for item dumping
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
				var placer = ItemRegistry.Create<Furniture>(id);
				var valid = placer.canBePlacedHere(
					where, tile, 
					CollisionMask.Buildings | CollisionMask.Farmers | CollisionMask.Furniture | 
					CollisionMask.Objects | CollisionMask.TerrainFeatures | CollisionMask.LocationSpecific
				);
				return valid;
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

			Clear(where);

			foreach ((var tile, var furn) in Furniture)
				if (!furn.CanPlace(where, tile))
					return false;

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
			var preserve = new List<Furniture>();
			foreach (var furn in where.furniture)
			{
				if (furn is StorageFurniture storage && storage.heldItems.Count != 0)
					preserve.Add(furn);

				if (!furn.AllowLocalRemoval)
					preserve.Add(furn);

				if (furn.heldObject.Value is Chest)
					preserve.Add(furn);

				if (furn.heldObject.Value is not StardewValley.Objects.Furniture && furn.heldObject.Value is not null)
					Game1.createItemDebris(furn.heldObject.Value, furn.TileLocation * 64f, -1, where);
			}

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
