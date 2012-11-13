using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;
using System.IO;

namespace Networked_game
{

    public class Game1 : Microsoft.Xna.Framework.Game
    {
       
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;
        TcpClient client;
        string IP = "127.0.0.1";
        int PORT = 1490;
        int BUFFER_SIZE = 2048;
        byte[] readBuffer;
        MemoryStream readStream, writeStream;
        BinaryReader reader;
        BinaryWriter writer;


        Player player;
        StarBackground background;
        PlayerX[] players;

        KeyboardState current, previous;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 600;
            graphics.PreferredBackBufferWidth = 800;
            graphics.ApplyChanges();
            Content.RootDirectory = "Content";

            current = new KeyboardState();
            previous = new KeyboardState();
        }


        protected override void Initialize()
        {
            players = new PlayerX[100]; //MAX CONNECTION NUMBER
            
            readStream = new MemoryStream();
            reader = new BinaryReader(readStream);
            writeStream = new MemoryStream();
            writer = new BinaryWriter(writeStream);
            current = previous = Keyboard.GetState();

            base.Initialize();

        }


        protected override void LoadContent()
        {

            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("font1");
            player = new Player(new GameplayObject(), 3,5, 3, 0, 500, -300, Content.Load<Texture2D>("PlayerPaper"), new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2));
            background = new StarBackground(player, Content.Load<Texture2D>("fluffyball"), 100);
            client = new TcpClient();
            client.NoDelay = true;
            client.Connect(IP, PORT);
            readBuffer = new byte[BUFFER_SIZE];
            client.GetStream().BeginRead(readBuffer, 0, BUFFER_SIZE, StreamReceived, null);
        }


        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {

            Boolean up = false;
            Boolean down = false;
            Boolean left = false;
            Boolean right = false;

            previous = current;
            current = Keyboard.GetState();


            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                this.Exit();

            if (current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F))
                graphics.ToggleFullScreen();

            if (current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Left))
                left = true;
            if (current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Right))
                right = true;
            if (current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Up))
                up = true;
            if (current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Down))
                down = true;

            player.Update(gameTime, up, down, left, right);

            foreach (PlayerX gameObject in players)
            {
                if (gameObject!=null)
                    gameObject.Update(gameTime);
            }
            background.Update(gameTime);

            writeStream.Position = 0;
            writer.Write((byte)Protocol.PlayerMoved);
            writer.Write((Int16)player.getPosition().X);
            writer.Write((Int16)player.getPosition().Y);
            writer.Write((Int16)MathHelper.ToDegrees(player.player.Rotation));
            SendData(GetDataFromMemoryStream(writeStream));

            base.Update(gameTime);
        }





        private void StreamReceived(IAsyncResult ar)
        {
            int bytesRead = 0;

            try
            {
                lock (client.GetStream())
                {
                    bytesRead = client.GetStream().EndRead(ar);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("3 " + ex.Message);
            }

            if (bytesRead == 0)
            {
                client.Close();
                return;
            }

            byte[] data = new byte[bytesRead];

            for (int i = 0; i < bytesRead; i++)
                data[i] = readBuffer[i];

            ProcessData(data);
            
            client.GetStream().BeginRead(readBuffer, 0, BUFFER_SIZE, StreamReceived, null);
        }

        private void ProcessData(byte[] data)
        {
            int checker=0;

            readStream.SetLength(0); 
            readStream.Position = 0; 
            readStream.Write(data, 0, data.Length);
            readStream.Position = 0; 

            Protocol p;

            try
            {
                p = (Protocol)reader.ReadByte(); 
                if (p == Protocol.Connected)
                {

                    byte id = reader.ReadByte(); 
                    //string ip = reader.ReadString();
                    if (players[id] == null)
                    {
                        players[id] = new PlayerX(new GameplayObject(), Content.Load<Texture2D>("PlayerPaper"));
                        writeStream.Position = 0;
                        writer.Write((byte)Protocol.Connected);
                        SendData(GetDataFromMemoryStream(writeStream));
                    }
                }
                if (p == Protocol.Disconnected)
                {
                    byte id = reader.ReadByte();
                    //string ip = reader.ReadString();
                    players[id] = null;
                }
                if (p == Protocol.PlayerMoved)
                {
                    float px = reader.ReadInt16(); checker = 11;
                    float py = reader.ReadInt16(); checker = 12;
                    float pr = reader.ReadInt16(); checker = 13;
                    byte id = reader.ReadByte(); checker = 14;
                    //string ip = reader.ReadString(); checker = 5;
                    if (players[id]!=null)
                    {
                        players[id].positionX = -px + player.origin.Position.X ;
                        players[id].positionY = -py + player.origin.Position.Y ;
                    }
                    if (players[id]!=null)
                        players[id].player.Rotation =MathHelper.ToRadians(pr);
                }

            }
            catch (Exception ex)
            {
                //MessageBox.Show(checker.ToString() +ex.Message);
            }

        }

        private byte[] GetDataFromMemoryStream(MemoryStream ms)
        {
            byte[] result;
            lock (ms)
            {
                int bytesWritten = (int)ms.Position;
                result = new byte[bytesWritten];

                ms.Position = 0;
                ms.Read(result, 0, bytesWritten);
            }

            return result;
        }

        public void SendData(byte[] b)
        {
            //Try to send the data.  If an exception is thrown, disconnect the client
            try
            {
                lock (client.GetStream())
                {
                    client.GetStream().BeginWrite(b, 0, b.Length, null, null);
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show("2 " + e.Message);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();
            background.Draw(spriteBatch);
            player.Draw().Draw(gameTime, spriteBatch);
            foreach (PlayerX gameObject in players)
            {
                if (gameObject!=null)
                    gameObject.Draw().Draw(gameTime, spriteBatch);
            }
            spriteBatch.DrawString(font, "Rotation   :" + ((int)(MathHelper.ToDegrees(player.player.Rotation)+90+360)%360).ToString(), new Vector2(10, 540), Color.White);
            spriteBatch.DrawString(font, "Speed      :"+ player.fv, new Vector2(10, 560), Color.White);
            spriteBatch.DrawString(font, "Coordinates: " + new Vector2((-(int)player.origin.Position.X+400)/10, (int)(player.origin.Position.Y-300)/10).ToString(), new Vector2(10, 580), Color.White);
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}



