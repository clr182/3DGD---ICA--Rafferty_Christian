using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GDLibrary
{
    public class CashManager : PausableDrawableGameComponent
    {
        private float startingCash;
        private float[] money;
        private InputManagerParameters inputManagerParameters;
        private ObjectManager objectManager;
        private PickingManager pickingManager;

        public CashManager(
            Game game,
            EventDispatcher eventDispatcher,
            InputManagerParameters inputManagerParameters,
            ObjectManager objectManager,
            PickingManager pickingManager,
            StatusType statusType, 
            float startingCash
        ) : base(game, eventDispatcher, statusType)
        {
            this.objectManager = objectManager;
            this.pickingManager = pickingManager;
            this.startingCash = startingCash; 
            this.money = new float[10];
            this.inputManagerParameters = inputManagerParameters;
            divyUpCash(money);
            RegisterForEventHandling(eventDispatcher);
        }

        public float StartingCash
        {
            get
            {
                return this.startingCash;
            }
            set
            {
                this.startingCash = value;
            }
        }

        private void divyUpCash(float[] money)
        {
            money[0] = 50.00f;  //50s
            money[1] = 100.00f; //20
            money[2] = 50.00f;  //10
            money[3] = 15.00f;  //5
            money[4] = 10.00f;  //2
            money[5] = 7.00f;   //1
            money[6] = 2.50f;   //0.50
            money[7] = 2.00f;   //0.20
            money[8] = 1.00f;   //0.10
            money[9] = 0.20f;   //0.05
        }

        #region cash getters
        public float getFifties()
        {
            return money[0];
        }
        public float getTwenties()
        {
            return money[1];
        }
        public float getTens()
        {
            return money[2];
        }
        public float getFives()
        {
            return money[3];
        }
        public float getTwos()
        {
            return money[4];
        }
        public float getOnes()
        {
            return money[5];
        }
        public float getFiftyCents()
        {
            return money[6];
        }
        public float getTwentyCents()
        {
            return money[7];
        }
        public float getTenCents()
        {
            return money[8];
        }
        public float getFiveCents()
        {
            return money[9];
        }
        #endregion

        public void clearStartingCash()
        {
            this.startingCash = 0;
        }

        public void addCash(float num)
        {
              this.startingCash += num;
        }

        public void minusCash(float num)
        {
            this.startingCash -= num;
        }


        public Actor3D findByID(Predicate<Actor3D> predicate)
        {
           return this.objectManager.Find(predicate);
        }

        #region Event Handling
        protected void EventDispatcher_CashChanged(EventData eventData)
        {
            if (eventData.EventType == EventActionType.OnMoneyClicked)
            {
                addCash(20);
            }

            
        }
        protected override void RegisterForEventHandling(EventDispatcher eventDispatcher)
        {
            eventDispatcher.CashEvent += EventDispatcher_CashChanged;
            eventDispatcher.CashEvent += CallAddCash;
            base.RegisterForEventHandling(eventDispatcher);
        }
        #endregion

        private void CallAddCash(EventData eventData)
        {
            addCash(20);
        }



        private void CallMinusCash(EventData eventData)
        {
            minusCash(20);
        }


        protected override void HandleInput(GameTime gameTime)
        {
            if (this.inputManagerParameters.KeyboardManager.IsFirstKeyPress(Microsoft.Xna.Framework.Input.Keys.G))
            {
                addCash(20);
            }
            if (this.inputManagerParameters.KeyboardManager.IsFirstKeyPress(Microsoft.Xna.Framework.Input.Keys.H))
            {
                minusCash(20);
            }

            

            base.HandleInput(gameTime);
        }


        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void ApplyDraw(GameTime gameTime)
        {
            base.ApplyDraw(gameTime);
        }
    }
}
