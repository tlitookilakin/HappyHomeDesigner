﻿using StardewValley;
using StardewValley.Locations;
using System;
using System.Net;

namespace HappyHomeDesigner.Framework
{
	public struct WallFloorState : IEquatable<WallFloorState>
	{
		public bool isFloor;
		public string area;
		public string which;
		public string old;

		public static bool Apply(WallFloorState state, GameLocation where, bool forward)
		{
			if (state.area is null || where is null)
				return false;

			string what = forward ? state.which : state.old;

			if (what is null || where is not DecoratableLocation deco)
				return false;

			if (state.isFloor)
				deco.SetFloor(what, state.area);
			else
				deco.SetWallpaper(what, state.area);

			return true;
		}

		public override bool Equals(object obj)
			=> obj is WallFloorState state && Equals(state);

		public bool Equals(WallFloorState other)
			=> isFloor == other.isFloor &&
				area == other.area &&
				which == other.which;

		public override int GetHashCode()
			=> HashCode.Combine(isFloor, area, which);

		public static bool operator ==(WallFloorState left, WallFloorState right)
			=> left.Equals(right);

		public static bool operator !=(WallFloorState left, WallFloorState right)
			=> !(left == right);
	}
}
