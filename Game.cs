using SFML.System;
namespace rotstein
{
    class Game
    {
        private static readonly int TEXTURE_SIZE = 16;

        public TPlayer Player { get; } = new TPlayer();
        public Tile[,] Tiles { get; private set; }
        private Tile[,] NextTiles { get; set; }

        private event System.EventHandler NextTickEvent;

        private bool[,] Prealloc_RedCheckedNodes;

        public Game(uint playground_size)
        {
            Tiles = new Tile[playground_size, playground_size];
            NextTiles = new Tile[playground_size, playground_size];
            Prealloc_RedCheckedNodes = new bool[Tiles.GetLength(0), Tiles.GetLength(1)];

            for (int i = 0; i < playground_size; i++)
            {
                Tiles[0, i] = new Tile(Tile.TKind.Iron);
                Tiles[playground_size - 1, i] = new Tile(Tile.TKind.Iron);
            }
            for (int i = 1; i < playground_size - 1; i++)
            {
                Tiles[i, 0] = new Tile(Tile.TKind.Iron);
                Tiles[i, playground_size - 1] = new Tile(Tile.TKind.Iron);
            }
            Tiles[playground_size / 2, playground_size / 2] = new Tile(Tile.TKind.RedstoneBlock);

            Player.Position = new Vector2f(playground_size / 2 * TEXTURE_SIZE, playground_size / 2 * TEXTURE_SIZE);
        }

        public void PlaceTile(Vector2u v, Tile tile)
        {
            (uint x, uint y) = (v.X, v.Y);

            Tiles[x, y] = tile;
            UpdateTiles();
        }

        public void ActivateTile(Vector2u v)
        {
            (uint x, uint y) = (v.X, v.Y);

            if (Tiles[x, y].Kind == Tile.TKind.Lever)
                Tiles[x, y].Activity = !Tiles[x, y].Activity;
            UpdateTiles();
        }

        private void UpdateTiles()
        {
            System.Array.Copy(Tiles, NextTiles, Tiles.Length);
            for (uint i = 0; i < Tiles.GetLength(0); i++)
            {
                for (uint j = 0; j < Tiles.GetLength(1); j++)
                {
                    UpdateTile(new Vector2u(i, j));
                }
            }
            System.Array.Copy(NextTiles, Tiles, Tiles.Length);
        }

