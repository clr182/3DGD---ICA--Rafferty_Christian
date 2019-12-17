﻿/*
Function: 		Creates a class based on the DrawableGameComponent class that can be paused when the menu is shown.
Author: 		NMCG
Version:		1.0
Date Updated:	
Bugs:			None
Fixes:			None
*/
using Microsoft.Xna.Framework;

namespace GDLibrary
{
    public class PausableDrawableGameComponent : DrawableGameComponent
    {
        #region Fields
        private StatusType statusType;
        private EventDispatcher eventDispatcher;
        #endregion

        #region Properties 
        private EventDispatcher EventDispatcher
        {
            get
            {
                return this.eventDispatcher;
            }
            set
            {
                this.eventDispatcher = value;
            }
        }
        public StatusType StatusType
        {
            get
            {
                return this.statusType;
            }
            set
            {
                this.statusType = value;
            }
        }
        #endregion

        public PausableDrawableGameComponent(Game game, EventDispatcher eventDispatcher, StatusType statusType)
            : base(game)
        {
            //store handle to event dispatcher for event registration and de-registration
            this.eventDispatcher = eventDispatcher;

            //allows us to start the game component with drawing and/or updating paused
            this.StatusType = statusType;

            //register with the event dispatcher for the events of interest
            RegisterForEventHandling(eventDispatcher);
        }

        #region Event Handling
        protected virtual void RegisterForEventHandling(EventDispatcher eventDispatcher)
        {
            this.eventDispatcher.MenuChanged += EventDispatcher_MenuChanged;
        }

        protected virtual void EventDispatcher_MenuChanged(EventData eventData)
        {
            if (eventData.EventType == EventActionType.OnStart)
                this.statusType = StatusType.Drawn | StatusType.Update;
            else if (eventData.EventType == EventActionType.OnPause)
                this.statusType = StatusType.Off;
        }

        #endregion

        public override void Update(GameTime gameTime)
        {
            //screen manager needs to listen to input even when paused i.e. hide/show menu - see ScreenManager::HandleInput()
            HandleInput(gameTime);

            if ((this.statusType & StatusType.Update) != 0) //if update flag is set
            {
                ApplyUpdate(gameTime);
                base.Update(gameTime);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if ((this.statusType & StatusType.Drawn) != 0) //if draw flag is set
            {
                ApplyDraw(gameTime);
                base.Draw(gameTime);
            }
        }

        protected virtual void ApplyUpdate(GameTime gameTime)
        {

        }

        protected virtual void ApplyDraw(GameTime gameTime)
        {

        }

        protected virtual void HandleInput(GameTime gameTime)
        {
           // HandleMouse(gameTime);
           // HandleKeyboard(gameTime);
           // HandleGamePad(gameTime);
        }

        protected virtual void HandleMouse(GameTime gameTime)
        {

        }

        protected virtual void HandleKeyboard(GameTime gameTime)
        {

        }

        protected virtual void HandleGamePad(GameTime gameTime)
        {

        }
    }
}