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
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        Texture2D bulletTexture;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        TcpClient client;
        string IP = "127.0.0.1";
        int PORT = 1490;
        int BUFFER_SIZE = 2048;
        byte[] readBuffer;
        List<GameplayObject> playerBullets, enemyBullets;
        MemoryStream readStream, writeStream;
        BinaryReader reader;
        BinaryWriter writer;
        //Temp ship stats
        
        float ms;
        float fa;
        float ba;
        float fv;
        float bv;
        float mfv;
        float mbv;

        GameplayObject player;
        GameplayObject origin;
        GameplayObject enemy;
        Boolean enemyConnected;

        KeyboardState current, previous;
        TimeSpan bulletTimer;
        float shotseconds;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            current = new KeyboardState();
            previous = new KeyboardState();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            enemyConnected=false;
            readStream = new MemoryStream();
            reader = new BinaryReader(readStream);
            writeStream = new MemoryStream();
            writer = new BinaryWriter(writeStream);
            player = new GameplayObject();
            origin = new GameplayObject();
            enemy = new GameplayObject();
            playerBullets = new List<GameplayObject>(10);
            enemyBullets = new List<GameplayObject>(10);

            current = previous = Keyboard.GetState();
            shotseconds = 5;
            bulletTimer = TimeSpan.FromSeconds(shotseconds);

            base.Initialize();

        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            ms = 3;
            fv = 0;
            
            fa = 10;
            ba = 5;
            mfv = 500;
            mbv = -500;

            player.Texture = Content.Load<Texture2D>("PlayerPaper");
            enemy.Texture = Content.Load<Texture2D>("EnemyPaper-2");
            origin.Texture = Content.Load<Texture2D>("origin");
            player.Rotation = MathHelper.ToRadians(-90);
            player.Position = new Vector2(GraphicsDevice.Viewport.Width/2, GraphicsDevice.Viewport.Height / 2);
            origin.Position = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            bulletTexture = Content.Load<Texture2D>("BulletPaper_2");

            client = new TcpClient();
            client.NoDelay = true;
            client.Connect(IP, PORT);
            readBuffer = new byte[BUFFER_SIZE];
            client.GetStream().BeginRead(readBuffer, 0, BUFFER_SIZE, StreamReceived, null);
            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            previous = current;
            current = Keyboard.GetState();
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here
            Vector2 iPosition = new Vector2(player.Position.X, player.Position.Y);

            Vector2 movement = Vector2.Zero;

            if (current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Left))
            {
                //MessageBox.Show(origin.Position.X.ToString());
                player.Rotation += MathHelper.ToRadians(-ms);
            }
            if (current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Right))
                player.Rotation += MathHelper.ToRadians(+ms);
            if (current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Up))
            {
                if (fv < mfv)
                    fv += fa;
            }
            if (current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Down))
            {
                if (fv > mbv)
                    fv -= ba;
            }

            movement.X = -fv * (float)Math.Cos(player.Rotation);
            movement.Y = -fv * (float)Math.Sin(player.Rotation);

           origin.Velocity = movement;
           origin.Update(gameTime);

            foreach (GameplayObject gameObject in playerBullets)
                gameObject.Update(gameTime);
            foreach (GameplayObject gameObject in enemyBullets)
                gameObject.Update(gameTime);


            if (movement != Vector2.Zero &&enemyConnected) 
            {
                writeStream.Position = 0;
                writer.Write((byte)Protocol.PlayerMoved);
                writer.Write(origin.Position.X);
                writer.Write(origin.Position.Y);
                if (player.Rotation!=null)
                 writer.Write(player.Rotation);
                SendData(GetDataFromMemoryStream(writeStream));
            }

            if (bulletTimer.TotalSeconds > 0) bulletTimer = bulletTimer.Subtract(gameTime.ElapsedGameTime);
            else
            {
                if (current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space) && previous.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.Space))
                {
                    GameplayObject bullet = new GameplayObject();
                    bullet.Texture = bulletTexture;
                    bullet.Position = player.Position;
                    bullet.Rotation = player.Rotation;
                    bullet.Speed = 200;
                    bullet.Velocity = new Vector2(bullet.Speed * (float)Math.Cos(bullet.Rotation),
                        bullet.Speed * (float)Math.Sin(bullet.Rotation));
                    writeStream.Position = 0;
                    writer.Write((byte)Protocol.BulletCreated);
                    SendData(GetDataFromMemoryStream(writeStream));
                    playerBullets.Add(bullet);
                    bulletTimer = TimeSpan.FromSeconds(shotseconds);
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
                        enemy.Position = new Vector2(10,10);
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
                    enemy.Position = new Vector2(-px + origin.Position.X + GraphicsDevice.Viewport.Width / 2, -py + origin.Position.Y + GraphicsDevice.Viewport.Height / 2);
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

        /// <summary>
        /// Converts a MemoryStream to a byte array
        /// </summary>
        /// <param name="ms">MemoryStream to convert</param>
        /// <returns>Byte array representation of the data</returns>
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

        /// <summary>
        /// Code to actually send the data to the client
        /// </summary>
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
            }
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            spriteBatch.Begin();
            if (player != null) player.Draw(gameTime, spriteBatch);
            if (origin != null) origin.Draw(gameTime, spriteBatch);
            if (enemyConnected) enemy.Draw(gameTime, spriteBatch);
            foreach (GameplayObject gameObject in playerBullets)
                gameObject.Draw(gameTime, spriteBatch);
            foreach (GameplayObject gameObject in enemyBullets)
                gameObject.Draw(gameTime, spriteBatch);
            
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
