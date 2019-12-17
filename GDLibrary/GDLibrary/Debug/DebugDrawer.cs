using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDLibrary
{
    public class DebugDrawer : PausableDrawableGameComponent
    {
        #region Fields
        private string strInfo = "[C] - Cycle camera; [WASD] - Move player; [TGFH] - Move Camera; [F5, F6] - You Win message";

        private SpriteBatch spriteBatch;
        private CameraManager cameraManager;
        private CameraLayoutType cameraLayoutType;
        private SpriteFont spriteFont;
        private Vector2 position;
        private Color color;
        private int fpsRate;
        private int totalTime, count;
        private Vector2 positionOffset = new Vector2(0, 20);
        #endregion

        #region Properties  
        public CameraLayoutType CameraLayoutType
        {
            get
            {
                return this.cameraLayoutType;
            }
            set
            {
                this.cameraLayoutType = value;
            }
        }
        #endregion

        public DebugDrawer(Game game, CameraManager cameraManager, 
            EventDispatcher eventDispatcher, StatusType statusType,
            CameraLayoutType cameraLayoutType,
            SpriteBatch spriteBatch, SpriteFont spriteFont, Vector2 position, Color color) 
            : base(game, eventDispatcher, statusType)
        {
            this.spriteBatch = spriteBatch;
            this.cameraManager = cameraManager;
            this.cameraLayoutType = cameraLayoutType;
            this.spriteFont = spriteFont;
            this.position = position;
            this.color = color;
        }
        #region Event Handling
        protected override void RegisterForEventHandling(EventDispatcher eventDispatcher)
        {
            eventDispatcher.DebugChanged += EventDispatcher_DebugChanged;
            base.RegisterForEventHandling(eventDispatcher);
        }

        //enable dynamic show/hide of debug info
        private void EventDispatcher_DebugChanged(EventData eventData)
        {
            if (eventData.EventType == EventActionType.OnToggle)
            {
                if (this.StatusType == StatusType.Off)
                    this.StatusType = StatusType.Drawn | StatusType.Update;
                else
                    this.StatusType = StatusType.Off;
            }
        }
        #endregion

        protected override void ApplyUpdate(GameTime gameTime)
        {
            this.totalTime += gameTime.ElapsedGameTime.Milliseconds;
            this.count++;

            if(this.totalTime >= 1000) //1 second
            {
                this.fpsRate = count;
                this.totalTime = 0;
                this.count = 0;
            }

            base.ApplyUpdate(gameTime);
        }

        protected override void ApplyDraw(GameTime gameTime)
        {
            if (this.cameraLayoutType == CameraLayoutType.Single)
                ApplySingleCameraDraw(gameTime, this.cameraManager.ActiveCamera);
            else
                ApplyMultiCameraDraw(gameTime);

           

            base.ApplyDraw(gameTime);
        }

        private void ApplyMultiCameraDraw(GameTime gameTime)
        {
            //draw the debug info for all of the cameras in the cameramanager
            foreach (Camera3D activeCamera in this.cameraManager)
            {
                ApplySingleCameraDraw(gameTime, activeCamera);
            }
        }

        private void ApplySingleCameraDraw(GameTime gameTime, Camera3D activeCamera)
        {
            //set the viewport dimensions to the size defined by the active camera
            Game.GraphicsDevice.Viewport = activeCamera.Viewport;
            this.spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, null, DepthStencilState.Default, RasterizerState.CullCounterClockwise);
            //frame rate
            this.spriteBatch.DrawString(this.spriteFont, "FPS: " + this.fpsRate, this.position, this.color);
            //camera info
            this.spriteBatch.DrawString(this.spriteFont, activeCamera.GetDescription(), this.position + this.positionOffset, this.color);
            //str info
            this.spriteBatch.DrawString(this.spriteFont, this.strInfo, this.position + 2 * this.positionOffset, this.color);
            this.spriteBatch.End();
        }
    }
}
