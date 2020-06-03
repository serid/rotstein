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

        public void PlaceTile(Vector2u v, Tile tile)
        {
            (uint x, uint y) = (v.X, v.Y);

            var oldTileKind = Tiles[x, y].Kind;
            Tiles[x, y] = tile;

            System.Array.Clear(Prealloc_UpToDateNodes, 0, Prealloc_UpToDateNodes.Length); // Clear preallocated array before using it

            switch (oldTileKind)
            { // Update neighbors if old tile was important
                case Tile.TKind.RedstoneWire:
                case Tile.TKind.RedstoneBlock:
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.North), Prealloc_UpToDateNodes);
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.East), Prealloc_UpToDateNodes);
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.South), Prealloc_UpToDateNodes);
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.West), Prealloc_UpToDateNodes);
                    break;
                case Tile.TKind.NotGate:
                case Tile.TKind.OrGate:
                case Tile.TKind.AndGate:
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirectionAdd(tile.Direction, Tile.TDirection.North)), Prealloc_UpToDateNodes);
                    break;
            }

            switch (tile.Kind)
            { // Update self if new tile is dynamic
              // Update neighbors if new tile is important
                case Tile.TKind.RedstoneWire:
                    UpdateTile(v, Prealloc_UpToDateNodes);
                    break;
                case Tile.TKind.RedstoneBlock:
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.North), Prealloc_UpToDateNodes);
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.East), Prealloc_UpToDateNodes);
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.South), Prealloc_UpToDateNodes);
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.West), Prealloc_UpToDateNodes);
                    break;
                case Tile.TKind.NotGate:
                case Tile.TKind.OrGate:
                case Tile.TKind.AndGate:
                    UpdateTile(v, Prealloc_UpToDateNodes);
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirectionAdd(tile.Direction, Tile.TDirection.North)), Prealloc_UpToDateNodes);
                    break;
            }
        }

        void UpdateTile(Vector2u v, bool[,] upToDateNodes)
        {
            (uint x, uint y) = (v.X, v.Y);

            if (upToDateNodes[x, y])
                return;

            upToDateNodes[x, y] = true;

            System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it

            uint old_activity;
            uint new_activity = 0;
            switch (Tiles[x, y].Kind)
            {
                case Tile.TKind.RedstoneWire:
                    old_activity = Tiles[x, y].Variant;
                    new_activity = Tile.BoolToActivity(isRedReachable(v, Tile.TDirection.NA, Prealloc_RedCheckedNodes));
                    Tiles[x, y].Variant = new_activity;

                    if (old_activity != new_activity)
                    {
                        UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.North), upToDateNodes);
                        UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.East), upToDateNodes);
                        UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.South), upToDateNodes);
                        UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.West), upToDateNodes);
                    }
                    break;
                case Tile.TKind.NotGate:
                case Tile.TKind.OrGate:
                case Tile.TKind.AndGate:
                    // Gates
                    old_activity = Tiles[x, y].Variant;
                    {
                        switch (Tiles[x, y].Kind)
                        {
                            case Tile.TKind.NotGate:
                                // Unary gate
                                Vector2u input = Tile.PickTileInDirection(v, Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.South));
                                new_activity = Tile.BoolToActivity(!IsRedActive(ref Tiles[input.X, input.Y], Tiles[x, y].Direction));
                                break;
                            case Tile.TKind.OrGate:
                            case Tile.TKind.AndGate:
                                // Binary gate
                                Vector2u input_left = Tile.PickTileInDirection(v, Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.West));
                                Vector2u input_right = Tile.PickTileInDirection(v, Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.East));
                                switch (Tiles[x, y].Kind)
                                {
                                    case Tile.TKind.OrGate:
                                        new_activity = Tile.BoolToActivity(
                                            IsRedActive(ref Tiles[input_left.X, input_left.Y], Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.East)) ||
                                            IsRedActive(ref Tiles[input_right.X, input_right.Y], Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.West))
                                            );
                                        break;
                                    case Tile.TKind.AndGate:
                                        new_activity = Tile.BoolToActivity(
                                            IsRedActive(ref Tiles[input_left.X, input_left.Y], Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.East)) &&
                                            IsRedActive(ref Tiles[input_right.X, input_right.Y], Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.West))
                                            );
                                        break;
                                }
                                break;
                        }
                    }
                    Tiles[x, y].Variant = new_activity;

                    if (old_activity != new_activity)
                    {
                        UpdateTile(Tile.PickTileInDirection(v, Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.North)), upToDateNodes);
                    }
                    break;
            }
        }

        bool isRedReachable(Vector2u v, Tile.TDirection direction, bool[,] checkedNodes)
        {
            (uint x, uint y) = (v.X, v.Y);
            if (checkedNodes[x, y])
                return false;

            checkedNodes[x, y] = true;

            switch (Tiles[x, y].Kind)
            {
                case Tile.TKind.RedstoneWire:
                    return isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.North), Tile.TDirection.South, checkedNodes) ||
                    isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.West), Tile.TDirection.East, checkedNodes) ||
                    isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.South), Tile.TDirection.North, checkedNodes) ||
                    isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.East), Tile.TDirection.West, checkedNodes);
                default:
                    return IsRedActive(ref Tiles[x, y], direction);
            }
        }

        /// Is this tile active in direction "direction"?
        static bool IsRedActive(ref Tile tile, Tile.TDirection direction)
        {
            switch (tile.Kind)
            {
                case Tile.TKind.RedstoneBlock:
                    return true;
                case Tile.TKind.RedstoneWire:
                    return (tile.Variant == 1);
                case Tile.TKind.NotGate:
                case Tile.TKind.OrGate:
                case Tile.TKind.AndGate:
                    return (Tile.TDirectionAdd(tile.Direction, Tile.TDirection.North) == direction) && (tile.Variant == 1);

                default:
                    return false;
            }
        }

        /// Does this tile connect in direction "direction"?
        public static bool IsRedConnected(Tile tile, Tile.TDirection direction)
        {
            switch (tile.Kind)
            {
                case Tile.TKind.RedstoneBlock:
                case Tile.TKind.RedstoneWire:
                    return true;
                case Tile.TKind.NotGate:
                    return (Tile.TDirectionAdd(tile.Direction, Tile.TDirection.North) == direction) ||
                        (Tile.TDirectionAdd(tile.Direction, Tile.TDirection.South) == direction);
                case Tile.TKind.OrGate:
                case Tile.TKind.AndGate:
                    return (Tile.TDirectionAdd(tile.Direction, Tile.TDirection.North) == direction) ||
                        (Tile.TDirectionAdd(tile.Direction, Tile.TDirection.East) == direction) ||
                        (Tile.TDirectionAdd(tile.Direction, Tile.TDirection.West) == direction);

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

        /// Adds two TDirection values.
        public static TDirection TDirectionAdd(TDirection d1, TDirection d2)
        {
            if (d1 == TDirection.NA)
            {
                throw new System.ArgumentException("Direction 1 was NA", "d1");
            }
            if (d2 == TDirection.NA)
            {
                throw new System.ArgumentException("Direction 2 was NA", "d2");
            }
            return (TDirection)(((int)d1 + (int)d2) % 4);
        }

        public static Vector2u PickTileInDirection(Vector2u tile_coords, TDirection direction)
        {
            switch (direction)
            {
                case TDirection.North:
                    return new Vector2u(tile_coords.X, tile_coords.Y - 1);
                case TDirection.East:
                    return new Vector2u(tile_coords.X + 1, tile_coords.Y);
                case TDirection.South:
                    return new Vector2u(tile_coords.X, tile_coords.Y + 1);
                case TDirection.West:
                    return new Vector2u(tile_coords.X - 1, tile_coords.Y);
                default:
                    throw new System.ArgumentException("Direction was NA", "direction");
            }
        }

        public static uint BoolToActivity(bool b)
        {
            return b ? (uint)1 : (uint)0;
        }
    }
}
