using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace rotstein
{
    class Drawer
    {
        const int PLAYGROUND_SIZE = 10;
        Texture atlas;
        RenderWindow window;
        Tile[,] tiles;
        
        public Drawer()
        {
            atlas = new Texture("rsc/atlas.png");
            window = new RenderWindow(new VideoMode(1600, 900), "Rotstein",
                Styles.Titlebar | Styles.Close);
            window.Closed += (_, __) => window.Close();
            tiles = new Tile[PLAYGROUND_SIZE, PLAYGROUND_SIZE];
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
                window.Display();
                System.Threading.Thread.Sleep(15);
            }
        }
        
        void DrawTile(int x, int y, Tile tile)
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
