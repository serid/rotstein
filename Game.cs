using SFML.System;
namespace rotstein
{
    class Game
    {
        public TPlayer Player = new TPlayer();
        public Tile[,] Tiles;

        public Game(uint playground_size)
        {
            Tiles = new Tile[playground_size, playground_size];
        }

        public class TPlayer
        {
            public Vector2u Position;
            public byte AnimationStep;
            public bool Direction; // false is left, true is right
            public THotbar Hotbar;

            public TPlayer()
            {
                Hotbar.Tiles = new Tile.TKind[10];
                Hotbar.Tiles[0] = Tile.TKind.Iron;
                Hotbar.Tiles[1] = Tile.TKind.Planks;
                Hotbar.Tiles[2] = Tile.TKind.RedstoneBlock;
                Hotbar.Tiles[3] = Tile.TKind.Stone;
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

            public struct THotbar
            {
                public Tile.TKind[] Tiles;
                public uint Index; // Which item from hotbar is picked

                public Tile.TKind IndexTile => Tiles[Index];
            }
        }
    }

    struct Tile
    {
        public Tile.TKind Kind;

        public Tile(Tile.TKind kind)
        {
            this.Kind = kind;
        }

        public enum TKind
        {
            Void,
            Planks,
            Stone,
            Iron,
            RedstoneBlock,
        }
    }
}
