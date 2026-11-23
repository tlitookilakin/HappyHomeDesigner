using HappyHomeDesigner.Framework;
using HappyHomeDesigner.Menus;
using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Widgets
{
	internal class SimpleItemPool(Func<IGridItem> GetFocused) : IItemPool
	{
		public IReadOnlyList<IGridItem> Items { get; private set; } = [];

		public event EventHandler<ItemPoolChangedEvent> ItemPoolChanged;

		public IGridItem GetFocusedItem()
		{
			return GetFocused();
		}

		public void SetItems(IReadOnlyList<IGridItem> items, bool reset)
		{
			if (items == Items)
				return;

			var old = Items;
			Items = items;
			ItemPoolChanged?.Invoke(this, new(this, old, reset));
		}

		public void Update(IGridItem change, bool added, bool reset = false)
		{
			if (change is not null)
			{
				IReadOnlyList<IGridItem> old = added ? Items.CopyWithout(change) : [.. Items, change];
				ItemPoolChanged?.Invoke(this, new(this, old, reset));
			}
			else
			{
				ItemPoolChanged?.Invoke(this, new(this, null, reset));
			}
		}
	}
}
