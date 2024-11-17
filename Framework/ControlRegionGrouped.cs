using HappyHomeDesigner.Menus;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyHomeDesigner.Framework
{
	public class ControlRegionGrouped : ControlRegion
	{
		public IReadOnlyList<ControlRegion> Regions
		{
			get => regions;
			set
			{
				regions = value;
				RecalculateBounds();
			}
		}
		private IReadOnlyList<ControlRegion> regions;

		public ControlRegionGrouped(IReadOnlyList<ControlRegion> regions)
		{
			this.regions = regions;
			RecalculateBounds();
			Handler = HandleMovement;
		}

		public bool HandleMovement(ref int mouseX, ref int mouseY, int direction, out ControlRegion? toRegion, bool inside)
		{
			toRegion = null;
			Point p = new(mouseX, mouseY);
			var target = inside ?
				regions.FirstOrDefault(r => r.Bounds.Contains(p)) :
				GetClosestDirectional(mouseX, mouseY, direction);

			if (target is not null)
			{
				while (!target.TryApplyMovement(ref mouseX, ref mouseY, direction, out target))
					if (target == null)
						return false;
				return true;
			}

			return false;
		}

		private ControlRegion? GetClosestDirectional(int x, int y, int dir)
		{
			Point p = new(x, y);
			int minDist = int.MaxValue;
			ControlRegion? closest = null;

			foreach (var region in regions)
			{
				var b = region.Bounds;
				var dist = (b.Center - p).GetLength();
				if (dist >= minDist)
					continue;

				if (
					(dir == Direction.RIGHT && b.Right < p.X) ||
					(dir == Direction.LEFT && b.Left > p.X) ||
					(dir == Direction.UP && b.Top < p.Y) ||
					(dir == Direction.DOWN && b.Bottom > p.Y)
				)
					continue;

				minDist = dist;
				closest = region;
			}

			return closest;
		}

		public void RecalculateBounds()
		{
			int minx, miny, maxx, maxy;
			minx = miny = maxx = maxy = 0;

			for (int i = 0; i < regions.Count; i++)
			{
				var b = regions[i].Bounds;
				minx = Math.Min(b.Left, minx);
				maxx = Math.Max(b.Right, maxx);
				miny = Math.Min(b.Top, miny);
				maxy = Math.Max(b.Bottom, maxy);
			}

			Bounds = new(minx, miny, maxx - minx, maxy - miny);
		}
	}
}
