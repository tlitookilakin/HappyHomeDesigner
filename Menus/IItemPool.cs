using System;
using System.Collections.Generic;

namespace HappyHomeDesigner.Menus
{
    public interface IItemPool
    {
        public IReadOnlyList<IGridItem> Items { get; }

        public event EventHandler<ItemPoolChangedEvent> ItemPoolChanged;

        public IGridItem GetFocusedItem();
    }

    public record class ItemPoolChangedEvent(IItemPool Source, IReadOnlyList<IGridItem> OldItems, bool Reset);
}
