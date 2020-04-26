using SFML.System;
namespace rotstein
{
    class Game {
        public Vector2u Player;
        public Tile[,] tiles;
        
        public Game(uint playground_size)
        {
            tiles = new Tile[playground_size, playground_size];
        }
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
