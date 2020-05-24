using SFML.System;
namespace rotstein
{
    class Game
    {
        public TPlayer Player { get; } = new TPlayer();
        public Tile[,] Tiles { get; }

        bool[,] Prealloc_UpToDateNodes;
        bool[,] Prealloc_RedCheckedNodes;

        public Game(uint playground_size)
        {
            Tiles = new Tile[playground_size, playground_size];
            Prealloc_UpToDateNodes = new bool[Tiles.GetLength(0), Tiles.GetLength(1)];
            Prealloc_RedCheckedNodes = new bool[Tiles.GetLength(0), Tiles.GetLength(1)];
        }

        public void PlaceTile(uint x, uint y, Tile tile)
        {
            var oldTileKind = Tiles[x, y].Kind;
            Tiles[x, y] = tile;

            System.Array.Clear(Prealloc_UpToDateNodes, 0, Prealloc_UpToDateNodes.Length); // Clear preallocated array before using it

            switch (oldTileKind)
            { // Update neigbors if old tile was important
                case Tile.TKind.InactiveRedstone:
                case Tile.TKind.ActiveRedstone:
                case Tile.TKind.RedstoneBlock:
                    UpdateTile(x, y - 1, Prealloc_UpToDateNodes);
                    UpdateTile(x - 1, y, Prealloc_UpToDateNodes);
                    UpdateTile(x, y + 1, Prealloc_UpToDateNodes);
                    UpdateTile(x + 1, y, Prealloc_UpToDateNodes);
                    break;
            }

            switch (tile.Kind)
            { // Update self if new tile is dynamic
              // Update neigbors if new tile is important
                case Tile.TKind.InactiveRedstone:
                case Tile.TKind.ActiveRedstone:
                    UpdateTile(x, y, Prealloc_UpToDateNodes);
                    break;
                case Tile.TKind.RedstoneBlock:
                    UpdateTile(x, y - 1, Prealloc_UpToDateNodes);
                    UpdateTile(x - 1, y, Prealloc_UpToDateNodes);
                    UpdateTile(x, y + 1, Prealloc_UpToDateNodes);
                    UpdateTile(x + 1, y, Prealloc_UpToDateNodes);
                    break;
            }
        }

        void UpdateTile(uint x, uint y, bool[,] upToDateNodes)
        {
            if (upToDateNodes[x, y])
                return;

            upToDateNodes[x, y] = true;

            System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it

            switch (Tiles[x, y].Kind)
            {
                case Tile.TKind.InactiveRedstone:
                    if (isRedReachable(x, y, Prealloc_RedCheckedNodes))
                    {
                        Tiles[x, y].Kind = Tile.TKind.ActiveRedstone;

                        UpdateTile(x, y - 1, upToDateNodes);
                        UpdateTile(x - 1, y, upToDateNodes);
                        UpdateTile(x, y + 1, upToDateNodes);
                        UpdateTile(x + 1, y, upToDateNodes);
                    }
                    break;
                case Tile.TKind.ActiveRedstone:
                    if (!(isRedReachable(x, y, Prealloc_RedCheckedNodes)))
                    {
                        Tiles[x, y].Kind = Tile.TKind.InactiveRedstone;

                        UpdateTile(x, y - 1, upToDateNodes);
                        UpdateTile(x - 1, y, upToDateNodes);
                        UpdateTile(x, y + 1, upToDateNodes);
                        UpdateTile(x + 1, y, upToDateNodes);
                    }
                    break;
            }
        }

        bool isRedReachable(uint x, uint y, bool[,] checkedNodes)
        {
            if (checkedNodes[x, y])
                return false;

            checkedNodes[x, y] = true;

            switch (Tiles[x, y].Kind)
            {
                case Tile.TKind.RedstoneBlock:
                    return true;
                case Tile.TKind.InactiveRedstone:
                case Tile.TKind.ActiveRedstone:
                    return isRedReachable(x, y - 1, checkedNodes) ||
                    isRedReachable(x - 1, y, checkedNodes) ||
                    isRedReachable(x, y + 1, checkedNodes) ||
                    isRedReachable(x + 1, y, checkedNodes);
                default:
                    return false;
            }
        }

        bool IsRedActive(ref Tile tile)
        {
            switch (tile.Kind)
            {
                case Tile.TKind.RedstoneBlock:
                case Tile.TKind.ActiveRedstone:
                    return true;

                default:
                    return false;
            }
        }

        public class TPlayer
        {
            public Vector2f Position;
            public Vector2f Velocity;
            public byte AnimationStep { get; private set; }
            public bool SpriteDirection; // false is left, true is right
            public THotbar Hotbar;

            public TPlayer()
            {
                Hotbar.Tiles = new Tile.TKind[10];
                Hotbar.Tiles[0] = Tile.TKind.Planks;
                Hotbar.Tiles[1] = Tile.TKind.Stone;
                Hotbar.Tiles[2] = Tile.TKind.Iron;
                Hotbar.Tiles[3] = Tile.TKind.RedstoneBlock;
                Hotbar.Tiles[4] = Tile.TKind.InactiveRedstone;
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
            InactiveRedstone,
            ActiveRedstone,
        }
    }
}
