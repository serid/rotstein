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
        Vector2u WindowSize = new Vector2u(1600, 900);
        
        Texture Atlas;
        RenderWindow Window;
        Game Game;
        
        public Drawer()
        {
            Atlas = new Texture("rsc/atlas.png");
            Window = new RenderWindow(new VideoMode(WindowSize.X, WindowSize.Y), "Rotstein",
                Styles.Titlebar | Styles.Close | Styles.Resize);
            Window.Closed += (_, __) => Window.Close();

            Window.Resized += (_, args) => {
                WindowSize = new Vector2u(args.Width, args.Height);
                View view = Window.GetView();
                view.Size = new Vector2f(WindowSize.X / SCALE, WindowSize.Y / SCALE);
                Window.SetView(view);
            };

            Window.KeyPressed += (_, args) => {
                View view = Window.GetView();
                int shift = TEXTURE_SIZE;
                switch (args.Code)
                {
                    case Keyboard.Key.W:
                        Game.Player.Position.Y -= (uint)shift;
                        view.Move(new Vector2f(0f, -shift));
                        break;
                    case Keyboard.Key.A:
                        Game.Player.Position.X -= (uint)shift;
                        view.Move(new Vector2f(-shift, 0f));
                        break;
                    case Keyboard.Key.S:
                        Game.Player.Position.Y += (uint)shift;
                        view.Move(new Vector2f(0f, shift));
                        break;
                    case Keyboard.Key.D:
                        Game.Player.Position.X += (uint)shift;
                        view.Move(new Vector2f(shift, 0f));
                        break;
                }
                Window.SetView(view);
            };

            Window.MouseButtonPressed += (_, args) => {
                //System.Console.WriteLine(args);
                Vector2u tile_coord = new Vector2u(
                    (uint)System.Math.Ceiling((float)((args.X - WindowSize.X/2) / SCALE + Game.Player.Position.X) / (float)(TEXTURE_SIZE) - 0.5),
                    (uint)System.Math.Ceiling((float)((args.Y - WindowSize.Y/2) / SCALE + Game.Player.Position.Y) / (float)(TEXTURE_SIZE)));
                Game.Tiles[tile_coord.X,tile_coord.Y] = new Tile(TileKind.Iron);
            };

            Game = new Game(PLAYGROUND_SIZE);

            Window.SetView(new View(new Vector2f(Game.Player.Position.X + TEXTURE_SIZE / 2, Game.Player.Position.Y + 2 * TEXTURE_SIZE / 2), // Player center
            new Vector2f(WindowSize.X  / SCALE, WindowSize.Y / SCALE)));

            for (int i = 0; i < PLAYGROUND_SIZE; i++)
            {
                for (int j = 0; j < PLAYGROUND_SIZE; j++)
                {
                    Game.Tiles[i,j] = new Tile(TileKind.Planks);
                }
            }
        }

        public void Loop()
        {
            
            //Console.WriteLine(tiles[0,0].kind);

            while (Window.IsOpen)
            {
                Window.DispatchEvents();
                Window.Clear(Color.White);
                for (int i = 0; i < PLAYGROUND_SIZE; i++)
                {
                    for (int j = 0; j < PLAYGROUND_SIZE; j++)
                    {
                        DrawTile(i, j, Game.Tiles[i,j]);
                    }
                }
                DrawPlayer();
                Window.Display();
                System.Threading.Thread.Sleep(15);
            }
        }
        
        void DrawTile(int x, int y, Tile tile)
        {
            if (tile.Kind == 0)
                return;
            

            int texture_index = (int)tile.Kind - 1;
            var sprite = new Sprite(Atlas, new IntRect(
                texture_index * TEXTURE_SIZE,
                0,
                TEXTURE_SIZE, TEXTURE_SIZE));
            sprite.Position = new Vector2f(x * TEXTURE_SIZE, y * TEXTURE_SIZE);

            Window.Draw(sprite);
        }

        void DrawPlayer()
        {
            var sprite = new Sprite(Atlas, new IntRect(
                0 * TEXTURE_SIZE,
                1 * TEXTURE_SIZE,
                TEXTURE_SIZE, 2 * TEXTURE_SIZE));
            sprite.Position = new Vector2f(Game.Player.Position.X, Game.Player.Position.Y);
            
            Window.Draw(sprite);
        }
    }
}
