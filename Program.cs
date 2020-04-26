using System;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace rotstein
{
    class Program
    {
        const int PLAYGROUND_SIZE = 10;
        static void Main(string[] args)
        {
            var atlas = new Texture("rsc/atlas.png");

            Tile[,] tiles = new Tile[PLAYGROUND_SIZE, PLAYGROUND_SIZE];
            
            //Console.WriteLine(tiles[0,0].kind);

            var window = new RenderWindow(new VideoMode(1600, 900), "Rotstein", Styles.Titlebar | Styles.Close);
            window.Closed += (_, __) => window.Close();

            while (window.IsOpen)
            {
                window.DispatchEvents();
                window.Clear(Color.White);
                for (int i = 0; i < PLAYGROUND_SIZE; i++)
                {
                    for (int j = 0; j < PLAYGROUND_SIZE; j++)
                    {
                        DrawTile(window, atlas, i, j, tiles[i,j]);
                    }
                }
                //DrawTile(window, tile);
                window.Display();
                System.Threading.Thread.Sleep(15);
            }
        }

        static void DrawTile(RenderWindow window, Texture atlas, int x, int y, Tile tile)
        {
            int texture_index = 0;
            var sprite = new Sprite(atlas, new IntRect(
                texture_index * 16,
                0,
                16, 16));
            sprite.Position = new Vector2f(x * 16 * 6, y * 16 * 6);
            sprite.Scale = new Vector2f(6.0f, 6.0f);

            window.Draw(sprite);
        }
    }
}
