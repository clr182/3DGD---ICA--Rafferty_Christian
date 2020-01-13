
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Text;
using System;
using System.Collections.Generic;


namespace GDLibrary
{
    public class CustomerManager : PausableDrawableGameComponent, IDisposable
    {
        #region fields
        private Queue<PrimitiveObject> customerQueue;
        private float customersCash;
        private UIProgressController uiProgressController;
        private ObjectManager object3DManager;
        private EffectParameters effectParameters;
        private ContentDictionary<Texture2D> textureDictionary;
        private Dictionary<string, EffectParameters> effectDictionary;
        private Dictionary<string, IVertexData> vertexDictionary;
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
        public ObjectManager Object3DManager
        {
            get
            {
                return this.Object3DManager;
            }
            set
            {

                this.Object3DManager = value;
            }
        }
        public Queue<PrimitiveObject> CustomerQueue
        {
            get
            {
                return this.customerQueue;
            }
            set
            {

                this.customerQueue = value;
            }
        }

        public CustomerManager(
            Game game,
            EventDispatcher eventDispatcher,
            StatusType statusType,
            Queue<PrimitiveObject> collidablePrimitiveObject,
            UIProgressController uiProgressController,
            ObjectManager object3DManager
            ) : base(game, eventDispatcher, statusType)
        {
            this.customerQueue = collidablePrimitiveObject;
            this.customersCash = setRandomCash();
            this.uiProgressController = uiProgressController;
            this.object3DManager = object3DManager;
        }

        private float setRandomCash()
        {
            float randomCash;
            Random rand = new Random();
            randomCash = rand.Next(1, 400);
            return randomCash;
        }
        

        private bool serveCustomer(GameTime gameTime, Actor3D actor3D, int queuePosition)
        {
           //queuePosition = this.customerQueue.First();
            
            if(actor3D is CollidablePrimitiveObject && queuePosition == 0)
            {
                CollidablePrimitiveObject collidee = actor3D as CollidablePrimitiveObject;

                if(collidee.ActorType == ActorType.Customer)
                {
                //    HandleCash(gameTime, collidee);
                //    return true;
                }
                else
                {

                }
            }
            return false;
        }



        public void moveQueue()
        {
           


            for(int i = 0; i < 1; i++)
            {

                //if (actor.ID == "character1")
                //{
                //    actor.Transform.Translation = new Vector3(40, 40, 40);
                //}
                if (this.customerQueue.Count != 0)
                {
                    if (this.customerQueue.First().ActorType == ActorType.Customer)
                    {
                        this.customerQueue.First().Transform.Translation = new Vector3(40, 40, 40);
                        this.customerQueue.Dequeue();
                    }
                }
                
            }

            //iterator<PrimitiveObject> = this.customerQueue.

        }


        #region event mangaging
        protected void EventDispatcher_CustomerChanged(EventData eventData)
        {
            if (eventData.EventType == EventActionType.OnCustomerChanged)
            {
                moveQueue();
                this.uiProgressController.CurrentValue = 0;
            }

        }




        protected override void RegisterForEventHandling(EventDispatcher eventDispatcher)
        {
            eventDispatcher.CustomerChanged += EventDispatcher_CustomerChanged;
            base.RegisterForEventHandling(eventDispatcher);
        }
        #endregion

        protected override void ApplyUpdate(GameTime gameTime)
        {
            base.ApplyUpdate(gameTime);
        }

        protected override void ApplyDraw(GameTime gameTime)
        {
            base.ApplyDraw(gameTime);
        }
    }
}
