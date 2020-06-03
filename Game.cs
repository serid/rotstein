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
            { // Update neighbors if old tile was important
                case Tile.TKind.RedstoneWire:
                case Tile.TKind.RedstoneBlock:
                    UpdateTile(x, y - 1, Prealloc_UpToDateNodes);
                    UpdateTile(x - 1, y, Prealloc_UpToDateNodes);
                    UpdateTile(x, y + 1, Prealloc_UpToDateNodes);
                    UpdateTile(x + 1, y, Prealloc_UpToDateNodes);
                    break;
                case Tile.TKind.NotGate:
                    UpdateTile(x, y - 1, Prealloc_UpToDateNodes); // TODO: implement rotation
                    break;
            }

            switch (tile.Kind)
            { // Update self if new tile is dynamic
              // Update neighbors if new tile is important
                case Tile.TKind.RedstoneWire:
                    UpdateTile(x, y, Prealloc_UpToDateNodes);
                    break;
                case Tile.TKind.RedstoneBlock:
                    UpdateTile(x, y - 1, Prealloc_UpToDateNodes);
                    UpdateTile(x - 1, y, Prealloc_UpToDateNodes);
                    UpdateTile(x, y + 1, Prealloc_UpToDateNodes);
                    UpdateTile(x + 1, y, Prealloc_UpToDateNodes);
                    break;
                case Tile.TKind.NotGate:
                    UpdateTile(x, y, Prealloc_UpToDateNodes);
                    UpdateTile(x, y - 1, Prealloc_UpToDateNodes); // TODO: implement rotation
                    break;
            }
        }

        void UpdateTile(uint x, uint y, bool[,] upToDateNodes)
        {
            if (upToDateNodes[x, y])
                return;

            upToDateNodes[x, y] = true;

            System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it

            uint old_activity;
            uint new_activity;
            switch (Tiles[x, y].Kind)
            {
                case Tile.TKind.RedstoneWire:
                    old_activity = Tiles[x, y].Variant;
                    new_activity = isRedReachable(x, y, Tile.TDirection.NA, Prealloc_RedCheckedNodes) ? (uint)1 : (uint)0;
                    Tiles[x, y].Variant = new_activity;

                    if (old_activity != new_activity)
                    {
                        UpdateTile(x, y - 1, upToDateNodes);
                        UpdateTile(x - 1, y, upToDateNodes);
                        UpdateTile(x, y + 1, upToDateNodes);
                        UpdateTile(x + 1, y, upToDateNodes);
                    }
                    break;
                case Tile.TKind.NotGate:
                    old_activity = Tiles[x, y].Variant;
                    new_activity = (!IsRedActive(ref Tiles[x, y + 1], 0)) ? (uint)1 : (uint)0; // TODO: implement rotation
                    Tiles[x, y].Variant = new_activity;

                    if (old_activity != new_activity)
                    {
                        UpdateTile(x, y - 1, upToDateNodes); // TODO: implement rotation
                    }
                    break;
            }
        }

        bool isRedReachable(uint x, uint y, Tile.TDirection direction, bool[,] checkedNodes)
        {
            if (checkedNodes[x, y])
                return false;

            checkedNodes[x, y] = true;

            switch (Tiles[x, y].Kind)
            {
                case Tile.TKind.RedstoneWire:
                    return isRedReachable(x, y - 1, Tile.TDirection.South, checkedNodes) ||
                    isRedReachable(x - 1, y, Tile.TDirection.East, checkedNodes) ||
                    isRedReachable(x, y + 1, Tile.TDirection.North, checkedNodes) ||
                    isRedReachable(x + 1, y, Tile.TDirection.West, checkedNodes);
                default:
                    return IsRedActive(ref Tiles[x, y], direction);
            }
        }

        bool IsRedActive(ref Tile tile, Tile.TDirection direction)
        {
            switch (tile.Kind)
            {
                case Tile.TKind.RedstoneBlock:
                    return true;
                case Tile.TKind.RedstoneWire:
                    return (tile.Variant == 1);
                case Tile.TKind.NotGate:
                    return (tile.Direction == direction) && (tile.Variant == 1);

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
                Hotbar.Tiles = new Tile[10];
                Hotbar.Tiles[0] = new Tile(Tile.TKind.Planks);
                Hotbar.Tiles[1] = new Tile(Tile.TKind.Stone);
                Hotbar.Tiles[2] = new Tile(Tile.TKind.Iron);
                Hotbar.Tiles[3] = new Tile(Tile.TKind.RedstoneBlock);
                Hotbar.Tiles[4] = new Tile(Tile.TKind.RedstoneWire);
                Hotbar.Tiles[5] = new Tile(Tile.TKind.NotGate);
                Hotbar.Tiles[6] = new Tile(Tile.TKind.OrGate);
                Hotbar.Tiles[7] = new Tile(Tile.TKind.AndGate);
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
                public Tile[] Tiles;
                public uint Index; // Which item from hotbar is picked

                public Tile IndexTile
                {
                    get { return Tiles[Index]; }
                    set { Tiles[Index] = value; }
                }
            }
        }
    }

    struct Tile
    {
        public Tile.TKind Kind;
        public uint Variant;
        public TDirection Direction;

        public Tile(Tile.TKind kind)
        {
            this.Kind = kind;
            this.Variant = 0;
            this.Direction = TDirection.North;
        }

        public enum TKind
        {
            Void,
            Planks,
            Stone,
            Iron,
            RedstoneBlock,
            RedstoneWire,
            NotGate,
            OrGate,
            AndGate,
        }

        public enum TDirection
        {
            North,
            East,
            South,
            West,
            NA,
        }

        public float RotationDegree()
        {
            return ((int)Direction) * 90f;
        }

        /// Rotates tile right "turns" times. "turns" can be negative.
        public static TDirection TDirectionRotate(TDirection direction, int turns)
        {
            if (direction == TDirection.NA)
            {
                throw new System.ArgumentException("Direction was NA", "direction");
            }
            return (TDirection)(((int)direction + turns) % 4);
        }
    }
}
