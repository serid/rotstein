using SFML.System;
namespace rotstein
{
    class Game {
        public Vector2u Player;
        public Tile[,] Tiles;
        
        public Game(uint playground_size)
        {
            Tiles = new Tile[playground_size, playground_size];
        }
    }

    struct Tile
    {
        public TileKind Kind;

        public Tile(TileKind kind)
        {
            this.Kind = kind;
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
