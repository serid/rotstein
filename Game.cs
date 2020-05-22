using SFML.System;
namespace rotstein
{
    class Game
    {
        public Player Player = new Player();
        public Tile[,] Tiles;

        public Game(uint playground_size)
        {
            Tiles = new Tile[playground_size, playground_size];
        }
    }

    class Player
    {
        public Vector2u Position;
        public byte AnimationStep;
        public bool Direction; // false is left, true is right
        public Hotbar Hotbar;

        public Player()
        {
            Hotbar.Tiles = new TileKind[10];
            Hotbar.Tiles[0] = TileKind.Iron;
            Hotbar.Tiles[1] = TileKind.Planks;
            Hotbar.Tiles[2] = TileKind.RedstoneBlock;
            Hotbar.Tiles[3] = TileKind.Stone;
        }

        public byte NextAnimationStep()
        {
            AnimationStep += 1;
            if (AnimationStep == 2 + 1)
            {
                AnimationStep = 0;
            }
            return AnimationStep;
        }
    }

    struct Hotbar
    {
        public TileKind[] Tiles;
        public uint Index; // Which item from hotbar is picked

        public TileKind IndexTile => Tiles[Index];
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
