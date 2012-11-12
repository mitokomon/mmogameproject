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
    public class Player
    {
        float ms;
        float fa;
        float ba;
        float fv;
        float mfv;
        float mbv;
        public GameplayObject player;
        public GameplayObject origin;


        public Player(GameplayObject player, float ms, float fa, float ba, float fv, float mfv, float mbv, Texture2D texture,Vector2 position)
        {
            this.player = player;
            this.ms = ms;
            this.fa = fa;
            this.ba = ba;
            this.fv = fv;
            this.mfv = mfv;
            this.mbv = mbv;
            player.Texture = texture;
            player.Position = position;
            player.Rotation = MathHelper.ToRadians(-90);
            //use origin for coordinate system//
            origin = new GameplayObject();
            origin.Texture = texture;
            origin.Position = position;
        }

        public GameplayObject Draw( )
        {
            return player;
        }

        public void Update(GameTime gameTime,Boolean up,Boolean down,Boolean left, Boolean right) 
        {
            //Movement//
            if (right == true)
            {
                //MessageBox.Show(origin.Position.ToString());
                player.Rotation += MathHelper.ToRadians(ms);
            }
            if (left==true)
                player.Rotation += MathHelper.ToRadians(-ms);
            if (up == true)
            {
                if (    Math.Sqrt(Math.Pow(-fv * (float)Math.Cos(player.Rotation),2)+ Math.Pow(-fv * (float)Math.Sin(player.Rotation),2))<mfv  )
                    fv += fa;
            }
            else if (down == true)
            {
                if (    Math.Sqrt(Math.Pow(-fv * (float)Math.Cos(player.Rotation),2)+ Math.Pow(-fv * (float)Math.Sin(player.Rotation),2))>mbv  )
                    fv -= ba;
            }
            origin.Velocity = new Vector2(-fv * (float)Math.Cos(player.Rotation), -fv * (float)Math.Sin(player.Rotation));
            origin.Update(gameTime);
        }

        public Vector2 getPosition()
        {
            return new Vector2(origin.Position.X-player.Position.X,origin.Position.Y-player.Position.Y);
        }

    }
}
