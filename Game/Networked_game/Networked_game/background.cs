using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Networked_game
{
    public class StarBackground
    {
        private Texture2D _StarTexture;
        private Texture2D _CloudTexture;
        private Player player;

        private List<Star> _Stars;
        private int _Intensity;
        private Random _Random;

        private int MaxX;
        private int MaxY;

        public StarBackground(Player player,Texture2D texture, int Intensity)
        {
            this.player = player;
            _Intensity = Intensity;
            _Stars = new List<Star>();
            _Random = new Random(DateTime.Now.Millisecond);

            MaxX = 1000;
            MaxY = 1000;

            Vector2 PlayPos = player.getPosition();
            _StarTexture = texture;
            _CloudTexture = texture;

            for (int i = 0; i <= Intensity; i++)
            {
                int StarColorR = _Random.Next(25, 100);
                int StarColorG = _Random.Next(10, 100);
                int StarColorB = _Random.Next(90, 150);
                int StarColorA = _Random.Next(10, 50);

                float Scale = _Random.Next(100, 500) / 100f;
                int Depth = _Random.Next(4, 7);

                _Stars.Add(new Star(new Vector2(_Random.Next(MaxX / -2 - 500, MaxX / 2 + 500), _Random.Next(MaxY / -2 - 500, MaxY / 2 + 500)), new Color(StarColorR / 3, StarColorG / 3, StarColorB / 3, StarColorA / 3), Scale, Depth, true));
            }

            for (int i = 0; i <= Intensity * 4; i++)
            {
                int StarColor = _Random.Next(100, 200);
                int Depth = _Random.Next(1, 4);
                float Scale = _Random.Next(2, 9) / 100f;

                _Stars.Add(new Star(new Vector2(_Random.Next(MaxX / -2 - 200, MaxX / 2 + 200), _Random.Next(MaxY / -2 - 200, MaxY / 2 + 200)), new Color(StarColor, StarColor, StarColor, StarColor), Scale, Depth, false));
            }

        }

        public void Update(GameTime time)
        {
            foreach (Star s in _Stars)
            {
                s.Position -= player.origin.Velocity * -1f /500* s.Depth;

                if (s.Position.X > MaxX + 501)
                    s.Position.X -= s.Position.X + 500;

                if (s.Position.Y > MaxY + 500)
                    s.Position.Y -= s.Position.Y + 500;

                if (s.Position.X < -560)
                    s.Position.X += MaxX + 550;

                if (s.Position.Y < -570)
                    s.Position.Y += MaxY + 510;
            }
        }

        public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin();
            foreach (Star s in _Stars)
            {
                spriteBatch.Draw(_CloudTexture, s.Position, null, s.Color, 0, Vector2.Zero, s.Scale, SpriteEffects.None, 0);
            }
            spriteBatch.End();
            spriteBatch.Begin();
        }

    }
    class Star
    {
        public Vector2 Position;
        public Color Color;
        public float Scale;
        public float Depth;
        public bool isCloud;
        public Star(Vector2 Position, Color Color, float Scale, int Depth, bool isCloud)
        {
            this.Position = Position;
            this.Color = Color;
            this.Scale = Scale;
            this.Depth = Depth;
            this.isCloud = isCloud;
        }
    }
}