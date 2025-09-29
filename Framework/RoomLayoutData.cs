using HappyHomeDesigner.Menus;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
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

		[JsonConverter(typeof(FurnitureDataMigrator))]
		public List<FurnitureItem> Furniture { get; set; } = [];
		public Dictionary<string, string> Walls { get; set; }
		public Dictionary<string, string> Floors { get; set; }
		public string Date { get; set; } = "";
		public string FarmerName { get; set; } = "";

		public record class FurnitureItem(string id, Dictionary<string, string> modData, int rotation, FurnitureItem held, Vector2 tile)
		{
			public bool CanPlace(GameLocation where, out string? error)
			{
				error = null;
				const CollisionMask passMask = CollisionMask.Buildings | CollisionMask.Flooring | CollisionMask.TerrainFeatures;
				const CollisionMask mask = 
					CollisionMask.Buildings | CollisionMask.TerrainFeatures | CollisionMask.LocationSpecific | CollisionMask.Flooring;

				var placer = ItemRegistry.Create<Furniture>(id);

				if (!where.CanPlaceThisFurnitureHere(placer))
				{
					error = $"'{placer.DisplayName}': Placement restrictions not met for object.";
					return false;
				}

				bool ground = placer.isGroundFurniture();
				Vector2 tile2 = tile;
				if (!ground)
					tile2.Y = placer.GetModifiedWallTilePosition(where, (int)tile.X, (int)tile.Y);

				int width = placer.getTilesWide();
				int height = placer.getTilesHigh();
				bool passable = placer.isPassable();
				int type = placer.furniture_type.Value;

				Vector2 tileSnap = new(MathF.Floor(tile.X), MathF.Floor(tile.Y));
				for (int x = 0; x < width; x++)
				{
					for (int y = 0; y < height; y++)
					{
						Vector2 pos = new(tileSnap.X + x, tileSnap.Y + y);

						if (!where.isTilePlaceable(pos, passable))
						{
							error = $"'{placer.DisplayName}': Tile {pos} is not placeable";
							return false;
						}

						if (!passable && where.objects.TryGetValue(pos, out var obj) && !obj.isPassable())
						{
							error = $"'{placer.DisplayName}': Tile {pos} is blocked by object '{obj.DisplayName}'";
							return false;
						}

						if (!ground || (type is 15 && y is 0))
						{
							if (!where.IsTileOccupiedBy(pos, mask, passMask))
								continue;

							error = $"'{placer.DisplayName}': Tile {pos} is blocked";
							return false;
						}

						if (where.IsTileBlockedBy(pos, mask, passMask))
						{
							error = $"'{placer.DisplayName}': Tile {pos} is blocked";
							return false;
						}

						if (where.terrainFeatures.GetValueOrDefault(pos) is HoeDirt dirt && dirt.crop != null)
						{
							error = $"'{placer.DisplayName}': Tile {pos} is blocked by a crop";
							return false;
						}
					}
				}

				switch (placer.GetAdditionalFurniturePlacementStatus(where, (int)tile.X * 64, (int)tile.Y * 64))
				{
					case 0:
						return true;
					case 1:
						error = $"'{placer.DisplayName}': Wall object must be placed on a wall";
						break;
					case 2:
						error = $"'{placer.DisplayName}': Cannot place on a tile that does not allow furniture";
						break;
					case 3:
						error = $"'{placer.DisplayName}': Cannot place floor furniture on a wall";
						break;
					case 4:
						error = $"'{placer.DisplayName}': This location is not decoratable";
						break;
					default:
						error = $"'{placer.DisplayName}': Furniture could not be placed for an unknown reason";
						break;
				}

				return false;
			}

			public void Place(GameLocation where)
			{
				var placer = Create();
				placer.TileLocation = tile;
				placer.currentRotation.Value = rotation;
				placer.updateRotation();
				where.furniture.Add(placer);

				// kills tile snapping from TileLocation
				// needed because placement code breaks precise placement
				var bb = placer.boundingBox.Value;
				placer.boundingBox.Value = new((int)(tile.X * 64f), (int)(tile.Y * 64f), bb.Width, bb.Height);
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

			List<string> errors = [];
			foreach (var furn in Furniture)
				if (!furn.CanPlace(where, out string error))
					errors.Add(error);

			if (errors.Count > 0)
			{
				ModEntry.monitor.Log(
					$"Could not place {errors.Count} furniture items in layout: {string.Join('\n', [.. errors])}", 
					StardewModdingAPI.LogLevel.Debug
				);
				return false;
			}

			Clear(where);

			foreach (var furn in Furniture)
				furn.Place(where);

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
				data.Furniture.Add(CreateFrom(furn));

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
			return new(
				furn.QualifiedItemId, furn.modData.Get(), furn.currentRotation.Value, 
				furn.heldObject.Value is Furniture held ? CreateFrom(held) : null, furn.TileLocation
			);
		}

		public class FurnitureDataMigrator : JsonConverter
		{
			public override bool CanConvert(Type objectType)
			{
				return objectType == typeof(List<FurnitureItem>) || objectType == typeof(Dictionary<Vector2, FurnitureItem>);
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				if (reader.TokenType == JsonToken.StartArray)
					return serializer.Deserialize<List<FurnitureItem>>(reader);

				if (reader.TokenType != JsonToken.StartObject)
					throw new JsonSerializationException("Expected object or array for furniture list, got neither.");

				List<FurnitureItem> items = [];
				while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
				{
					var split = ((string)reader.Value).Split(',', 2);
					var tile = new Vector2(float.Parse(split[0]), float.Parse(split[1]));

					if (!reader.Read())
						break;

					var entry = serializer.Deserialize<FurnitureItem>(reader);
					items.Add(new(entry.id, entry.modData, entry.rotation, entry.held, tile));
				}

				return items;
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				// write as-is
				serializer.Serialize(writer, value);
			}
		}
	}
}
