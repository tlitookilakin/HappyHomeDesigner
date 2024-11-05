using HappyHomeDesigner.Menus;
using Microsoft.Xna.Framework;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HappyHomeDesigner.Framework
{
	public class ControlRegion
	{
		public delegate bool MovementHandler(ref int mouseX, ref int mouseY, int direction, out ControlRegion? toRegion, bool isInside);

		public Rectangle Bounds;
		public ControlRegion? Left;
		public ControlRegion? Right;
		public ControlRegion? Top;
		public ControlRegion? Bottom;
		public MovementHandler? Handler;
		public List<ClickableComponent>? clickables;

		public bool TryApplyMovement(ref int mouseX, ref int mouseY, int direction, out ControlRegion? toRegion)
		{
			toRegion = null;
			Point mPos = new(mouseX, mouseY);

			if (clickables != null && clickables.Count > 0)
			{
				ClickableComponent? target = Bounds.Contains(mouseX, mouseY) ?
					clickables.GetIntersecting(mouseX, mouseY).GetNeighbor(direction, clickables) :
					clickables.MinBy(c => (c.bounds.Center - mPos).GetLength());

				if (target is not null)
				{
					mouseX = target.bounds.Center.X;
					mouseY = target.bounds.Center.Y;
					return true;
				}
			}
			else if (Handler is not null)
			{
				if(Handler(ref mouseX, ref mouseY, direction, out toRegion, Bounds.Contains(mouseX, mouseY)))
					return true;
			}

			toRegion = direction switch
			{
				Direction.RIGHT => Right,
				Direction.LEFT => Left,
				Direction.UP => Top,
				Direction.DOWN => Bottom,
				_ => null
			};
			return false;
		}
	}
}
