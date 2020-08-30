using System.Linq;

using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace rotstein
{
    class Drawer
    {
        private static readonly uint PLAYGROUND_SIZE = 20;
        private static readonly int SCALE = 6; // Game scale
        private static readonly int TEXTURE_SIZE = 16;
        private static readonly float TICK_LENGTH = 0.400f; // In seconds

        private Vector2u WindowSize = new Vector2u(1600, 900); // TODO: change type to Vector2f and remove `* SCALE` everywhere
        private Texture Atlas;
        private RenderWindow Window;
        private Clock PhysicsClock;
        private Clock TicksClock;
        private TInputState InputState = TInputState.None;
        private string Chatbox = "";
        private Font BasicFont = new Font("SourceCodePro-Regular.otf");
        // private Font BasicFont = new Font("Cantarell-Regular.otf");

        private Sprite Prealloc_Sprite;
        private Text Prealloc_Text;
        private RectangleShape Prealloc_RectangleShape;

        private Game Game;

        public Drawer()
        {
            Atlas = new Texture("rsc/atlas.png");
            Prealloc_Sprite = new Sprite(Atlas);
            Prealloc_Text = new Text("", BasicFont, 60);
            Prealloc_Text.Scale = new Vector2f(0.14f, 0.14f);
            Prealloc_Text.FillColor = new Color(0, 0, 0);
            Prealloc_RectangleShape = new RectangleShape();

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
                            DrawTile(new Vector2f(i * TEXTURE_SIZE, j * TEXTURE_SIZE), Game.Tiles[i, j]);
                        }
                    }
                    DrawPlayer();
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
                    Prealloc_Sprite.TextureRect = new IntRect(
                        texture_index * TEXTURE_SIZE,
                        (0 + (tile.Activity ? 1 : 0)) * TEXTURE_SIZE,
                        TEXTURE_SIZE, TEXTURE_SIZE);

                    Prealloc_Sprite.Rotation = tile.RotationDegree();
                    break;
            }
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
                center.X - WindowSize.X / SCALE / 2,
                center.Y - WindowSize.Y / SCALE / 2);

            if (InputState == TInputState.Chat)
            {
                // Chat
                Prealloc_RectangleShape.Size = new Vector2f(100, 10);
                Prealloc_RectangleShape.Position = window_zero + new Vector2f(0, WindowSize.Y / SCALE / 4 * 3);
                Prealloc_RectangleShape.FillColor = new Color(100, 100, 100, 200);
                Window.Draw(Prealloc_RectangleShape);

                Prealloc_Text.DisplayedString = '>' + Chatbox + (System.DateTime.Now.Second % 2 == 0 ? "" : "|");
                Prealloc_Text.Position = Prealloc_RectangleShape.Position + new Vector2f(1.0f, -0.8f);
                Window.Draw(Prealloc_Text);
            }

            {
                Vector2f hotbar_zero = window_zero + new Vector2f(0, (float)WindowSize.Y / SCALE - 20);

                for (int i = 0; i < 9; i++)
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
                            Game.ExecuteCommand(Chatbox);
                            Chatbox = "";
                            InputState = TInputState.None;
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
                    Chatbox += args.Unicode;
                    break;
            }
        }

        private void HandleMouseButtonPress(object _, SFML.Window.MouseButtonEventArgs args)
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
            System.Console.WriteLine(System.GC.CollectionCount(0));

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