        private void UpdateTile(Vector2u v)
        {
            (uint x, uint y) = (v.X, v.Y);

            System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it

            switch (Tiles[x, y].Kind)
            {
                case Tile.TKind.RedstoneWire:
                    System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it
                    NextTiles[x, y].Activity = IsRedActive(v, Tile.TDirection.NA);

                    NextTiles[x, y].Variant = (uint)
                        (((IsRedConnected(Tiles[x, y - 1], Tile.TDirection.South) ? 1 : 0) << 0) |
                        ((IsRedConnected(Tiles[x + 1, y], Tile.TDirection.West) ? 1 : 0) << 1) |
                        ((IsRedConnected(Tiles[x, y + 1], Tile.TDirection.North) ? 1 : 0) << 2) |
                        ((IsRedConnected(Tiles[x - 1, y], Tile.TDirection.East) ? 1 : 0) << 3));
                    break;
                case Tile.TKind.RedstoneBridge:
                    System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it
                    bool activity_N = IsRedActive(new Vector2u(x, y - 1), Tile.TDirection.South);
                    System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it
                    bool activity_S = IsRedActive(new Vector2u(x, y + 1), Tile.TDirection.North);
                    System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it
                    bool activity_W = IsRedActive(new Vector2u(x - 1, y), Tile.TDirection.East);
                    System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it
                    bool activity_E = IsRedActive(new Vector2u(x + 1, y), Tile.TDirection.West);
                    NextTiles[x, y].Variant = (uint)
                        (uint)(((activity_N || activity_S ? 1 : 0) << 0) |
                        (activity_W || activity_E ? 1 : 0) << 1);
                    break;
                case Tile.TKind.NotGate:
                case Tile.TKind.Repeater:
                case Tile.TKind.OrGate:
                case Tile.TKind.AndGate:
                case Tile.TKind.XorGate:
                    // Gates
                    switch (Tiles[x, y].Kind)
                    {
                        case Tile.TKind.NotGate:
                        case Tile.TKind.Repeater:
                            // Unary gate
                            Vector2u input = Tile.PickTileInDirection(v, Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.South));
                            bool input_activity = IsRedActive(input, Tiles[x, y].Direction);
                            switch (Tiles[x, y].Kind)
                            {
                                case Tile.TKind.NotGate:
                                    System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it
                                    NextTiles[x, y].Activity = !input_activity;
                                    break;
                                case Tile.TKind.Repeater:
                                    System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it
                                    if (input_activity && Tiles[x, y].Variant == 0)
                                    {
                                        Tiles[x, y].Variant = 1;

                                        // NOTE: following code always allocates memory in heap even if never executed
                                        System.EventHandler action = null;
                                        action = (_, __) =>
                                        {
                                            if (Tiles[x, y].Variant == 4)
                                            {
                                                Tiles[x, y].Variant = 0;
                                                return;
                                            }
                                            Tiles[x, y].Variant += 1;
                                            NextTickEvent += action;
                                        };
                                        NextTickEvent += action;
                                    }
                                    break;
                            }
                            break;
                        case Tile.TKind.OrGate:
                        case Tile.TKind.AndGate:
                        case Tile.TKind.XorGate:
                            // Binary gate
                            Vector2u input_left = Tile.PickTileInDirection(v, Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.West));
                            System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it
                            bool left = IsRedActive(input_left, Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.East));
                            Vector2u input_right = Tile.PickTileInDirection(v, Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.East));
                            System.Array.Clear(Prealloc_RedCheckedNodes, 0, Prealloc_RedCheckedNodes.Length); // Clear preallocated array before using it
                            bool right = IsRedActive(input_right, Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.West));
                            switch (Tiles[x, y].Kind)
                            {
                                case Tile.TKind.OrGate:
                                    NextTiles[x, y].Activity = left | right;
                                    break;
                                case Tile.TKind.AndGate:
                                    NextTiles[x, y].Activity = left & right;
                                    break;
                                case Tile.TKind.XorGate:
                                    NextTiles[x, y].Activity = left ^ right;
                                    break;
                            }
                            break;
                    }
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

            UpdateTiles();
        }

        public void ExecuteCommand(string command)
        {
            string[] words = command.Split();
            if (words[0] == "world")
            {
                if (words[1] == "save")
                {
                    // TODO: print success message in chat
                    SerializePlayerAndMap("saves/" + words[2] + ".rts");
                }
                else if (words[1] == "load")
                {
                    // TODO: print success message in chat
                    DeserializePlayerAndMap("saves/" + words[2] + ".rts");
                }
            }
        }

        private void SerializePlayerAndMap(string file_name)
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

        private void DeserializePlayerAndMap(string file_name)
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
                    NextTiles = new Tile[tiles_size.X, tiles_size.Y];
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

        private bool isRedReachable(Vector2u v, Tile.TDirection direction)
        {
            (uint x, uint y) = (v.X, v.Y);
            if (Prealloc_RedCheckedNodes[x, y])
                return false;

            if (Tiles[x, y].Kind == Tile.TKind.RedstoneWire)
                Prealloc_RedCheckedNodes[x, y] = true;

            switch (Tiles[x, y].Kind)
            {
                case Tile.TKind.RedstoneWire:
                    return isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.North), Tile.TDirection.South) ||
                    isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.West), Tile.TDirection.East) ||
                    isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.South), Tile.TDirection.North) ||
                    isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.East), Tile.TDirection.West);
                case Tile.TKind.RedstoneBridge:
                    switch (direction)
                    {
                        case Tile.TDirection.North:
                            return isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.South), Tile.TDirection.North);
                        case Tile.TDirection.South:
                            return isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.North), Tile.TDirection.South);
                        case Tile.TDirection.East:
                            return isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.West), Tile.TDirection.East);
                        case Tile.TDirection.West:
                            return isRedReachable(Tile.PickTileInDirection(v, Tile.TDirection.East), Tile.TDirection.West);
                        default:
                            throw new System.ArgumentException("Direction was NA", "direction");
                    }
                default:
                    return IsRedActive(v, direction);
            }
        }

        /// Is this tile active in direction "direction"?
        private bool IsRedActive(Vector2u v, Tile.TDirection direction)
        {
            (uint x, uint y) = (v.X, v.Y);
            switch (Tiles[x, y].Kind)
            {
                case Tile.TKind.RedstoneBlock:
                    return true;
                case Tile.TKind.RedstoneWire:
                case Tile.TKind.RedstoneBridge:
                    return isRedReachable(v, direction);
                case Tile.TKind.NotGate:
                case Tile.TKind.OrGate:
                case Tile.TKind.AndGate:
                case Tile.TKind.XorGate:
                    return (Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.North) == direction) && Tiles[x, y].Activity;
                case Tile.TKind.Repeater:
                    return (Tile.TDirectionAdd(Tiles[x, y].Direction, Tile.TDirection.North) == direction) && Tiles[x, y].Variant == 4;
                case Tile.TKind.Lever:
                    return Tiles[x, y].Activity;

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
                case Tile.TKind.RedstoneBridge:
                case Tile.TKind.Lever:
                    return true;
                case Tile.TKind.NotGate:
                case Tile.TKind.Repeater:
                    return (Tile.TDirectionAdd(tile.Direction, Tile.TDirection.North) == direction) ||
                        (Tile.TDirectionAdd(tile.Direction, Tile.TDirection.South) == direction);
                case Tile.TKind.OrGate:
                case Tile.TKind.AndGate:
                case Tile.TKind.XorGate:
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
                Hotbar.Tiles = new Tile[12];
                Hotbar.Tiles[0] = new Tile(Tile.TKind.Planks);
                Hotbar.Tiles[1] = new Tile(Tile.TKind.Stone);
                Hotbar.Tiles[2] = new Tile(Tile.TKind.Iron);
                Hotbar.Tiles[3] = new Tile(Tile.TKind.RedstoneBlock);
                Hotbar.Tiles[4] = new Tile(Tile.TKind.RedstoneWire);
                Hotbar.Tiles[5] = new Tile(Tile.TKind.RedstoneBridge);
                Hotbar.Tiles[6] = new Tile(Tile.TKind.NotGate);
                Hotbar.Tiles[7] = new Tile(Tile.TKind.OrGate);
                Hotbar.Tiles[8] = new Tile(Tile.TKind.AndGate);
                Hotbar.Tiles[9] = new Tile(Tile.TKind.XorGate);
                Hotbar.Tiles[10] = new Tile(Tile.TKind.Repeater);
                Hotbar.Tiles[11] = new Tile(Tile.TKind.Lever);
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
            RedstoneBridge,
            NotGate,
            OrGate,
            AndGate,
            XorGate,
            Repeater,
            Lever,
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
