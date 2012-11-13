using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Windows.Forms;
using System.IO;
namespace Networked_game
{
    class PlayerX
    {
        public GameplayObject player;
        public float positionX;
        public float positionY;

        public PlayerX(GameplayObject player,Texture2D texture)
        {
            this.player=player;
            player.Texture=texture;
            player.Rotation = MathHelper.ToRadians(-90);
            positionX = 0;
            positionY = 0;
        }

        public GameplayObject Draw()
        {
            return player;
        }

        public void Update(GameTime gameTime)
        {
            player.Position = (new Vector2(positionX, positionY));
            player.Update(gameTime);
        }
    }
}
