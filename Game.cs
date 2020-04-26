using SFML.System;
namespace rotstein
{
    class Game {
        public Vector2u Player;
    }

    struct Tile
    {
        public TileKind kind;

        public Tile(TileKind kind)
        {
            this.kind = kind;
        }
    }

    enum TileKind
    {
        Void,
        Planks,
        Stone,
        Iron,
        RedstoneBlock,
    }
}
