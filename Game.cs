using SFML.System;
namespace rotstein
{
    class Game
    {
        public TPlayer Player { get; } = new TPlayer();
        public Tile[,] Tiles { get; private set; }

        public event System.EventHandler NextTickEvent;

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

            var old_tile = Tiles[x, y];
            Tiles[x, y] = tile;

            System.Array.Clear(Prealloc_UpToDateNodes, 0, Prealloc_UpToDateNodes.Length); // Clear preallocated array before using it

            MaybeUpdateSelfTile(v, tile); // Update self if new tile is dynamic
            MaybeUpdateNeighborTiles(v, old_tile); // Update neighbors if old tile was important
            MaybeUpdateNeighborTiles(v, tile); // Update neighbors if new tile is important
        }

        void MaybeUpdateSelfTile(Vector2u v, Tile tile)
        {
            switch (tile.Kind)
            {
                case Tile.TKind.RedstoneWire:
                case Tile.TKind.NotGate:
                case Tile.TKind.OrGate:
                case Tile.TKind.AndGate:
                    UpdateTile(v);
                    break;
            }
        }

        void MaybeUpdateNeighborTiles(Vector2u v, Tile tile)
        {
            switch (tile.Kind)
            {
                case Tile.TKind.RedstoneWire:
                case Tile.TKind.RedstoneBlock:
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.North));
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.East));
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.South));
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirection.West));
                    break;
                case Tile.TKind.NotGate:
                    // Unary gate
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirectionAdd(tile.Direction, Tile.TDirection.South)));
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirectionAdd(tile.Direction, Tile.TDirection.North)));
                    break;
                case Tile.TKind.OrGate:
                case Tile.TKind.AndGate:
                    // Binary gate
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirectionAdd(tile.Direction, Tile.TDirection.East)));
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirectionAdd(tile.Direction, Tile.TDirection.West)));
                    UpdateTile(Tile.PickTileInDirection(v, Tile.TDirectionAdd(tile.Direction, Tile.TDirection.North)));
                    break;
            }
        }

        void UpdateTile(Vector2u v)
        {
            (uint x, uint y) = (v.X, v.Y);

            if (Prealloc_UpToDateNodes[x, y])
                return;

            Prealloc_UpToDateNodes[x, y] = true;

            System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it

            bool old_activity;
            bool new_activity = false;
            switch (Tiles[x, y].Kind)
            {
                case Tile.TKind.RedstoneWire:
                    old_activity = Tiles[x, y].Activity;
                    new_activity = isRedReachable(v, Tile.TDirection.NA);
                    Tiles[x, y].Activity = new_activity;

                    Tiles[x, y].Variant = (uint)
                        (((IsRedConnected(Tiles[x, y - 1], Tile.TDirection.South) ? 1 : 0) << 0) |
                        ((IsRedConnected(Tiles[x + 1, y], Tile.TDirection.West) ? 1 : 0) << 1) |
                        ((IsRedConnected(Tiles[x, y + 1], Tile.TDirection.North) ? 1 : 0) << 2) |
                        ((IsRedConnected(Tiles[x - 1, y], Tile.TDirection.East) ? 1 : 0) << 3));

                    if (old_activity != new_activity)
                    {
                        MaybeUpdateNeighborTiles(v, Tiles[x, y]);
                    }
                    break;
                case Tile.TKind.NotGate:
                case Tile.TKind.OrGate:
                case Tile.TKind.AndGate:
                    // Gates
                    old_activity = Tiles[x, y].Activity;
                    {
                        switch (Tiles[x, y].Kind)
                        {
                            case Tile.TKind.NotGate:
                                // Unary gate
                                Vector2u input = Tile.PickTileInDirection(v, Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.South));
                                new_activity = !IsRedActive(ref Tiles[input.X, input.Y], Tiles[x, y].Direction);
                                break;
                            case Tile.TKind.OrGate:
                            case Tile.TKind.AndGate:
                                // Binary gate
                                Vector2u input_left = Tile.PickTileInDirection(v, Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.West));
                                Vector2u input_right = Tile.PickTileInDirection(v, Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.East));
                                switch (Tiles[x, y].Kind)
                                {
                                    case Tile.TKind.OrGate:
                                        new_activity =
                                            IsRedActive(ref Tiles[input_left.X, input_left.Y], Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.East)) ||
                                            IsRedActive(ref Tiles[input_right.X, input_right.Y], Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.West))
                                            ;
                                        break;
                                    case Tile.TKind.AndGate:
                                        new_activity =
                                            IsRedActive(ref Tiles[input_left.X, input_left.Y], Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.East)) &&
                                            IsRedActive(ref Tiles[input_right.X, input_right.Y], Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.West))
                                            ;
                                        break;
                                }
                                break;
                        }
                    }

                    NextTickEvent += (_, __) =>
                    {
                        if (old_activity != new_activity)
                        {
                            Tiles[x, y].Activity = new_activity;
                            System.Array.Clear(Prealloc_UpToDateNodes, 0, Prealloc_UpToDateNodes.Length); // Clear preallocated array before using it
                            MaybeUpdateNeighborTiles(v, Tiles[x, y]);
                        }
                    };
                    break;
            }
        }

        public void TickOnce()
        {
            if (NextTickEvent != null)
            {
                // Shift events and imvoke.
                // This is needed because event invokation can add new event handlers
                // and they should register to a different event queue.
                var NextTickEvent_tmp = NextTickEvent;
                NextTickEvent = null;
                NextTickEvent_tmp.Invoke(null, null);
            }
        }

        public void ExecuteCommand(string command)
        {
            string[] words = command.Split();
            if (words[0] == "world")
            {
                if (words[1] == "save")
                {
                    // TODO: print success message in chat
                    SerializePlayerAndMap(words[2]);
                }
                else if (words[1] == "load")
                {
                    // TODO: print success message in chat
                    DeserializePlayerAndMap(words[2]);
                }
            }
        }

        void SerializePlayerAndMap(string file_name)
        {
            using (var file = new System.IO.BinaryWriter(System.IO.File.Create(file_name)))
            {
                file.Write(1); // FormatVersion
                file.Write(Player.Position.X);
                file.Write(Player.Position.Y);
                file.Write(Tiles.GetLength(0));
                file.Write(Tiles.GetLength(1));
                for (int i = 0; i < Tiles.GetLength(0); i++)
                {
                    for (int j = 0; j < Tiles.GetLength(1); j++)
                    {
                        var tile = Tiles[i, j];
                        file.Write((byte)tile.Kind);
                        file.Write((byte)(tile.Activity ? 1 : 0));
                        file.Write((byte)tile.Variant);
                        file.Write((byte)tile.Direction);
                    }
                }
            }
        }

        void DeserializePlayerAndMap(string file_name)
        {
            using (var file = new System.IO.BinaryReader(System.IO.File.OpenRead(file_name)))
            {
                var format_version = file.ReadInt32();

                if (format_version == 1)
                {
                    // Format is binary. All variables are ints by default.
                    //
                    // Layout:
                    // FormatVersion
                    // (float)Player.Position.X (float)Player.Position.Y
                    // Tiles.GetLength(0) Tiles.GetLength(1)
                    // <tiles,format:
                    //   (byte)this.Kind
                    //   (byte)this.Activity
                    //   (byte)this.Variant
                    //   (byte)this.Direction

                    Player.Position.X = file.ReadSingle();
                    Player.Position.Y = file.ReadSingle();
                    var tiles_size = new Vector2i(file.ReadInt32(), file.ReadInt32());
                    Tiles = new Tile[tiles_size.X, tiles_size.Y];
                    for (int i = 0; i < tiles_size.X; i++)
                    {
                        for (int j = 0; j < tiles_size.Y; j++)
                        {
                            Tiles[i, j].Kind = (Tile.TKind)file.ReadByte();
                            Tiles[i, j].Activity = file.ReadByte() == 1 ? true : false;
                            Tiles[i, j].Variant = (uint)file.ReadByte();
                            Tiles[i, j].Direction = (Tile.TDirection)file.ReadByte();
                        }
                    }
                }
            }
        }

        bool isRedReachable(Vector2u v, Tile.TDirection direction)
        {
            (uint x, uint y) = (v.X, v.Y);
            if (Prealloc_RedCheckedNodes[x, y])
                return false;

            Prealloc_RedCheckedNodes[x, y] = true;

            switch (Tiles[x, y].Kind)
            {
                case Tile.TKind.RedstoneWire:
                    return isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.North), Tile.TDirection.South) ||
                    isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.West), Tile.TDirection.East) ||
                    isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.South), Tile.TDirection.North) ||
                    isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.East), Tile.TDirection.West);
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
                    return tile.Activity;
                case Tile.TKind.NotGate:
                case Tile.TKind.OrGate:
                case Tile.TKind.AndGate:
                    return (Tile.TDirectionAdd(tile.Direction, Tile.TDirection.North) == direction) && tile.Activity;

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
        public bool Activity;
        public uint Variant;
        public TDirection Direction;

        public Tile(Tile.TKind kind)
        {
            this.Kind = kind;
            this.Activity = false;
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
