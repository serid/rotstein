using System.Collections.Generic;
using System.Linq;

using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace rotstein
{
    class Drawer
    {
        private static readonly uint PLAYGROUND_SIZE = 50;
        private static readonly int TEXTURE_SIZE = 16;
        private static readonly float TICK_LENGTH = 0.100f; // In seconds

        private float Scale = 6; // Game scale
        private Vector2u WindowSize = new Vector2u(1600, 900); // TODO: change type to Vector2f and remove `* SCALE` everywhere
        private Texture Atlas;
        private RenderWindow Window;
        private Clock PhysicsClock;
        private Clock TicksClock;
        private TInputState InputState = TInputState.None;
        private string CommandBox = "";
        private string LastCommand = "";
        private List<string> ChatLog = new List<string>();
        private Font BasicFont = new Font("SourceCodePro-Regular.otf");
        // private Font BasicFont = new Font("Cantarell-Regular.otf");

        private Sprite Prealloc_Sprite;
        private Text Prealloc_Text;
        private RectangleShape Prealloc_RectangleShape;
        private Vertex[] Prealloc_Vertex_2;

        private Game Game;

        private long GcUsedMemory;

        public Drawer()
        {
            Atlas = new Texture("rsc/atlas.png");
            Prealloc_Sprite = new Sprite(Atlas);
            Prealloc_Text = new Text("", BasicFont, 60);
            Prealloc_Text.Scale = new Vector2f(0.14f, 0.14f);
            Prealloc_Text.OutlineColor = new Color(0, 0, 0);
            Prealloc_RectangleShape = new RectangleShape();
            Prealloc_Vertex_2 = new Vertex[2];

            Window = new RenderWindow(new VideoMode(WindowSize.X, WindowSize.Y), "Rotstein",
                Styles.Titlebar | Styles.Close | Styles.Resize);
            Window.Closed += (_, __) => Window.Close();

            Window.Resized += (_, args) =>
            {
                WindowSize = new Vector2u(args.Width, args.Height);
                View view = Window.GetView();
                view.Size = new Vector2f(WindowSize.X / Scale, WindowSize.Y / Scale);
                Window.SetView(view);
            };

            Window.KeyPressed += HandleKeyPress;
            Window.MouseWheelScrolled += MouseWheelScrolled;
            Window.KeyReleased += HandleKeyRelease;
            Window.TextEntered += HandleTextEnter;
            Window.MouseButtonPressed += HandleMouseButtonPress;
            Window.SetKeyRepeatEnabled(true);
            Window.SetVerticalSyncEnabled(true);

            Game = new Game(PLAYGROUND_SIZE);

            Window.SetView(new View(new Vector2f(Game.Player.Position.X + TEXTURE_SIZE / 2, Game.Player.Position.Y + 2 * TEXTURE_SIZE / 2), // Player center
            new Vector2f(WindowSize.X / Scale, WindowSize.Y / Scale)));
        }

        public void Loop()
        {
            using (PhysicsClock = new Clock())
            using (TicksClock = new Clock())
            {
                while (Window.IsOpen)
                {
                    Window.DispatchEvents();

                    ClockHandlePhysics();
                    ClockHandleTicks();

                    Window.Clear(new Color(50, 100, 0));
                    // Grid
                    for (int i = 0; i < PLAYGROUND_SIZE; i++)
                    {
                        Color grid_color = new Color(150, 150, 150);
                        Prealloc_Vertex_2[0] = new Vertex(new Vector2f(1 * TEXTURE_SIZE, i * TEXTURE_SIZE), grid_color);
                        Prealloc_Vertex_2[1] = new Vertex(new Vector2f(PLAYGROUND_SIZE * TEXTURE_SIZE, i * TEXTURE_SIZE), grid_color);
                        Window.Draw(Prealloc_Vertex_2, PrimitiveType.Lines);
                        Prealloc_Vertex_2[0] = new Vertex(new Vector2f(i * TEXTURE_SIZE, 1 * TEXTURE_SIZE), grid_color);
                        Prealloc_Vertex_2[1] = new Vertex(new Vector2f(i * TEXTURE_SIZE, PLAYGROUND_SIZE * TEXTURE_SIZE), grid_color);
                        Window.Draw(Prealloc_Vertex_2, PrimitiveType.Lines);
                    }
                    for (int i = 0; i < PLAYGROUND_SIZE; i++)
                    {
                        for (int j = 0; j < PLAYGROUND_SIZE; j++)
                        {
                            DrawTile(new Vector2f(i * TEXTURE_SIZE, j * TEXTURE_SIZE), Game.Tiles[i, j]);
                        }
                    }
                    DrawPlayer();
                    DrawLabels();
                    DrawGui();

                    Window.Display();
                }
            }
        }

        private void DrawTile(Vector2f pos, Tile tile)
        {
            Prealloc_Sprite.Rotation = 0;
            switch (tile.Kind)
            {
                case Tile.TKind.Void:
                    return;

                case Tile.TKind.RedstoneWire:
                    Prealloc_Sprite.TextureRect = new IntRect(
                        ((int)tile.Variant) * TEXTURE_SIZE,
                        (4 + (tile.Activity ? 1 : 0)) * TEXTURE_SIZE,
                        TEXTURE_SIZE, TEXTURE_SIZE);
                    break;
                case Tile.TKind.RedstoneBridge:
                    Prealloc_Sprite.TextureRect = new IntRect(
                        ((int)tile.Variant) * TEXTURE_SIZE,
                        6 * TEXTURE_SIZE,
                        TEXTURE_SIZE, TEXTURE_SIZE);
                    break;
                case Tile.TKind.Repeater:
                    Prealloc_Sprite.TextureRect = new IntRect(
                        ((int)tile.Variant) * TEXTURE_SIZE,
                        7 * TEXTURE_SIZE,
                        TEXTURE_SIZE, TEXTURE_SIZE);
                    break;

                default:
                    int texture_index = (int)tile.Kind - 1;
                    Prealloc_Sprite.TextureRect = new IntRect(
                        texture_index * TEXTURE_SIZE,
                        (0 + (tile.Activity ? 1 : 0)) * TEXTURE_SIZE,
                        TEXTURE_SIZE, TEXTURE_SIZE);
                    break;
            }

            switch (tile.Direction)
            {
                case Tile.TDirection.North:
                    break;
                case Tile.TDirection.East:
                    pos.X += TEXTURE_SIZE;
                    break;
                case Tile.TDirection.South:
                    pos.X += TEXTURE_SIZE;
                    pos.Y += TEXTURE_SIZE;
                    break;
                case Tile.TDirection.West:
                    pos.Y += TEXTURE_SIZE;
                    break;
            }
            Prealloc_Sprite.Rotation = tile.RotationDegree();
            Prealloc_Sprite.Position = pos;
            Window.Draw(Prealloc_Sprite);
        }

        private void DrawPlayer()
        {
            var shift = (Game.Player.AnimationStep + (!Game.Player.SpriteDirection ? 0 : 3)) * TEXTURE_SIZE;
            Prealloc_Sprite.TextureRect = new IntRect(
                shift + 0 * TEXTURE_SIZE,
                2 * TEXTURE_SIZE,
                TEXTURE_SIZE,
                2 * TEXTURE_SIZE);
            Prealloc_Sprite.Position = new Vector2f(Game.Player.Position.X, Game.Player.Position.Y);

            Window.Draw(Prealloc_Sprite);
        }

        private void DrawGui()
        {
            Vector2f center = Window.GetView().Center;
            // Coordinates of Window 0,0 pixel in **the world**.
            // Required to trick view to always display gui regardless of view position.
            Vector2f window_zero = new Vector2f(
                center.X - WindowSize.X / Scale / 2,
                center.Y - WindowSize.Y / Scale / 2);

            if (InputState == TInputState.Chat)
            {
                float border_thickness = 2.0f;
                FloatRect bound;

                // CommandBox
                Prealloc_Text.FillColor = new Color(0, 0, 0);
                Prealloc_Text.OutlineThickness = 0;
                Prealloc_Text.DisplayedString = '>' + CommandBox + (System.DateTime.Now.Second % 2 == 0 ? "" : "|");
                Prealloc_Text.Position = window_zero + new Vector2f(0, WindowSize.Y / Scale / 4 * 3);

                bound = Prealloc_Text.GetGlobalBounds();

                Prealloc_RectangleShape.Size = new Vector2f(bound.Width + border_thickness * 2, bound.Height + border_thickness * 2);
                Prealloc_RectangleShape.Position = new Vector2f(bound.Left - border_thickness, bound.Top - border_thickness);
                Prealloc_RectangleShape.FillColor = new Color(200, 200, 200);
                Window.Draw(Prealloc_RectangleShape);

                Window.Draw(Prealloc_Text);

                float previous_message_top = Prealloc_RectangleShape.Position.Y;

                // Chat
                for (int i = ChatLog.Count - 1; i >= 0; i--)
                {
                    Prealloc_Text.FillColor = new Color(0, 0, 0);
                    Prealloc_Text.OutlineThickness = 0;
                    Prealloc_Text.DisplayedString = ChatLog[i];
                    bound = Prealloc_Text.GetGlobalBounds();
                    Prealloc_Text.Position = new Vector2f(window_zero.X + border_thickness, previous_message_top - bound.Height - border_thickness * 2);

                    bound = Prealloc_Text.GetGlobalBounds();

                    Prealloc_RectangleShape.Size = new Vector2f(bound.Width + border_thickness * 2, bound.Height + border_thickness * 2);
                    Prealloc_RectangleShape.Position = new Vector2f(window_zero.X, bound.Top - border_thickness);
                    Prealloc_RectangleShape.FillColor = new Color(200, 200, 200);
                    Window.Draw(Prealloc_RectangleShape);

                    Window.Draw(Prealloc_Text);

                    previous_message_top = Prealloc_RectangleShape.Position.Y;
                }
            }

            {
                Vector2f hotbar_zero = window_zero + new Vector2f(0, (float)WindowSize.Y / Scale - 20);

                for (int i = 0; i < Game.Player.Hotbar.Tiles.Length; i++)
                {
                    Color color = i == Game.Player.Hotbar.Index ? new Color(50, 200, 50, 230) : new Color(50, 50, 50, 230);

                    // Hotbar tile background
                    Prealloc_RectangleShape.Size = new Vector2f(20, 20);
                    Prealloc_RectangleShape.Position = hotbar_zero + new Vector2f(20, 0) * i;
                    Prealloc_RectangleShape.FillColor = color;
                    Window.Draw(Prealloc_RectangleShape);

                    DrawTile(hotbar_zero + new Vector2f(20, 0) * i + new Vector2f(2, 2),
                    Game.Player.Hotbar.Tiles[i]);
                }
            }
        }

        private void DrawLabels()
        {
            foreach (Label l in Game.Labels)
            {
                Prealloc_Text.FillColor = new Color(255, 255, 255);
                Prealloc_Text.OutlineThickness = 3;
                Prealloc_Text.DisplayedString = l.v;
                Prealloc_Text.Position = l.Pos;

                // Draw text background
                FloatRect bounds = Prealloc_Text.GetGlobalBounds();
                float border_thickness = 2.0f;
                Prealloc_RectangleShape.Position = new Vector2f(bounds.Left - border_thickness, bounds.Top - border_thickness);
                Prealloc_RectangleShape.Size = new Vector2f(bounds.Width + border_thickness * 2, bounds.Height + border_thickness * 2);
                Window.Draw(Prealloc_RectangleShape);

                Window.Draw(Prealloc_Text);
            }
        }

        private void MouseWheelScrolled(object _, SFML.Window.MouseWheelScrollEventArgs e)
        {
            uint newIndex = Game.Player.Hotbar.Index + (uint)e.Delta;
            if (newIndex < Game.Player.Hotbar.Tiles.Length)
                Game.Player.Hotbar.Index = newIndex;
        }

        private void HandleKeyPress(object _, SFML.Window.KeyEventArgs args)
        {
            switch (InputState)
            {
                case TInputState.None:
                    switch (args.Code)
                    {
                        case Keyboard.Key.Tilde:
                            InputState = TInputState.Chat;
                            break;
                        case Keyboard.Key.Num1:
                            Game.Player.Hotbar.Index = (uint)(!args.Shift ? 0 : 10);
                            break;
                        case Keyboard.Key.Num2:
                            Game.Player.Hotbar.Index = (uint)(!args.Shift ? 1 : 11);
                            break;
                        case Keyboard.Key.Num3:
                            Game.Player.Hotbar.Index = (uint)(!args.Shift ? 2 : 12);
                            break;
                        case Keyboard.Key.Num4:
                            Game.Player.Hotbar.Index = (uint)(!args.Shift ? 3 : 13);
                            break;
                        case Keyboard.Key.Num5:
                            Game.Player.Hotbar.Index = (uint)(!args.Shift ? 4 : 14);
                            break;
                        case Keyboard.Key.Num6:
                            Game.Player.Hotbar.Index = (uint)(!args.Shift ? 5 : 15);
                            break;
                        case Keyboard.Key.Num7:
                            Game.Player.Hotbar.Index = (uint)(!args.Shift ? 6 : 16);
                            break;
                        case Keyboard.Key.Num8:
                            Game.Player.Hotbar.Index = (uint)(!args.Shift ? 7 : 17);
                            break;
                        case Keyboard.Key.Num9:
                            Game.Player.Hotbar.Index = (uint)(!args.Shift ? 8 : 18);
                            break;
                        case Keyboard.Key.Num0:
                            Game.Player.Hotbar.Index = (uint)(!args.Shift ? 9 : 19);
                            break;
                        case Keyboard.Key.W:
                        case Keyboard.Key.A:
                        case Keyboard.Key.S:
                        case Keyboard.Key.D:
                            switch (args.Code)
                            {
                                case Keyboard.Key.W:
                                    Game.Player.Velocity.Y = -64;
                                    break;
                                case Keyboard.Key.A:
                                    Game.Player.Velocity.X = -96;
                                    Game.Player.SpriteDirection = false;
                                    break;
                                case Keyboard.Key.S:
                                    Game.Player.Velocity.Y = +64;
                                    break;
                                case Keyboard.Key.D:
                                    Game.Player.Velocity.X = +96;
                                    Game.Player.SpriteDirection = true;
                                    break;
                            }
                            break;
                        case Keyboard.Key.R:
                            Tile tile = Game.Player.Hotbar.IndexTile;
                            if (tile.Kind != Tile.TKind.RedstoneWire && tile.Kind != Tile.TKind.RedstoneBridge)
                            {
                                tile.Direction = Tile.TDirectionRotate(tile.Direction, 1); // Rotate tile right
                                Game.Player.Hotbar.IndexTile = tile;
                            }
                            break;
                        case Keyboard.Key.Z:
                            Scale *= 1.1f;
                            Window.SetView(new View(new Vector2f(Game.Player.Position.X + TEXTURE_SIZE / 2, Game.Player.Position.Y + 2 * TEXTURE_SIZE / 2), // Player center
                            new Vector2f(WindowSize.X / Scale, WindowSize.Y / Scale)));
                            break;
                        case Keyboard.Key.X:
                            Scale /= 1.1f;
                            Window.SetView(new View(new Vector2f(Game.Player.Position.X + TEXTURE_SIZE / 2, Game.Player.Position.Y + 2 * TEXTURE_SIZE / 2), // Player center
                            new Vector2f(WindowSize.X / Scale, WindowSize.Y / Scale)));
                            break;
                    }
                    break;
                case TInputState.Chat:
                    switch (args.Code)
                    {
                        case Keyboard.Key.Tilde:
                            InputState = TInputState.None;
                            break;
                        case Keyboard.Key.Backspace:
                            if (CommandBox.Length > 0)
                            {
                                CommandBox = CommandBox.Remove(CommandBox.Length - 1);
                            }
                            break;
                        case Keyboard.Key.Enter:
                            string command_result = Game.ExecuteCommand(CommandBox);
                            if (command_result != "")
                                ChatLog.Add(command_result);
                            LastCommand = CommandBox;
                            CommandBox = "";
                            InputState = TInputState.None;
                            break;
                        case Keyboard.Key.Up:
                            CommandBox = LastCommand;
                            break;
                    }
                    // Chatbox input is handled in TextEntered event
                    break;
            }
        }

        private void HandleKeyRelease(object _, SFML.Window.KeyEventArgs args)
        {
            switch (InputState)
            {
                case TInputState.None:
                    switch (args.Code)
                    {
                        case Keyboard.Key.W:
                        case Keyboard.Key.S:
                            Game.Player.Velocity.Y = 0;
                            break;
                        case Keyboard.Key.A:
                        case Keyboard.Key.D:
                            Game.Player.Velocity.X = 0;
                            break;
                    }
                    break;
            }
        }

        private void HandleTextEnter(object _, SFML.Window.TextEventArgs args)
        {
            switch (InputState)
            {
                case TInputState.Chat:
                    if (args.Unicode.Any(c => char.IsControl(c) | "`~".Contains(c)))
                    {
                        break;
                    }
                    CommandBox += args.Unicode;
                    break;
            }
        }

        private void HandleMouseButtonPress(object _, SFML.Window.MouseButtonEventArgs args)
        {
            Vector2u tile_coord = new Vector2u(
                (uint)System.Math.Ceiling((float)((args.X - WindowSize.X / 2) / Scale + Game.Player.Position.X) / (float)(TEXTURE_SIZE) - 0.5),
                (uint)System.Math.Ceiling((float)((args.Y - WindowSize.Y / 2) / Scale + Game.Player.Position.Y) / (float)(TEXTURE_SIZE)));

            uint bound_north = 1;
            uint bound_west = 1;
            uint bound_east = (uint)Game.Tiles.GetLength(0) - 2;
            uint bound_south = (uint)Game.Tiles.GetLength(1) - 2;

            if (tile_coord.X < bound_north ||
                tile_coord.Y < bound_west ||
                tile_coord.X > bound_east ||
                tile_coord.Y > bound_south)
                return;

            switch (args.Button)
            {
                case Mouse.Button.Left:
                    if (Game.Tiles[tile_coord.X, tile_coord.Y].Kind == Tile.TKind.Lever)
                        Game.ActivateTile(tile_coord);
                    else
                        Game.PlaceTile(tile_coord, Game.Player.Hotbar.IndexTile);
                    break;
                case Mouse.Button.Right:
                    Game.PlaceTile(tile_coord, new Tile(Tile.TKind.Void));
                    break;
                case Mouse.Button.Middle:
                    Tile tile_under_cursor = Game.Tiles[tile_coord.X, tile_coord.Y];
                    tile_under_cursor.Activity = false;
                    tile_under_cursor.Variant = 0;
                    Game.Player.Hotbar.IndexTile = tile_under_cursor; // Pick tile
                    break;
            }
        }

        private void ClockHandlePhysics()
        {
            var elapsed = PhysicsClock.ElapsedTime.AsMilliseconds() / 1000f;
            PhysicsClock.Restart();

            Game.Player.Position.X += Game.Player.Velocity.X * elapsed;
            Game.Player.Position.Y += Game.Player.Velocity.Y * elapsed;

            View view = Window.GetView();
            view.Center = Game.Player.Position + new Vector2f(TEXTURE_SIZE / 2, 2 * TEXTURE_SIZE / 2);
            Window.SetView(view);
        }

        private void ClockHandleTicks()
        {
            // Garbage monitor
            long new_gc_used_memory = System.GC.GetTotalMemory(false);
            if (new_gc_used_memory != GcUsedMemory)
            {
                if (new_gc_used_memory > GcUsedMemory)
                    System.Console.WriteLine("New allocation of {0}. GC total allocated memory: {1}", new_gc_used_memory - GcUsedMemory, new_gc_used_memory);
                else
                    System.Console.WriteLine("Freed {0} bytes. GC total allocated memory: {1}", GcUsedMemory - new_gc_used_memory, new_gc_used_memory);
                GcUsedMemory = new_gc_used_memory;
            }

            float elapsed = TicksClock.ElapsedTime.AsMilliseconds() / 1000f;
            if (elapsed < TICK_LENGTH)
                return;

            TicksClock.Restart();

            Game.TickOnce();
        }

        private enum TInputState
        {
            None,
            Chat,
        }
    }
}
