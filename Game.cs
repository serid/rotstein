using SFML.System;
namespace rotstein
{
    class Game
    {
        public Player Player;
        public Tile[,] Tiles;

        public Game(uint playground_size)
        {
            Tiles = new Tile[playground_size, playground_size];
        }
    }

    struct Player
    {
        public Vector2u Position;
        public byte AnimationStep;
        public bool Direction; // false is left, true is right

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
