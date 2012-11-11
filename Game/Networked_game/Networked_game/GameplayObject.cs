
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Networked_game
{
    /// <summary>
    /// Holds the states our object can have
    /// </summary>
    public enum ObjectStatus
    {
        Active,
        Dying,
        Dead
    }
    public class GameplayObject
    {
        #region Status Data
        /// <summary>
        /// The current status of the game object.  No public set since this should all be done internally
        /// If you need to set this externally, add the set in.  Use protected set if you only set with
        /// derived classes.
        /// </summary>
        ObjectStatus status;
        public ObjectStatus Status
        {
            get { return status; }

        }

        #endregion

        #region Graphics Data
        /// <summary>
        /// The current texture of our game object
        /// </summary>
        Texture2D texture;
        public Texture2D Texture
        {
            get { return texture; }
            set
            {
                //When we set the texture, we also want to get
                //the data right away and calculate the matrix
                texture = value;
                TextureData = new Color[value.Width * value.Height];
                Texture.GetData(TextureData);

                //Calculate the matrix of the object
                CalculateMatrix();
            }
        }

        //The color data of every pixel in
        //the image
        public Color[] TextureData
        {
            get;
            private set;
        }

        /// <summary>
        /// A rectangle of the object used for rectangular collision
        /// </summary>
        Rectangle rectangle;
        public Rectangle Rectangle
        {
            get { return rectangle; }
        }

        /// <summary>
        /// Transform matrix for correct rectangle collision and
        /// per-pixel collision
        /// </summary>
        public Matrix Transform
        {
            get;
            private set;
        }

        /// <summary>
        /// Origin property used for the center of the game object
        /// </summary>
        public Vector2 Origin
        {
            get
            {
                return new Vector2(texture.Width / 2.0f, texture.Height / 2.0f);
            }
        }

        /// <summary>
        /// Opacity and alpha of the object
        /// </summary>
        float opacity = 1.0f;
        float Alpha
        {
            get { return opacity; }
        }

        /// <summary>
        /// Color defaulted to White.  The Color property uses the Alpha property
        /// to automatically use the opacity.
        /// </summary>
        Color color = Color.White;
        protected Color Color
        {
            get
            {
                return color * opacity;
            }
            set { color = value; }
        }
        #endregion

        #region Physics Data
        /// <summary>
        /// The location of the game object in the game world
        /// </summary>
        Vector2 position = Vector2.Zero;
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }

        /// <summary>
        /// Object's velocity to update the position
        /// </summary>
        Vector2 velocity = Vector2.Zero;
        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }

        /// <summary>
        /// Object's acceleration to update the velocity and speed
        /// </summary>
        Vector2 acceleration = Vector2.Zero;
        public Vector2 Acceleration
        {
            get { return acceleration; }
            set { acceleration = value; }
        }

        /// <summary>
        /// Where our object is looking
        /// </summary>
        float rotation = 0f;
        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        /// <summary>
        /// Speed of our object, used with rotation and updated velocity.
        /// </summary>
        float speed = 0.0f;
        public float Speed
        {
            get { return speed; }
            set { speed = value; }
        }
        #endregion

        #region Die Data
        /// <summary>
        /// The time it takes for our object to die
        /// </summary>
        TimeSpan dieTime = TimeSpan.Zero;
        public TimeSpan DieTime
        {
            get { return dieTime; }
            set { dieTime = value; }
        }

        /// <summary>
        /// 0.0 - fully alive, 1.0 - fully dead.
        /// </summary>
        float diePercent = 0.0f;
        #endregion

        #region Initialization Methods
        /// <summary>
        /// Initialize method, used for initializing object's variables and fields
        /// </summary>
        public virtual void Initialize()
        {
            if (!(status == ObjectStatus.Active))
                status = ObjectStatus.Active;
        }
        #endregion

        #region Update and Draw Methods
        /// <summary>
        /// Update the game object. 
        /// </summary>
        /// <param name="gameTime">The GameTime object from the active screen</param>
        public virtual void Update(GameTime gameTime)
        {
            if (status == ObjectStatus.Active)
            {
                velocity += Vector2.Multiply(acceleration, (float)gameTime.ElapsedGameTime.TotalSeconds);
                position += Vector2.Multiply(velocity, (float)gameTime.ElapsedGameTime.TotalSeconds);
                CalculateMatrix();
                CalculateBoundingRectangle();
            }
            else if (status == ObjectStatus.Dying)
            {
                Dying(gameTime);
            }
            else if (status == ObjectStatus.Dead)
            {
                Dead(gameTime);
            }
        }

        /// <summary>
        /// Calculates the transform matrix of the object with the origin,
        /// rotation, scale, and position.  This will need to be done every
        /// game loop because chances are the position changed.
        /// </summary>
        private void CalculateMatrix()
        {
            Transform = Matrix.CreateTranslation(new Vector3(-Origin, 0)) *
                Matrix.CreateRotationZ(rotation) *
                Matrix.CreateScale(1.0f) *
                Matrix.CreateTranslation(new Vector3(position, 0));
        }

        /// <summary>
        /// Calculates the bounding rectangle of the object using the object's transform
        /// matrix to make a correct rectangle.
        /// </summary>
        private void CalculateBoundingRectangle()
        {
            if (texture != null)
            {
                rectangle = new Rectangle(0, 0, texture.Width, texture.Height);
                Vector2 leftTop = Vector2.Transform(new Vector2(rectangle.Left, rectangle.Top), Transform);
                Vector2 rightTop = Vector2.Transform(new Vector2(rectangle.Right, rectangle.Top), Transform);
                Vector2 leftBottom = Vector2.Transform(new Vector2(rectangle.Left, rectangle.Bottom), Transform);
                Vector2 rightBottom = Vector2.Transform(new Vector2(rectangle.Right, rectangle.Bottom), Transform);

                Vector2 min = Vector2.Min(Vector2.Min(leftTop, rightTop),
                                  Vector2.Min(leftBottom, rightBottom));
                Vector2 max = Vector2.Max(Vector2.Max(leftTop, rightTop),
                                          Vector2.Max(leftBottom, rightBottom));

                rectangle = new Rectangle((int)min.X, (int)min.Y,
                    (int)(max.X - min.X), (int)(max.Y - min.Y));
            }
        }

        public void NormalizeVelocity()
        {
            velocity.Normalize();
        }

        /// <summary>
        /// Logic to perform when the object is dying, but not dead yet
        /// </summary>
        /// <param name="gameTime">The GameTime object from the active screen</param>
        public virtual void Dying(GameTime gameTime)
        {
            if (diePercent >= 1)
                status = ObjectStatus.Dead;
            else
            {
                float dieDelta = (float)(gameTime.ElapsedGameTime.TotalMilliseconds / dieTime.TotalMilliseconds);
                diePercent += dieDelta;
            }
        }

        /// <summary>
        /// Logic to perform when the object is completely dead.
        /// </summary>
        /// <param name="gameTime">The GameTime object from the active screen</param>
        public virtual void Dead(GameTime gameTime) { }

        /// <summary>
        /// Collision logic with another GameplayObject
        /// </summary>
        /// <param name="target"></param>
        public virtual void Collision(GameplayObject target) { }

        /// <summary>
        /// Method called when you want the object to die
        /// </summary>
        public void Die()
        {
            if (status == ObjectStatus.Active)
            {
                if (dieTime != TimeSpan.Zero)
                    status = ObjectStatus.Dying;
                else
                    status = ObjectStatus.Dead;
            }
        }

        /// <summary>
        /// Draws the gameplay object to the game world.  spriteBatch.Begin HAS to be called before this.
        /// </summary>
        /// <param name="gameTime">The GameTime object from the active screen</param>
        /// <param name="spriteBatch">The spriteBatch from the GameScreen (the one you used to call .Begin</param>
        public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (spriteBatch != null)
            {
                if (texture != null)
                {
                    spriteBatch.Draw(texture, position, null, Color, rotation, Origin, 1.0f, SpriteEffects.None, 0.0f);
                }
            }
        }

        public void DrawBounds(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (spriteBatch != null)
            {
                if (bounds != null)
                    spriteBatch.Draw(bounds, rectangle, Color.White);
            }
        }
        #endregion

        public static Texture2D bounds;
    }
}