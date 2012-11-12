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
       
        Texture2D bulletTexture;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;
        TcpClient client;
        string IP = "127.0.0.1";
        int PORT = 1490;
        int BUFFER_SIZE = 2048;
        byte[] readBuffer;
        List<GameplayObject> playerBullets, enemyBullets;
        MemoryStream readStream, writeStream;
        BinaryReader reader;
        BinaryWriter writer;


        Player player;
        StarBackground background;
        GameplayObject enemy;
        Boolean enemyConnected;

        KeyboardState current, previous;
        TimeSpan bulletTimer;
        float shotseconds;

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
            // TODO: Add your initialization logic here
            enemyConnected = false;
            readStream = new MemoryStream();
            reader = new BinaryReader(readStream);
            writeStream = new MemoryStream();
            writer = new BinaryWriter(writeStream);

            enemy = new GameplayObject();
            playerBullets = new List<GameplayObject>(10);
            enemyBullets = new List<GameplayObject>(10);

            current = previous = Keyboard.GetState();
            shotseconds = 5;
            bulletTimer = TimeSpan.FromSeconds(shotseconds);

            base.Initialize();

        }


        protected override void LoadContent()
        {

            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("font1");
            enemy.Texture = Content.Load<Texture2D>("EnemyPaper-2");
            bulletTexture = Content.Load<Texture2D>("BulletPaper_2");
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
            // Allows the game to exit
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
            background.Update(gameTime);


            foreach (GameplayObject gameObject in playerBullets)
                gameObject.Update(gameTime);
            foreach (GameplayObject gameObject in enemyBullets)
                gameObject.Update(gameTime);


            if (enemyConnected)
            {
                writeStream.Position = 0;
                writer.Write((byte)Protocol.PlayerMoved);
                writer.Write(player.getPosition().X);
                writer.Write(player.getPosition().Y);
                writer.Write(player.player.Rotation);
                SendData(GetDataFromMemoryStream(writeStream));
            }

            if (bulletTimer.TotalSeconds > 0) bulletTimer = bulletTimer.Subtract(gameTime.ElapsedGameTime);
            else
            {
                if (current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space) && previous.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.Space))
                {
                    //GameplayObject bullet = new GameplayObject();
                    //bullet.Texture = bulletTexture;
                    //bullet.Position = player.Position;
                    //bullet.Rotation = player.Rotation;
                    //bullet.Speed = 200;
                    //bullet.Velocity = new Vector2(bullet.Speed * (float)Math.Cos(bullet.Rotation),
                    //    bullet.Speed * (float)Math.Sin(bullet.Rotation));
                    //writeStream.Position = 0;
                    //writer.Write((byte)Protocol.BulletCreated);
                    //SendData(GetDataFromMemoryStream(writeStream));
                    //playerBullets.Add(bullet);
                    //bulletTimer = TimeSpan.FromSeconds(shotseconds);
                }
            }

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
                MessageBox.Show(ex.Message);
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
                    string ip = reader.ReadString();
                    if (!enemyConnected)
                    {
                        enemyConnected = true;
                        enemy.Rotation = MathHelper.ToRadians(90);
                        enemy.Position = new Vector2(10, 10);
                        writeStream.Position = 0;
                        writer.Write((byte)Protocol.Connected);
                        SendData(GetDataFromMemoryStream(writeStream));
                    }

                }
                else if (p == Protocol.Disconnected)
                {
                    enemyConnected = false;
                    byte id = reader.ReadByte();
                    string ip = reader.ReadString();
                    enemy = null;
                }
                else if (p == Protocol.PlayerMoved)
                {
                    float px = reader.ReadSingle();
                    float py = reader.ReadSingle();
                    float pr = reader.ReadSingle();
                    byte id = reader.ReadByte();
                    string ip = reader.ReadString();
                    if (px!=null  && pr!=null)
                        enemy.Position = new Vector2(-px + player.getPosition().X + GraphicsDevice.Viewport.Width/2, -py + player.getPosition().Y + GraphicsDevice.Viewport.Height / 2);
                    if (pr!=null)
                        enemy.Rotation = pr;
                }
                else if (p == Protocol.BulletCreated)
                {
                    GameplayObject bullet = new GameplayObject();
                    bullet.Texture = bulletTexture;
                    bullet.Position = enemy.Position;
                    bullet.Rotation = enemy.Rotation;
                    bullet.Speed = 200;
                    bullet.Velocity = new Vector2(bullet.Speed * (float)Math.Cos(bullet.Rotation),
                        bullet.Speed * (float)Math.Sin(bullet.Rotation));
                    enemyBullets.Add(bullet);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        /// Converts a MemoryStream to a byte array
        private byte[] GetDataFromMemoryStream(MemoryStream ms)
        {
            byte[] result;

            //Async method called this, so lets lock the object to make sure other threads/async calls need to wait to use it.
            lock (ms)
            {
                int bytesWritten = (int)ms.Position;
                result = new byte[bytesWritten];

                ms.Position = 0;
                ms.Read(result, 0, bytesWritten);
            }

            return result;
        }

        /// Code to actually send the data to the client
        /// <param name="b">Data to send</param>
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
                MessageBox.Show(e.Message);
            }
        }

        /// This is called when the game should draw itself.
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();
            background.Draw(spriteBatch);
            player.Draw().Draw(gameTime, spriteBatch);
            if (enemyConnected) enemy.Draw(gameTime, spriteBatch);
            foreach (GameplayObject gameObject in playerBullets)
                gameObject.Draw(gameTime, spriteBatch);
            foreach (GameplayObject gameObject in enemyBullets)
                gameObject.Draw(gameTime, spriteBatch);

            spriteBatch.DrawString(font, "Speed:" + ((int)Math.Sqrt(Math.Pow(player.origin.Velocity.X * Math.Cos(player.player.Rotation), 2) + Math.Pow(player.origin.Velocity.Y * Math.Sin(player.player.Rotation), 2))).ToString() + " " + new Vector2(-(int)player.origin.Velocity.X, (int)player.origin.Velocity.Y).ToString(), new Vector2(10, 560), Color.White);
            spriteBatch.DrawString(font, "Coordinates: " + new Vector2((int)player.origin.Position.X, (int)player.origin.Position.Y).ToString(), new Vector2(10, 580), Color.White);
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}