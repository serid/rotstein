using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace rotstein
{
    class Drawer
    {
        static readonly int PLAYGROUND_SIZE = 10;
        static readonly int SCALE = 6; // Game scale
        static readonly int TEXTURE_SIZE = 16;
        static readonly Vector2u WINDOW_SIZE = new Vector2u(1600, 900);
        
        Texture atlas;
        RenderWindow window;
        Game game;
        Tile[,] tiles;
        
        public Drawer()
        {
            atlas = new Texture("rsc/atlas.png");
            window = new RenderWindow(new VideoMode(WINDOW_SIZE.X, WINDOW_SIZE.Y), "Rotstein",
                Styles.Titlebar | Styles.Close);
            window.Closed += (_, __) => window.Close();

            window.KeyPressed += (_, args) => {
                View view = window.GetView();
                int shift = TEXTURE_SIZE / 2 / 2;
                switch (args.Code)
                {
                    case Keyboard.Key.W:
                        game.Player.Y -= (uint)shift;
                        view.Move(new Vector2f(0f, -shift));
                        break;
                    case Keyboard.Key.A:
                        game.Player.X -= (uint)shift;
                        view.Move(new Vector2f(-shift, 0f));
                        break;
                    case Keyboard.Key.S:
                        game.Player.Y += (uint)shift;
                        view.Move(new Vector2f(0f, shift));
                        break;
                    case Keyboard.Key.D:
                        game.Player.X += (uint)shift;
                        view.Move(new Vector2f(shift, 0f));
                        break;
                }
                window.SetView(view);
            };

            game = new Game();

            window.SetView(new View(new Vector2f(game.Player.X + TEXTURE_SIZE / 2, game.Player.Y + 2 * TEXTURE_SIZE / 2), // Player center
            new Vector2f(WINDOW_SIZE.X  / SCALE, WINDOW_SIZE.Y / SCALE)));

            tiles = new Tile[PLAYGROUND_SIZE, PLAYGROUND_SIZE];

            for (int i = 0; i < PLAYGROUND_SIZE; i++)
            {
                for (int j = 0; j < PLAYGROUND_SIZE; j++)
                {
                    tiles[i,j] = new Tile(TileKind.Planks);
                }
            }
        }

        public void Loop()
        {
            
            //Console.WriteLine(tiles[0,0].kind);

            while (window.IsOpen)
            {
                window.DispatchEvents();
                window.Clear(Color.White);
                for (int i = 0; i < PLAYGROUND_SIZE; i++)
                {
                    for (int j = 0; j < PLAYGROUND_SIZE; j++)
                    {
                        DrawTile(i, j, tiles[i,j]);
                    }
                }
                DrawPlayer();
                window.Display();
                System.Threading.Thread.Sleep(15);
            }
        }
        
        void DrawTile(int x, int y, Tile tile)
        {
            if (tile.kind == 0)
                return;
            

            int texture_index = (int)tile.kind - 1;
            var sprite = new Sprite(atlas, new IntRect(
                texture_index * TEXTURE_SIZE,
                0,
                TEXTURE_SIZE, TEXTURE_SIZE));
            sprite.Position = new Vector2f(x * TEXTURE_SIZE, y * TEXTURE_SIZE);

            window.Draw(sprite);
        }

        void DrawPlayer()
        {
            var sprite = new Sprite(atlas, new IntRect(
                0 * TEXTURE_SIZE,
                1 * TEXTURE_SIZE,
                TEXTURE_SIZE, 2 * TEXTURE_SIZE));
            sprite.Position = new Vector2f(game.Player.X, game.Player.Y);
            
            window.Draw(sprite);
        }
    }
}
