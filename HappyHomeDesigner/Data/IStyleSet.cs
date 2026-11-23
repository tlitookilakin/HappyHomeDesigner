using Microsoft.Xna.Framework;
using StardewValley;

namespace HappyHomeDesigner.Data
{
    public interface IStyleSet
    {
        public string IconTexture { get; }
        public Rectangle IconSource { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public bool Contains(Item item);
    }
}
