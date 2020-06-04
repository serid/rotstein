using System.Linq;

using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace rotstein
{
    class Drawer
    {
        static readonly uint PLAYGROUND_SIZE = 20;
        static readonly int SCALE = 6; // Game scale
        static readonly int TEXTURE_SIZE = 16;
        static readonly float TICK_LENGTH = 0.400f; // In seconds

        Vector2u WindowSize = new Vector2u(1600, 900); // TODO: change type to Vector2f and remove `* SCALE` everywhere
        Texture Atlas;
        RenderWindow Window;
        Clock PhysicsClock;
        Clock TicksClock;
        TInputState InputState = TInputState.None;
        string Chatbox = "";
        Font BasicFont = new Font("SourceCodePro-Regular.otf");
        // Font BasicFont = new Font("Cantarell-Regular.otf");

        Game Game;

        public Drawer()
        {
            Atlas = new Texture("rsc/atlas.png");
            Window = new RenderWindow(new VideoMode(WindowSize.X, WindowSize.Y), "Rotstein",
                Styles.Titlebar | Styles.Close | Styles.Resize);
            Window.Closed += (_, __) => Window.Close();

            Window.Resized += (_, args) =>
            {
                WindowSize = new Vector2u(args.Width, args.Height);
                View view = Window.GetView();
                view.Size = new Vector2f(WindowSize.X / SCALE, WindowSize.Y / SCALE);
                Window.SetView(view);
            };

            Window.KeyPressed += HandleKeyPress;
            Window.KeyReleased += HandleKeyRelease;
            Window.TextEntered += HandleTextEnter;
            Window.MouseButtonPressed += HandleMouseButtonPress;
            Window.SetKeyRepeatEnabled(false);
            Window.SetVerticalSyncEnabled(true);

            Game = new Game(PLAYGROUND_SIZE);

            Window.SetView(new View(new Vector2f(Game.Player.Position.X + TEXTURE_SIZE / 2, Game.Player.Position.Y + 2 * TEXTURE_SIZE / 2), // Player center
            new Vector2f(WindowSize.X / SCALE, WindowSize.Y / SCALE)));

            for (int i = 0; i < PLAYGROUND_SIZE; i++)
            {
                Game.Tiles[0, i] = new Tile(Tile.TKind.Iron);
                Game.Tiles[PLAYGROUND_SIZE - 1, i] = new Tile(Tile.TKind.Iron);
            }
            for (int i = 1; i < PLAYGROUND_SIZE - 1; i++)
            {
                Game.Tiles[i, 0] = new Tile(Tile.TKind.Iron);
                Game.Tiles[i, PLAYGROUND_SIZE - 1] = new Tile(Tile.TKind.Iron);
            }
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
                    for (int i = 0; i < PLAYGROUND_SIZE; i++)
                    {
                        for (int j = 0; j < PLAYGROUND_SIZE; j++)
                        {
                            int redwire_directions;
                            if (Game.Tiles[i, j].Kind == Tile.TKind.RedstoneWire)
                            {
                                redwire_directions =
                                    ((Game.IsRedConnected(Game.Tiles[i, j - 1], Tile.TDirection.South) ? 1 : 0) << 0) |
                                    ((Game.IsRedConnected(Game.Tiles[i + 1, j], Tile.TDirection.West) ? 1 : 0) << 1) |
                                    ((Game.IsRedConnected(Game.Tiles[i, j + 1], Tile.TDirection.North) ? 1 : 0) << 2) |
                                    ((Game.IsRedConnected(Game.Tiles[i - 1, j], Tile.TDirection.East) ? 1 : 0) << 3);
                            }
                            else
                            {
                                redwire_directions = 0;
                            }
                            DrawTile(new Vector2f(i * TEXTURE_SIZE, j * TEXTURE_SIZE), Game.Tiles[i, j], redwire_directions);
                        }
                    }
                    DrawPlayer();
                    DrawGui();

                    Window.Display();
                }
            }
        }

        void DrawTile(Vector2f pos, Tile tile, int redwire_directions = 0)
        {
            Sprite sprite;
            switch (tile.Kind)
            {
                case Tile.TKind.Void:
                    return;

                case Tile.TKind.RedstoneWire:
                    sprite = new Sprite(Atlas, new IntRect(
                        redwire_directions * TEXTURE_SIZE,
                        (4 + (int)(tile.Variant)) * TEXTURE_SIZE,
                        TEXTURE_SIZE, TEXTURE_SIZE));
                    break;

                default:
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

                    int texture_index = (int)tile.Kind - 1;
                    sprite = new Sprite(Atlas, new IntRect(
                        texture_index * TEXTURE_SIZE,
                        0 + (int)(tile.Variant) * TEXTURE_SIZE,
                        TEXTURE_SIZE, TEXTURE_SIZE));

                    sprite.Rotation = tile.RotationDegree();
                    break;
            }
            sprite.Position = pos;
            Window.Draw(sprite);
        }

        void DrawPlayer()
        {
            var shift = (Game.Player.AnimationStep + (!Game.Player.SpriteDirection ? 0 : 3)) * TEXTURE_SIZE;
            var sprite = new Sprite(Atlas, new IntRect(
                shift + 0 * TEXTURE_SIZE,
                2 * TEXTURE_SIZE,
                TEXTURE_SIZE,
                2 * TEXTURE_SIZE));
            sprite.Position = new Vector2f(Game.Player.Position.X, Game.Player.Position.Y);

            Window.Draw(sprite);
        }

        void DrawGui()
        {
            Vector2f center = Window.GetView().Center;
            // Coordinates of Window 0,0 pixel in **the world**.
            // Required to trick view to always display gui regardless of view position.
            Vector2f window_zero = new Vector2f(
                center.X - WindowSize.X / SCALE / 2,
                center.Y - WindowSize.Y / SCALE / 2);

            if (InputState == TInputState.Chat)
            {
                RectangleShape chat_background = new RectangleShape(new Vector2f(100, 10));
                chat_background.Position = window_zero + new Vector2f(0, WindowSize.Y / SCALE / 4 * 3);
                chat_background.FillColor = new Color(100, 100, 100, 200);
                Window.Draw(chat_background);

                Text text = new Text('>' + Chatbox + (System.DateTime.Now.Second % 2 == 0 ? "" : "|"), BasicFont, 60);
                text.Scale = new Vector2f(0.14f, 0.14f);
                text.FillColor = new Color(0, 0, 0);
                text.Position = chat_background.Position + new Vector2f(1.0f, -0.8f);
                Window.Draw(text);
            }

            {
                Vector2f hotbar_zero = window_zero + new Vector2f(0, (float)WindowSize.Y / SCALE - 20);

                for (int i = 0; i < 9; i++)
                {
                    Color color = i == Game.Player.Hotbar.Index ? new Color(50, 200, 50, 230) : new Color(50, 50, 50, 230);

                    RectangleShape hotbar_tile_background = new RectangleShape(new Vector2f(20, 20));
                    hotbar_tile_background.Position = hotbar_zero + new Vector2f(20, 0) * i;
                    hotbar_tile_background.FillColor = color;
                    Window.Draw(hotbar_tile_background);

                    DrawTile(hotbar_zero + new Vector2f(20, 0) * i + new Vector2f(2, 2),
                    Game.Player.Hotbar.Tiles[i]);
                }
            }
        }

        void HandleKeyPress(object _, SFML.Window.KeyEventArgs args)
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
                            Game.Player.Hotbar.Index = 0;
                            break;
                        case Keyboard.Key.Num2:
                            Game.Player.Hotbar.Index = 1;
                            break;
                        case Keyboard.Key.Num3:
                            Game.Player.Hotbar.Index = 2;
                            break;
                        case Keyboard.Key.Num4:
                            Game.Player.Hotbar.Index = 3;
                            break;
                        case Keyboard.Key.Num5:
                            Game.Player.Hotbar.Index = 4;
                            break;
                        case Keyboard.Key.Num6:
                            Game.Player.Hotbar.Index = 5;
                            break;
                        case Keyboard.Key.Num7:
                            Game.Player.Hotbar.Index = 6;
                            break;
                        case Keyboard.Key.Num8:
                            Game.Player.Hotbar.Index = 7;
                            break;
                        case Keyboard.Key.Num9:
                            Game.Player.Hotbar.Index = 8;
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
                            tile.Direction = Tile.TDirectionRotate(tile.Direction, 1); // Rotate tile right
                            Game.Player.Hotbar.IndexTile = tile;
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
                            if (Chatbox.Length > 0)
                            {
                                Chatbox = Chatbox.Remove(Chatbox.Length - 1);
                            }
                            break;
                        case Keyboard.Key.Enter:
                            Chatbox = "";
                            // TODO: execute command
                            InputState = TInputState.None;
                            break;
                    }
                    // Chatbox input is handled in TextEntered event
                    break;
            }
        }

        void HandleKeyRelease(object _, SFML.Window.KeyEventArgs args)
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

        void HandleTextEnter(object _, SFML.Window.TextEventArgs args)
        {
            switch (InputState)
            {
                case TInputState.Chat:
                    if (args.Unicode.Any(c => char.IsControl(c) | "`~".Contains(c)))
                    {
                        break;
                    }
                    Chatbox += args.Unicode;
                    break;
            }
        }

        void HandleMouseButtonPress(object _, SFML.Window.MouseButtonEventArgs args)
        {
            Vector2u tile_coord = new Vector2u(
                (uint)System.Math.Ceiling((float)((args.X - WindowSize.X / 2) / SCALE + Game.Player.Position.X) / (float)(TEXTURE_SIZE) - 0.5),
                (uint)System.Math.Ceiling((float)((args.Y - WindowSize.Y / 2) / SCALE + Game.Player.Position.Y) / (float)(TEXTURE_SIZE)));
            switch (args.Button)
            {
                case Mouse.Button.Left:
                    Game.PlaceTile(tile_coord, Game.Player.Hotbar.IndexTile);
                    break;
                case Mouse.Button.Right:
                    Game.PlaceTile(tile_coord, new Tile(Tile.TKind.Void));
                    break;
            }
        }

        void ClockHandlePhysics()
        {
            var elapsed = PhysicsClock.ElapsedTime.AsMilliseconds() / 1000f;
            PhysicsClock.Restart();

            View view = Window.GetView();
            Game.Player.Position.X += Game.Player.Velocity.X * elapsed;
            Game.Player.Position.Y += Game.Player.Velocity.Y * elapsed;
            view.Move(new Vector2f(Game.Player.Velocity.X * elapsed, Game.Player.Velocity.Y * elapsed));
            Window.SetView(view);
        }

        void ClockHandleTicks()
        {
            float elapsed = TicksClock.ElapsedTime.AsMilliseconds() / 1000f;
            if (elapsed < TICK_LENGTH)
                return;

            TicksClock.Restart();
            System.Console.WriteLine("tick");

            Game.TickOnce();
        }

        enum TInputState
        {
            None,
            Chat,
        }
    }
}
