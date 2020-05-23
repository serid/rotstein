using System.Linq;

using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace rotstein
{
    class Drawer
    {
        static readonly uint PLAYGROUND_SIZE = 10;
        static readonly int SCALE = 6; // Game scale
        static readonly int TEXTURE_SIZE = 16;

        Vector2u WindowSize = new Vector2u(1600, 900); // TODO: change type to Vector2f and remove `* SCALE` everywhere
        Texture Atlas;
        RenderWindow Window;
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
            Window.TextEntered += HandleTextEnter;
            Window.MouseButtonPressed += HandleMouseButtonPress;

            Game = new Game(PLAYGROUND_SIZE);

            Window.SetView(new View(new Vector2f(Game.Player.Position.X + TEXTURE_SIZE / 2, Game.Player.Position.Y + 2 * TEXTURE_SIZE / 2), // Player center
            new Vector2f(WindowSize.X / SCALE, WindowSize.Y / SCALE)));

            for (int i = 0; i < PLAYGROUND_SIZE; i++)
            {
                for (int j = 0; j < PLAYGROUND_SIZE; j++)
                {
                    Game.Tiles[i, j] = new Tile(Tile.TKind.Planks);
                }
            }
        }

        public void Loop()
        {
            while (Window.IsOpen)
            {
                Window.DispatchEvents();
                Window.Clear(Color.White);
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
                System.Threading.Thread.Sleep(15);
            }
        }

        void DrawTile(Vector2f pos, Tile tile)
        {
            if (tile.Kind == 0)
                return;


            int texture_index = (int)tile.Kind - 1;
            var sprite = new Sprite(Atlas, new IntRect(
                texture_index * TEXTURE_SIZE,
                0,
                TEXTURE_SIZE, TEXTURE_SIZE));
            sprite.Position = pos;

            Window.Draw(sprite);
        }

        void DrawPlayer()
        {
            var shift = (Game.Player.AnimationStep + (!Game.Player.Direction ? 0 : 3)) * TEXTURE_SIZE;
            var sprite = new Sprite(Atlas, new IntRect(
                shift + 0 * TEXTURE_SIZE,
                1 * TEXTURE_SIZE,
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
                    new Tile(Game.Player.Hotbar.Tiles[i]));
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
                            View view = Window.GetView();
                            int shift = TEXTURE_SIZE / 2;
                            switch (args.Code)
                            {
                                case Keyboard.Key.W:
                                    Game.Player.Position.Y -= (uint)shift;
                                    Game.Player.NextAnimationStep();
                                    view.Move(new Vector2f(0f, -shift));
                                    break;
                                case Keyboard.Key.A:
                                    Game.Player.Direction = false;
                                    Game.Player.NextAnimationStep();
                                    Game.Player.Position.X -= (uint)shift;
                                    view.Move(new Vector2f(-shift, 0f));
                                    break;
                                case Keyboard.Key.S:
                                    Game.Player.Position.Y += (uint)shift;
                                    Game.Player.NextAnimationStep();
                                    view.Move(new Vector2f(0f, shift));
                                    break;
                                case Keyboard.Key.D:
                                    Game.Player.Direction = true;
                                    Game.Player.NextAnimationStep();
                                    Game.Player.Position.X += (uint)shift;
                                    view.Move(new Vector2f(shift, 0f));
                                    break;
                            }
                            Window.SetView(view);
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
                default:
                    throw new System.Exception("Unhandled input state.");
            }
        }

        void HandleTextEnter(object _, SFML.Window.TextEventArgs args)
        {
            switch (InputState)
            {
                case TInputState.None:
                    // Game input is handled in KeyPressed event
                    break;
                case TInputState.Chat:
                    if (args.Unicode.Any(c => char.IsControl(c) | "`~".Contains(c)))
                    {
                        break;
                    }
                    Chatbox += args.Unicode;
                    break;
                default:
                    throw new System.Exception("Unhandled input state.");
            }
        }

        void HandleMouseButtonPress(object _, SFML.Window.MouseButtonEventArgs args)
        {
            Vector2u tile_coord = new Vector2u(
                (uint)System.Math.Ceiling((float)((args.X - WindowSize.X / 2) / SCALE + Game.Player.Position.X) / (float)(TEXTURE_SIZE) - 0.5),
                (uint)System.Math.Ceiling((float)((args.Y - WindowSize.Y / 2) / SCALE + Game.Player.Position.Y) / (float)(TEXTURE_SIZE)));
            Game.PlaceTile(tile_coord.X, tile_coord.Y, new Tile(Game.Player.Hotbar.IndexTile));
        }

        enum TInputState
        {
            None,
            Chat,
        }
    }
}
