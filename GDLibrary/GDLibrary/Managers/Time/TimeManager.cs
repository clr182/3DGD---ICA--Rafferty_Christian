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




/*                      REFERENCE
 *   
 *  THIS CLASS WAS TAKEN FROM SHRINE TO THE DARK GOD
 *  CODED BY CAOILINN HUGHES 
 * 
*/


namespace GDLibrary
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class TimeManager : GameComponent
    {
        #region Fields
        private static bool waiting;
        private static float startTime;
        private static float duration;
        private static float timer;
        #endregion

        #region Properties

        public bool Waiting
        {
            get
            {
                return waiting;
            }
            set
            {
                waiting = value;
            }
        }

        public float StartMoveTime
        {
            get
            {
                return startTime;
            }
            set
            {
                startTime = value;
            }
        }

        public float Duration
        {
            get
            {
                return duration;
            }
            set
            {
                duration = value;
            }
        }
        public float Timer
        {
            get
            {
                return timer;
            }
            set
            {
                timer = value;
            }
        }

        #endregion

        public TimeManager(Game game)
            : base(game)
        {
            // TODO: Construct any child components here
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // TODO: Add your update code here
            TurnTimer(gameTime);
            base.Update(gameTime);
        }

        public void StartTimer(float duration)
        {
            if (!Waiting)
            {
                Waiting = true;
                Duration = duration;
            }
        }

        public virtual void TurnTimer(GameTime gameTime)
        {

            if (Waiting)
            {
                //If first time called
                if (Timer < Duration)
                {
                    //this.StartMoveTime = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    Timer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                    return;
                }

                //If move has ended
                if (Timer >= Duration)
                {
                    this.Waiting = false;
                    Timer = 0f;
                    return;
                }
                return;
            }
        }
    }
}
