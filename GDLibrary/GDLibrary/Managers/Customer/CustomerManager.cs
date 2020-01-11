using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GDLibrary
{
    public class CustomerManager : PausableDrawableGameComponent, IDisposable
    {
        #region fields
        protected Queue<CollidablePrimitiveObject> collidablePrimitiveObjects;
        protected float customersCash;
        #endregion


        public float CustomersCash
        {
            get
            {
                return this.customersCash;
            }
            set
            {

                this.customersCash = value;
            }
        }


        public CustomerManager(
            Game game,
            EventDispatcher eventDispatcher,
            StatusType statusType,
            Queue<CollidablePrimitiveObject> collidablePrimitiveObject

            ) : base(game, eventDispatcher, statusType)
        {
            this.collidablePrimitiveObjects = collidablePrimitiveObject;
            this.customersCash = setRandomCash();
        }

        public float setRandomCash()
        {
            float randomCash;
            Random rand = new Random();
            randomCash = rand.Next(1, 400);
            return randomCash;
        }
    }
}
