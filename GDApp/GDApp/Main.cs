using GDLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace GDApp
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Main : Microsoft.Xna.Framework.Game
    {
        #region Fields

        //Graphics
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        private Integer2 resolution;
        private Integer2 screenCentre;

        //Camera
        private CameraLayoutType cameraLayoutType;
        private ScreenLayoutType screenLayoutType;

        //Dictionaries
        private ContentDictionary<Model> modelDictionary;
        private ContentDictionary<Texture2D> textureDictionary;
        private ContentDictionary<SpriteFont> fontDictionary;
        private Dictionary<string, RailParameters> railDictionary;
        private Dictionary<string, Track3D> track3DDictionary;
        private Dictionary<string, EffectParameters> effectDictionary;
        private Dictionary<string, IVertexData> vertexDictionary;
        private Dictionary<string, DrawnActor3D> objectArchetypeDictionary;

        //Event Dispatcher
        private EventDispatcher eventDispatcher;

        //Managers
        private CameraManager cameraManager;
        private ObjectManager object3DManager;
        private KeyboardManager keyboardManager;
        private MouseManager mouseManager;
        private InputManagerParameters inputManagerParameters;
        private PickingManager pickingManager;
        private SoundManager soundManager;
        private MyMenuManager menuManager;
        private UIManager uiManager;
        private CustomerManager customerManager;
        private CashManager cashManager;

        //Misc
        private Queue<CollidablePrimitiveObject> customers = new Queue<CollidablePrimitiveObject>();
        private Vector2[] arrOfClippedNumTexPoints = new Vector2[8];
        private float cashRegisterFloat = 123770;
        private IActor drivableModelObject;
        private List<CollidablePrimitiveObject> moneyList= new List<CollidablePrimitiveObject>();
        #endregion

        #region Constructors
        public Main()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }
        #endregion

        #region Initialization
        protected override void Initialize()
        {
            Window.Title = "Custodian of Capital";

            this.cameraLayoutType = CameraLayoutType.Multi;
            this.screenLayoutType = ScreenLayoutType.MultiFullCycle;

            #region Assets & Dictionaries
            InitializeDictionaries();
            #endregion

            #region Graphics Related
            spriteBatch = new SpriteBatch(GraphicsDevice);
            this.resolution = ScreenUtility.HD720;
            this.screenCentre = this.resolution / 3;
            InitializeGraphics();
            InitializeEffects();
            #endregion

            #region Event Handling
            //add the component to handle all system events
            this.eventDispatcher = new EventDispatcher(this, 20);
            Components.Add(this.eventDispatcher);
            #endregion

            #region Assets
            LoadAssets();
            #endregion

            #region Initialize Managers
            InitializeManagers();
            #endregion

            #region Load Game
            //load game happens before cameras are loaded because we may add a third person camera that needs a reference to a loaded Actor
            int worldScale = 1250;
            int gameLevel = 1;
            LoadGame(worldScale, gameLevel);
            #endregion

            #region Cameras
            InitializeCameras();
            #endregion

            #region customers
            initializeCustomers();
            #endregion

            #region cash
            InitializeNumberBlocks();
            #endregion

            #region Menu & UI
            InitializeMenu();
            //since debug needs sprite batch then call here
            InitializeUI();
            #endregion

            #region debug stuff
#if DEBUG
            InitializeDebug(true);
            bool bShowCDCRSurfaces = true;
            bool bShowZones = true;
            InitializeDebugCollisionSkinInfo(bShowCDCRSurfaces, bShowZones);
#endif

            //Publish Start Event(s)
            StartGame();
            base.Initialize();
        }
        #endregion

        private void StartGame()
        {
            //will be received by the menu manager and screen manager and set the menu to be shown and game to be paused
            EventDispatcher.Publish(new EventData(EventActionType.OnPause, EventCategoryType.Menu));

            //publish an event to set the camera
            object[] additionalEventParamsB = { AppData.CameraIDCollidableFirstPerson };
            EventDispatcher.Publish(new EventData(EventActionType.OnCameraSetActive, EventCategoryType.Camera, additionalEventParamsB)); 
        }

        private void InitializeManagers()
        {
            #region input manager
            //Keyboard
            this.keyboardManager = new KeyboardManager(this);
            Components.Add(this.keyboardManager);
            
            //mouse
            bool bMouseVisible = true;
            this.mouseManager = new MouseManager(this, bMouseVisible);
            this.mouseManager.SetPosition(this.screenCentre);
            Components.Add(this.mouseManager);
            
            //bundle together for easy passing
            this.inputManagerParameters = new InputManagerParameters(this.mouseManager, this.keyboardManager);
            #endregion

            #region camera manager
            //this is a list that updates all cameras
            this.cameraManager = new CameraManager(this, 5, this.eventDispatcher, StatusType.Off);
            Components.Add(this.cameraManager);
            #endregion

            #region 3d object manager
            //Object3D
            this.object3DManager = new ObjectManager(this, this.cameraManager,
                this.eventDispatcher, StatusType.Off, this.cameraLayoutType);
            this.object3DManager.DrawOrder = 1;
            Components.Add(this.object3DManager);
            #endregion

            #region audio manager
            //Sound
            this.soundManager = new SoundManager(this, this.eventDispatcher, StatusType.Update, "Content/Assets/Audio/", "Demo2DSound.xgs", "WaveBank1.xwb", "SoundBank1.xsb");
            Components.Add(this.soundManager);
            #endregion

            #region UI & menu manager
            //Menu
            this.menuManager = new MyMenuManager(this, this.inputManagerParameters,
                this.cameraManager, this.spriteBatch, this.eventDispatcher,
                StatusType.Drawn | StatusType.Update);
            this.menuManager.DrawOrder = 2;
            Components.Add(this.menuManager);

            //ui (e.g. reticule, inventory, progress)
            this.uiManager = new UIManager(this, this.spriteBatch, this.eventDispatcher, 10, StatusType.Off);
            this.uiManager.DrawOrder = 3;
            Components.Add(this.uiManager);
            #endregion

            #region picking manager
            this.pickingManager = new PickingManager(this, this.eventDispatcher, StatusType.Update,
               this.inputManagerParameters, this.cameraManager, this.object3DManager, PickingBehaviourType.PickOnly);
            Components.Add(this.pickingManager);
            #endregion

            #region Customer manager
            this.customerManager = new CustomerManager(this, this.eventDispatcher, StatusType.Drawn,
                this.customers);
            Components.Add(this.customerManager);
            //InitializeNumberBlocks();
            #endregion

            #region cash manger
            this.cashManager = new CashManager(this, this.eventDispatcher, this.inputManagerParameters, this.object3DManager, this.pickingManager, StatusType.Update, 
                this.cashRegisterFloat);
            Components.Add(this.cashManager);

           
            #endregion
        }

        private void initializeCustomers()
        {
            spawnAmountOfCustomers(5);
        }



        private void spawnAmountOfCustomers(int amount)
        {
            Transform3D transform;
            CollidablePrimitiveObject texturedPrimitiveObject;
            PrimitiveObject cloneTexturedPrimitiveObject = null;

            int X = 30;
            int Y = 8;
            int Z = 0;
            int RandX, RandY, RandZ, RandTextureNo;
            
            transform = new Transform3D(new Vector3(X, Y, Z), new Vector3(4, 20, 14));
            texturedPrimitiveObject = CreateTexturedBox(transform, "character" + 1);
            this.object3DManager.Add(texturedPrimitiveObject);
            this.customers.Enqueue(texturedPrimitiveObject);

            Random randY = new Random();
            Random randZ = new Random();
            Random randTextureNo = new Random();

            for (int i = 0; i < amount; i++)
            {
                RandY = randY.Next(10, 30);
                RandZ = randY.Next(5, 25);
                RandTextureNo = randTextureNo.Next(1, 10);
                X += 10;

                cloneTexturedPrimitiveObject = texturedPrimitiveObject.Clone() as PrimitiveObject;
                cloneTexturedPrimitiveObject.Transform.Translation = new Vector3(X, Y, Z);
                cloneTexturedPrimitiveObject.Transform.Scale = new Vector3(4, RandY, RandZ);
                cloneTexturedPrimitiveObject.EffectParameters.Texture = this.textureDictionary["character" + RandTextureNo];
                cloneTexturedPrimitiveObject.ID = i + " customer";

                this.object3DManager.Add(cloneTexturedPrimitiveObject);
                this.customers.Enqueue(texturedPrimitiveObject);
            }
        }

        private void InitializeUI()
        {
            InitializeUIMouse();
            InitializeUIProgress();
        }

        private void InitializeUIProgress()
        {
            float separation = 20; //spacing between progress bars

            Transform2D transform = null;
            Texture2D texture = null;
            UITextureObject textureObject = null;
            Vector2 position = Vector2.Zero;
            Vector2 scale = Vector2.Zero;
            float verticalOffset = 20;
            int startValue;

            texture = this.textureDictionary["progress_gradient"];
            scale = new Vector2(1, 0.75f);

            #region Player 1 Progress Bar
            position = new Vector2(graphics.PreferredBackBufferWidth / 2.0f - texture.Width * scale.X - separation, verticalOffset);
            transform = new Transform2D(position, 0, scale,
                Vector2.Zero, /*new Vector2(texture.Width/2.0f, texture.Height/2.0f),*/
                new Integer2(texture.Width, texture.Height));

            textureObject = new UITextureObject(AppData.PlayerOneProgressID,
                    ActorType.UITexture,
                    StatusType.Drawn | StatusType.Update,
                    transform, Color.Green,
                    SpriteEffects.None,
                    1,
                    texture);

            //add a controller which listens for pickupeventdata send when the player (or red box) collects the box on the left
            startValue = 0; //just a random number between 0 and max to demonstrate we can set initial progress value
            textureObject.AttachController(
                new UIProgressController(AppData.PlayerOneProgressControllerID, 
                ControllerType.UIProgress, startValue, 10, this.eventDispatcher));

            textureObject.AttachController(
                new UIProgressIncrementController("bla",
                ControllerType.UIProgressIncrement,
                PlayStatusType.Play,
                AppData.PlayerOneProgressControllerID, //send an event to this controller ID
                1000, //1 sec between update
                1)); //add 1 every 1 sec

            this.uiManager.Add(textureObject);
            #endregion


            #region Player 2 Progress Bar
            position = new Vector2(graphics.PreferredBackBufferWidth / 2.0f + separation, verticalOffset);
            transform = new Transform2D(position, 0, scale, Vector2.Zero, new Integer2(texture.Width, texture.Height));

            textureObject = new UITextureObject(AppData.PlayerTwoProgressID,
                    ActorType.UITexture,
                    StatusType.Drawn | StatusType.Update,
                    transform,
                    Color.Red,
                    SpriteEffects.None,
                    1,
                    texture);

            //add a controller which listens for pickupeventdata send when the player (or red box) collects the box on the left
            startValue = 4; //just a random number between 0 and max to demonstrate we can set initial progress value
            textureObject.AttachController(new UIProgressController(AppData.PlayerTwoProgressControllerID, ControllerType.UIProgress, startValue, 10, this.eventDispatcher));
            this.uiManager.Add(textureObject);
            #endregion
        }

        private void InitializeUIMouse()
        {
            Texture2D texture = this.textureDictionary["reticuleDefault"];
            //show complete texture
            Microsoft.Xna.Framework.Rectangle sourceRectangle 
            = new Microsoft.Xna.Framework.Rectangle(0, 0, texture.Width, texture.Height);

            //listens for object picking events from the object picking manager
            UIPickingMouseObject myUIMouseObject = 
                new UIPickingMouseObject("picking mouseObject",
                ActorType.UITexture,
                new Transform2D(Vector2.One),
                this.fontDictionary["mouse"],
                "",
                new Vector2(0, 40),
                texture,
                this.mouseManager,
                this.eventDispatcher);
                this.uiManager.Add(myUIMouseObject);
        }

        public void initializeTVScreen()
        {
            string strText = cashManager.StartingCash.ToString();
            SpriteFont strFont = this.fontDictionary["menu"];
            Vector2 strDim = strFont.MeasureString(strText);
            strDim /= 2.0f;

            Transform2D transform = new Transform2D(
                (Vector2)this.screenCentre,
                0, new Vector2(1, 1),
                strDim,
                new Integer2(1, 1));

            UITextObject newTextObj = new UITextObject(
                "cost", ActorType.UIText,
                StatusType.Drawn | StatusType.Update,
                transform,
                Color.Red,
                SpriteEffects.None,
                0,
                strText,
                strFont
               );

            
            EventDispatcher.Publish(new EventData(
                    "",
                    newTextObj, //handle to "win!"
                    EventActionType.OnAddActor2D,
                    EventCategoryType.SystemAdd));
        }
        #endregion


        public void InitializeNumberBlocks()
        {
            PrimitiveObject primitiveObject = null;
            Transform3D transform = null;

            string[] NumberTextures = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };

            //set transform
            Vector3 translation = new Vector3(12.8f, 8, -1.35f);
            Vector3 scale = new Vector3(.5f, 1, 1);
            transform = new Transform3D(translation, scale);

            primitiveObject = new PrimitiveObject(
               "tex quad",
               ActorType.Decorator,
               transform,
               this.effectDictionary[AppData.UnlitTexturedEffectID].Clone() as EffectParameters,
               StatusType.Update,
               this.vertexDictionary[AppData.UnlitTexturedQuadVertexDataID]);

            primitiveObject.EffectParameters.Alpha = 1;
            //primitiveObject.EffectParameters.Texture = this.textureDictionary["Zero"];
            primitiveObject.Transform.Rotation = new Vector3(0, -90, 0);
            primitiveObject.StatusType = StatusType.Drawn;
            primitiveObject.ID = "ZeroBlock";
            this.object3DManager.Add(primitiveObject);

            PrimitiveObject cloneTexturedPrimitiveObject = null;

            cloneTexturedPrimitiveObject = primitiveObject.Clone() as PrimitiveObject;
            cloneTexturedPrimitiveObject.Transform.Translation = new Vector3(12.8f, 8, translation.Z + 0.5f);
            //cloneTexturedPrimitiveObject.EffectParameters.Texture = this.textureDictionary["Zero"];
            cloneTexturedPrimitiveObject.StatusType = StatusType.Update | StatusType.Drawn;
            cloneTexturedPrimitiveObject.ID = "OneBlock";
            this.object3DManager.Add(cloneTexturedPrimitiveObject);

            cloneTexturedPrimitiveObject = primitiveObject.Clone() as PrimitiveObject;
            cloneTexturedPrimitiveObject.Transform.Translation = new Vector3(12.8f, 8, translation.Z + 1f);
            //cloneTexturedPrimitiveObject.EffectParameters.Texture = this.textureDictionary["Zero"];
            cloneTexturedPrimitiveObject.StatusType = StatusType.Update | StatusType.Drawn;
            cloneTexturedPrimitiveObject.ID = "TwoBlock";
            this.object3DManager.Add(cloneTexturedPrimitiveObject);

            cloneTexturedPrimitiveObject = primitiveObject.Clone() as PrimitiveObject;
            cloneTexturedPrimitiveObject.Transform.Translation = new Vector3(12.8f, 8, translation.Z + 1.5f);
            cloneTexturedPrimitiveObject.EffectParameters.Texture = this.textureDictionary["Dot"];
            cloneTexturedPrimitiveObject.StatusType = StatusType.Update | StatusType.Drawn;
            cloneTexturedPrimitiveObject.ID = "Dot";
            this.object3DManager.Add(cloneTexturedPrimitiveObject);

            cloneTexturedPrimitiveObject = primitiveObject.Clone() as PrimitiveObject;
            cloneTexturedPrimitiveObject.Transform.Translation = new Vector3(12.8f, 8, translation.Z + 2f);
            //cloneTexturedPrimitiveObject.EffectParameters.Texture = this.textureDictionary["Zero"];
            cloneTexturedPrimitiveObject.StatusType = StatusType.Update | StatusType.Drawn;
            cloneTexturedPrimitiveObject.ID = "ThreeBlock";
            this.object3DManager.Add(cloneTexturedPrimitiveObject);

            cloneTexturedPrimitiveObject = primitiveObject.Clone() as PrimitiveObject;
            cloneTexturedPrimitiveObject.Transform.Translation = new Vector3(12.8f, 8, translation.Z + 2.5f);
            //cloneTexturedPrimitiveObject.EffectParameters.Texture = this.textureDictionary["Zero"];
            cloneTexturedPrimitiveObject.StatusType = StatusType.Update | StatusType.Drawn;
            cloneTexturedPrimitiveObject.ID = "FourBlock";
            
            this.object3DManager.Add(cloneTexturedPrimitiveObject);
        }


        private void InitializeNumberSystem()
        {
            string[] NumberTextures = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
            string[] theCoolerNumberTextures = updateCounterTextures(NumberTextures, this.cashManager.StartingCash);
            PrimitiveObject primitiveObject = null;
            
            Predicate<Actor3D> prediZero = obj => obj.ID == "ZeroBlock";
            Predicate<Actor3D> prediOne = obj => obj.ID == "OneBlock";
            Predicate<Actor3D> prediTwo = obj => obj.ID == "TwoBlock";
            Predicate<Actor3D> prediThree = obj => obj.ID == "ThreeBlock";
            Predicate<Actor3D> prediFour = obj => obj.ID == "FourBlock";

            if (prediZero != null)
            {
                primitiveObject = this.cashManager.findByID(prediZero) as PrimitiveObject;
                primitiveObject.EffectParameters.Texture = this.textureDictionary[theCoolerNumberTextures[0]];

                primitiveObject = this.cashManager.findByID(prediOne) as PrimitiveObject;
                primitiveObject.EffectParameters.Texture = this.textureDictionary[theCoolerNumberTextures[1]];

                primitiveObject = this.cashManager.findByID(prediTwo) as PrimitiveObject;
                primitiveObject.EffectParameters.Texture = this.textureDictionary[theCoolerNumberTextures[2]];

                primitiveObject = this.cashManager.findByID(prediThree) as PrimitiveObject;
                primitiveObject.EffectParameters.Texture = this.textureDictionary[theCoolerNumberTextures[3]];

                primitiveObject = this.cashManager.findByID(prediFour) as PrimitiveObject;
                primitiveObject.EffectParameters.Texture = this.textureDictionary[theCoolerNumberTextures[4]];
            }
        }
           
        
        public string[] updateCounterTextures(string[] NumberTextures, float cashRegisterFloat)
        {
            //convert cash float to a string so I can convert to a chararr to split it back up again into a number to update the texture
            
            string numAsString = cashRegisterFloat.ToString();
            char[] charArr = numAsString.ToCharArray();
            string[] temp = new string[charArr.Length];
            //index out of range unless I convert the char to a string first then parse to an int ... weird stuff but idc it works
            int firstNum = int.Parse(charArr[0].ToString());
            int secondNum = int.Parse(charArr[1].ToString());
            int thirdNum = int.Parse(charArr[2].ToString());
            int fourthNum = int.Parse(charArr[3].ToString());
            int fifthNum = int.Parse(charArr[4].ToString());
            int sixthNum = int.Parse(charArr[5].ToString());


            temp[0] = NumberTextures[firstNum];
            temp[0] = NumberTextures[secondNum];
            temp[1] = NumberTextures[thirdNum];
            temp[2] = NumberTextures[fourthNum];
            temp[3] = NumberTextures[fifthNum];
            temp[4] = NumberTextures[sixthNum];

            return temp;
        }




            #region Load Game Content
            //load the contents for the level specified
            private void LoadGame(int worldScale, int gameLevel)
        {
            //remove anything from the last time LoadGame() may have been called
            this.object3DManager.Clear();
      
            if (gameLevel == 1)
            {
                //non-collidable
                InitializeSkyBox(worldScale);
                InitializeNonCollidableGround(worldScale);
                
                initializeTVScreen();
                //collidable
                InitializeCollidableProps();
                //demo of loading from a level image
                LoadObjectsFromImageFile("level1", 2, 2, 2.5f, new Vector3(-100, 0, 0));

                //collidable zones
                InitializeCollidableZones();

            }
            else if (gameLevel == 2)
            {
                //add different things for your next level
            }
        }


        #region Non-Collidable Primitive Objects
        private void InitializeSkyBox(int worldScale)
        {
            PrimitiveObject archTexturedPrimitiveObject = null, cloneTexturedPrimitiveObject = null;

            #region Archetype
            //we need to do an "as" typecast since the dictionary holds DrawnActor3D types
            archTexturedPrimitiveObject = this.objectArchetypeDictionary[AppData.UnlitTexturedQuadArchetypeID] as PrimitiveObject;
            archTexturedPrimitiveObject.Transform.Scale *= new Vector3(150, 100, 100);
            #endregion
            //demonstrates how we can simply clone an archetypal primitive object and re-use by re-cloning
            



            #region ring ropes
            ////back
            //cloneTexturedPrimitiveObject = archTexturedPrimitiveObject.Clone() as PrimitiveObject;
            //cloneTexturedPrimitiveObject.ID = "skybox_back";
            //cloneTexturedPrimitiveObject.Transform.Translation = new Vector3(0, 50, -75);
            //cloneTexturedPrimitiveObject.EffectParameters.Texture = this.textureDictionary["skybox_back"];
            //this.object3DManager.Add(cloneTexturedPrimitiveObject);
            //
            ////left
            //cloneTexturedPrimitiveObject = archTexturedPrimitiveObject.Clone() as PrimitiveObject;
            //cloneTexturedPrimitiveObject.ID = "skybox_left";
            //cloneTexturedPrimitiveObject.Transform.Translation = new Vector3(-75, 50, 0);
            //cloneTexturedPrimitiveObject.Transform.Rotation = new Vector3(0, 90, 0);
            //cloneTexturedPrimitiveObject.EffectParameters.Texture = this.textureDictionary["skybox_left"];
            //this.object3DManager.Add(cloneTexturedPrimitiveObject);
            //
            ////right
            //cloneTexturedPrimitiveObject = archTexturedPrimitiveObject.Clone() as PrimitiveObject;
            //cloneTexturedPrimitiveObject.ID = "skybox_right";
            //cloneTexturedPrimitiveObject.Transform.Translation = new Vector3(75, 50, 0);
            //cloneTexturedPrimitiveObject.Transform.Rotation = new Vector3(00, -90, 0);
            //cloneTexturedPrimitiveObject.EffectParameters.Texture = this.textureDictionary["skybox_right"];
            //this.object3DManager.Add(cloneTexturedPrimitiveObject);
            //
            ////front
            //cloneTexturedPrimitiveObject = archTexturedPrimitiveObject.Clone() as PrimitiveObject;
            //cloneTexturedPrimitiveObject.ID = "skybox_front";
            //cloneTexturedPrimitiveObject.Transform.Translation = new Vector3(0, 50, 75);
            //cloneTexturedPrimitiveObject.Transform.Rotation = new Vector3(0, 180, 0);
            //cloneTexturedPrimitiveObject.EffectParameters.Texture = this.textureDictionary["skybox_front"];
            //this.object3DManager.Add(cloneTexturedPrimitiveObject);

            #endregion 
        }

        private void InitializeNonCollidableGround(int worldScale)
        {
            Vector3 worldSize = new Vector3(150,150,150);
            Transform3D transform = new Transform3D(new Vector3(0, 0, 0), new Vector3(-90, 0, 0), worldSize,
              Vector3.UnitZ, Vector3.UnitY);

            EffectParameters effectParameters = this.effectDictionary[AppData.UnlitTexturedEffectID].Clone() as EffectParameters;
            effectParameters.Texture = this.textureDictionary["grass1"];

            PrimitiveObject primitiveObject = new PrimitiveObject("ground", ActorType.Helper,
                    transform,
                    effectParameters,
                    StatusType.Drawn | StatusType.Update,
                    this.vertexDictionary[AppData.UnlitTexturedQuadVertexDataID]);

            this.object3DManager.Add(primitiveObject);
        }

        private void LoadObjectsFromImageFile(string fileName, float scaleX, float scaleZ, float height, Vector3 offset)
        {
            LevelLoader levelLoader = new LevelLoader(this.objectArchetypeDictionary, this.textureDictionary);
            //List<DrawnActor3D> actorList = levelLoader.Load(this.textureDictionary[fileName],
            //   scaleX, scaleZ, height, offset);

           // this.object3DManager.Add(actorList);
        }
        #endregion




        #region Collidable Primitive Objects
        private void InitializeCollidableProps()
        {
            CollidablePrimitiveObject texturedPrimitiveObject = null;
            Transform3D transform = null;

            #region desk
            transform = new Transform3D(new Vector3(20, 5, 00), new Vector3(8, 8, 25));
            texturedPrimitiveObject = CreateTexturedBox(transform, "desk1");
            this.object3DManager.Add(texturedPrimitiveObject);

            transform = new Transform3D(new Vector3(13, 4, 0), new Vector3(8, 4, 25));
            texturedPrimitiveObject = CreateTexturedBox(transform, "desk2");
            this.object3DManager.Add(texturedPrimitiveObject);

            transform = new Transform3D(new Vector3(12, 5, 17), new Vector3(25, 8, 8));
            texturedPrimitiveObject = CreateTexturedBox(transform, "desk1");
            this.object3DManager.Add(texturedPrimitiveObject);

            transform = new Transform3D(new Vector3(12, 5, -17), new Vector3(25, 8, 8));
            texturedPrimitiveObject = CreateTexturedBox(transform, "desk1");
            this.object3DManager.Add(texturedPrimitiveObject);
            #endregion
            
            #region desk objects
            //pc
            transform = new Transform3D(new Vector3(13, 8, 0), new Vector3(.2f, 4, 4));
            texturedPrimitiveObject = CreateTexturedBox(transform, "tele");
            this.object3DManager.Add(texturedPrimitiveObject);

            //cash
            transform = new Transform3D(new Vector3(13, 6.5f, 5), new Vector3(3, 1, 1.5f));
            texturedPrimitiveObject = CreateTexturedBox(transform, "cashbox");
            this.object3DManager.Add(texturedPrimitiveObject);

            //coins
            transform = new Transform3D(new Vector3(13, 6.5f, -5), new Vector3(0, 0, 45), new Vector3(3, 1, 1.5f), Vector3.Zero, Vector3.Zero);
            texturedPrimitiveObject = CreateTexturedBox(transform, "cashbox");
            this.object3DManager.Add(texturedPrimitiveObject);
            #endregion

            #region cash
            CollidablePrimitiveObject fiver = null;
            transform = new Transform3D(new Vector3(12, 7f, 5), new Vector3(30, 90, 0), new Vector3(1.5f, .75f, .02f), Vector3.Zero, Vector3.Zero);
            fiver = CreateTexturedBox(transform, "fiver");
            this.object3DManager.Add(fiver);
            this.moneyList.Add(fiver);

            CollidablePrimitiveObject tenner = null;
            transform = new Transform3D(new Vector3(12.5f, 7f, 5), new Vector3(30, 90, 0), new Vector3(1.5f, .75f, .02f), Vector3.Zero, Vector3.Zero);
            tenner = CreateTexturedBox(transform, "tenner");
            this.object3DManager.Add(tenner);
            this.moneyList.Add(tenner);

            CollidablePrimitiveObject twenty = null;
            transform = new Transform3D(new Vector3(13f, 7f, 5), new Vector3(30, 90, 0), new Vector3(1.5f, .75f, .02f), Vector3.Zero, Vector3.Zero);
            twenty = CreateTexturedBox(transform, "twenny");
            this.object3DManager.Add(twenty);
            this.moneyList.Add(twenty);

            CollidablePrimitiveObject fifty = null;
            transform = new Transform3D(new Vector3(13.5f, 7f, 5), new Vector3(30, 90,0 ), new Vector3(1.5f, .75f, .02f), Vector3.Zero, Vector3.Zero);
            fifty = CreateTexturedBox(transform, "fiddy");
            fifty.ActorType = ActorType.Fifty;
            this.object3DManager.Add(fifty);
            this.moneyList.Add(fifty);
            #endregion

            #region coin

            #endregion
        }




        private CollidablePrimitiveObject CreateTexturedBox(Transform3D transform3d, string textureFromDictionary)
        {
            CollidablePrimitiveObject texturedPrimitiveObject = null;
            EffectParameters effectParameters = this.effectDictionary[AppData.UnlitTexturedEffectID].Clone() as EffectParameters;
            effectParameters.Texture = this.textureDictionary[textureFromDictionary];
            effectParameters.DiffuseColor = Color.White;
            effectParameters.Alpha = 1;

            BoxCollisionPrimitive collisionPrimitive = new BoxCollisionPrimitive();

            texturedPrimitiveObject = new CollidablePrimitiveObject("collidable lit cube ",
                //this is important as it will determine how we filter collisions in our collidable player CDCR code
                ActorType.CollidableArchitecture,
                transform3d,
                effectParameters,
                StatusType.Drawn | StatusType.Update,
                this.vertexDictionary[AppData.UnlitTexturedCubeVertexDataID],
                collisionPrimitive, this.object3DManager);
            
            return texturedPrimitiveObject;
        }
        #endregion




        #region Collidable Zone Objects
        private void InitializeCollidableZones()
        {
            Transform3D transform = null;
            SimpleZoneObject simpleZoneObject = null;
            ICollisionPrimitive collisionPrimitive = null;

            transform = new Transform3D(new Vector3(-20, 8, 40), 8 * Vector3.One);

            collisionPrimitive = new BoxCollisionPrimitive();

            simpleZoneObject = new SimpleZoneObject("camera trigger zone 1", ActorType.CollidableZone, transform,
                StatusType.Drawn | StatusType.Update, collisionPrimitive);

            this.object3DManager.Add(simpleZoneObject);

        }
        #endregion
        #endregion

        #region menu
        private void InitializeMenu()
        {
            Transform2D transform = null;
            Texture2D texture = null;
            Vector2 position = Vector2.Zero;
            UIButtonObject uiButtonObject = null, clone = null;
            string sceneID = "", buttonID = "", buttonText = "";
            int verticalBtnSeparation = 50;

            #region Main Menu
            sceneID = AppData.MenuMainID;

            //retrieve the background texture
            texture = this.textureDictionary["mainmenu"];
            //scale the texture to fit the entire screen
            Vector2 scale = new Vector2((float)graphics.PreferredBackBufferWidth / texture.Width,
                (float)graphics.PreferredBackBufferHeight / texture.Height);
            transform = new Transform2D(scale);

            this.menuManager.Add(sceneID, new UITextureObject("mainmenuTexture", ActorType.UITexture,
                StatusType.Drawn, //notice we dont need to update a static texture
                transform, Color.White, SpriteEffects.None,
                1, //depth is 1 so its always sorted to the back of other menu elements
                texture));

            //add start button
            buttonID = "startbtn";
            buttonText = "Start";
            position = new Vector2(graphics.PreferredBackBufferWidth / 2.0f, 200);
            texture = this.textureDictionary["genericbtn"];
            transform = new Transform2D(position,
                0, new Vector2(1.8f, 0.6f),
                new Vector2(texture.Width / 2.0f, texture.Height / 2.0f), new Integer2(texture.Width, texture.Height));

            uiButtonObject = new UIButtonObject(buttonID, ActorType.UIButton, StatusType.Update | StatusType.Drawn,
                transform, Color.LightPink, SpriteEffects.None, 0.1f, texture, buttonText,
                this.fontDictionary["menu"],
                Color.DarkGray, new Vector2(0, 2));
            this.menuManager.Add(sceneID, uiButtonObject);

            //add audio button - clone the audio button then just reset texture, ids etc in all the clones
            clone = (UIButtonObject)uiButtonObject.Clone();
            clone.ID = "audiobtn";
            clone.Text = "Audio";
            //move down on Y-axis for next button
            clone.Transform.Translation += new Vector2(0, verticalBtnSeparation);
            //change the texture blend color
            clone.Color = Color.LightGreen;
            this.menuManager.Add(sceneID, clone);

            //add controls button - clone the audio button then just reset texture, ids etc in all the clones
            clone = (UIButtonObject)uiButtonObject.Clone();
            clone.ID = "controlsbtn";
            clone.Text = "Controls";
            //move down on Y-axis for next button
            clone.Transform.Translation += new Vector2(0, 2 * verticalBtnSeparation);
            //change the texture blend color
            clone.Color = Color.LightBlue;
            this.menuManager.Add(sceneID, clone);

            //add exit button - clone the audio button then just reset texture, ids etc in all the clones
            clone = (UIButtonObject)uiButtonObject.Clone();
            clone.ID = "exitbtn";
            clone.Text = "Exit";
            //move down on Y-axis for next button
            clone.Transform.Translation += new Vector2(0, 3 * verticalBtnSeparation);
            //change the texture blend color
            clone.Color = Color.LightYellow;
            //store the original color since if we modify with a controller and need to reset
            clone.OriginalColor = clone.Color;
            this.menuManager.Add(sceneID, clone);
            #endregion

            #region Audio Menu
            sceneID = AppData.MenuAudioID;

            //retrieve the audio menu background texture
            texture = this.textureDictionary["audiomenu"];
            //scale the texture to fit the entire screen
            scale = new Vector2((float)graphics.PreferredBackBufferWidth / texture.Width,
                (float)graphics.PreferredBackBufferHeight / texture.Height);
            transform = new Transform2D(scale);
            this.menuManager.Add(sceneID, new UITextureObject("audiomenuTexture", 
                ActorType.UITexture,
                StatusType.Drawn, //notice we dont need to update a static texture
                transform, Color.White, SpriteEffects.None,
                1, //depth is 1 so its always sorted to the back of other menu elements
                texture));


            //add volume up button - clone the audio button then just reset texture, ids etc in all the clones
            clone = (UIButtonObject)uiButtonObject.Clone();
            clone.ID = "volumeUpbtn";
            clone.Text = "Volume Up";
            //change the texture blend color
            clone.Color = Color.LightPink;
            this.menuManager.Add(sceneID, clone);

            //add volume down button - clone the audio button then just reset texture, ids etc in all the clones
            clone = (UIButtonObject)uiButtonObject.Clone();
            //move down on Y-axis for next button
            clone.Transform.Translation += new Vector2(0, verticalBtnSeparation);
            clone.ID = "volumeDownbtn";
            clone.Text = "Volume Down";
            //change the texture blend color
            clone.Color = Color.LightGreen;
            this.menuManager.Add(sceneID, clone);

            //add volume mute button - clone the audio button then just reset texture, ids etc in all the clones
            clone = (UIButtonObject)uiButtonObject.Clone();
            //move down on Y-axis for next button
            clone.Transform.Translation += new Vector2(0, 2 * verticalBtnSeparation);
            clone.ID = "volumeMutebtn";
            clone.Text = "Volume Mute";
            //change the texture blend color
            clone.Color = Color.LightBlue;
            this.menuManager.Add(sceneID, clone);

            //add volume mute button - clone the audio button then just reset texture, ids etc in all the clones
            clone = (UIButtonObject)uiButtonObject.Clone();
            //move down on Y-axis for next button
            clone.Transform.Translation += new Vector2(0, 3 * verticalBtnSeparation);
            clone.ID = "volumeUnMutebtn";
            clone.Text = "Volume Un-mute";
            //change the texture blend color
            clone.Color = Color.LightSalmon;
            this.menuManager.Add(sceneID, clone);

            //add back button - clone the audio button then just reset texture, ids etc in all the clones
            clone = (UIButtonObject)uiButtonObject.Clone();
            //move down on Y-axis for next button
            clone.Transform.Translation += new Vector2(0, 4 * verticalBtnSeparation);
            clone.ID = "backbtn";
            clone.Text = "Back";
            //change the texture blend color
            clone.Color = Color.LightYellow;
            this.menuManager.Add(sceneID, clone);
            #endregion

            #region Controls Menu
            sceneID = AppData.MenuControlsID;

            //retrieve the controls menu background texture
            texture = this.textureDictionary["controlsmenu"];
            //scale the texture to fit the entire screen
            scale = new Vector2((float)graphics.PreferredBackBufferWidth / texture.Width,
                (float)graphics.PreferredBackBufferHeight / texture.Height);
            transform = new Transform2D(scale);
            this.menuManager.Add(sceneID, new UITextureObject("controlsmenuTexture", ActorType.UITexture,
                StatusType.Drawn, //notice we dont need to update a static texture
                transform, Color.White, SpriteEffects.None,
                1, //depth is 1 so its always sorted to the back of other menu elements
                texture));

            //add back button - clone the audio button then just reset texture, ids etc in all the clones
            clone = (UIButtonObject)uiButtonObject.Clone();
            //move down on Y-axis for next button
            clone.Transform.Translation += new Vector2(0, 9 * verticalBtnSeparation);
            clone.ID = "backbtn";
            clone.Text = "Back";
            //change the texture blend color
            clone.Color = Color.LightYellow;
            this.menuManager.Add(sceneID, clone);
            #endregion
        }
        #endregion

        #region initialize Dictionaries
        private void InitializeDictionaries()
        {
            this.modelDictionary = new ContentDictionary<Model>("model dictionary", this.Content);
            this.textureDictionary = new ContentDictionary<Texture2D>("texture dictionary", this.Content);
            this.fontDictionary = new ContentDictionary<SpriteFont>("font dictionary", this.Content);       
            this.railDictionary = new Dictionary<string, RailParameters>();
            this.track3DDictionary = new Dictionary<string, Track3D>();
            this.effectDictionary = new Dictionary<string, EffectParameters>();
            this.vertexDictionary = new Dictionary<string, IVertexData>();
            this.objectArchetypeDictionary = new Dictionary<string, DrawnActor3D>();
        }
        #endregion

        #region more debug
#if DEBUG
        private void InitializeDebug(bool bEnabled)
        {
            if (bEnabled)
            {
                DebugDrawer debugDrawer = new DebugDrawer(this, this.cameraManager,
                    this.eventDispatcher,
                    StatusType.Off,
                    this.cameraLayoutType,
                    this.spriteBatch, this.fontDictionary["debugFont"], new Vector2(20, 20), Color.White);

                debugDrawer.DrawOrder = 4;
                Components.Add(debugDrawer);
            }
        }
        private void InitializeDebugCollisionSkinInfo(bool bShowCDCRSurfaces, bool bShowZones)
        {
            //draws CDCR surfaces for boxes and spheres
            PrimitiveDebugDrawer primitiveDebugDrawer = new PrimitiveDebugDrawer(this, bShowCDCRSurfaces, bShowZones, 
                this.cameraManager, this.object3DManager, this.eventDispatcher, StatusType.Drawn | StatusType.Update);
            primitiveDebugDrawer.DrawOrder = 5;
            Components.Add(primitiveDebugDrawer);

            //set color for the bounding boxes
            BoundingBoxDrawer.boundingBoxColor = Color.White;
        }
#endif
        #endregion

        #region Assets
        private void LoadAssets()
        {
            LoadTextures();
            LoadFonts();
            LoadRails();
            LoadTracks();

            LoadStandardVertices();
            LoadBillboardVertices();
            LoadArchetypePrimitivesToDictionary();
        }

        //uses BufferedVertexData to create a container for the vertices. you can also use plain VertexData but
        //it doesnt have the advantage of moving the vertices ONLY ONCE onto VRAM on the GFX card
        private void LoadStandardVertices()
        {
            PrimitiveType primitiveType;
            int primitiveCount;

            #region Factory Based Approach
            #region Textured Quad
            this.vertexDictionary.Add(AppData.UnlitTexturedQuadVertexDataID,
                new VertexData<VertexPositionColorTexture>(
                VertexFactory.GetTextureQuadVertices(out primitiveType, out primitiveCount),
                primitiveType, primitiveCount));
            #endregion

            #region Wireframe Circle


            this.vertexDictionary.Add(AppData.WireframeCircleVertexDataID, new BufferedVertexData<VertexPositionColor>(
            graphics.GraphicsDevice, VertexFactory.GetCircleVertices(2, 10, out primitiveType, out primitiveCount, OrientationType.XYAxis),
                PrimitiveType.LineStrip, primitiveCount));

            #endregion

            #region Lit Textured Cube
            this.vertexDictionary.Add(
                AppData.LitTexturedCubeVertexDataID, 
                new BufferedVertexData<VertexPositionNormalTexture>
                    (graphics.GraphicsDevice, 
                    VertexFactory.GetVerticesPositionNormalTexturedCube(1, out primitiveType, out primitiveCount
                    ),
                primitiveType, 
                primitiveCount)
                );
            #endregion

            //#region Lit wireframe Sphere
            //this.vertexDictionary.Add(AppData.LitTexturedSphereVertexDataID, new BufferedVertexData<VertexPositionColor>
            //   (graphics.GraphicsDevice,
            //   VertexFactory.GetSphereVertices(100, 40, out primitiveType, out primitiveCount), primitiveType, primitiveCount)
            //);
            //#endregion
            #endregion

            #region Old User Defines Vertices Approach
            VertexPositionColor[] verticesPositionColor = null;

            #region Textured Cube
            this.vertexDictionary.Add(AppData.UnlitTexturedCubeVertexDataID,
                new BufferedVertexData<VertexPositionColorTexture>(graphics.GraphicsDevice, VertexFactory.GetVerticesPositionTexturedCube(1, out primitiveType, out primitiveCount),
                primitiveType, primitiveCount));
            #endregion

            #region Wireframe Origin Helper
            verticesPositionColor = new VertexPositionColor[20];

            //x-axis
            verticesPositionColor[0] = new VertexPositionColor(-Vector3.UnitX, Color.DarkRed);
            verticesPositionColor[1] = new VertexPositionColor(Vector3.UnitX, Color.DarkRed);

            //y-axis
            verticesPositionColor[2] = new VertexPositionColor(-Vector3.UnitY, Color.DarkGreen);
            verticesPositionColor[3] = new VertexPositionColor(Vector3.UnitY, Color.DarkGreen);

            //z-axis
            verticesPositionColor[4] = new VertexPositionColor(-Vector3.UnitZ, Color.DarkBlue);
            verticesPositionColor[5] = new VertexPositionColor(Vector3.UnitZ, Color.DarkBlue);

            //to do - x-text , y-text, z-text
            //x label
            verticesPositionColor[6] = new VertexPositionColor(new Vector3(1.1f, 0.1f, 0), Color.DarkRed);
            verticesPositionColor[7] = new VertexPositionColor(new Vector3(1.3f, -0.1f, 0), Color.DarkRed);
            verticesPositionColor[8] = new VertexPositionColor(new Vector3(1.3f, 0.1f, 0), Color.DarkRed);
            verticesPositionColor[9] = new VertexPositionColor(new Vector3(1.1f, -0.1f, 0), Color.DarkRed);


            //y label
            verticesPositionColor[10] = new VertexPositionColor(new Vector3(-0.1f, 1.3f, 0), Color.DarkGreen);
            verticesPositionColor[11] = new VertexPositionColor(new Vector3(0, 1.2f, 0), Color.DarkGreen);
            verticesPositionColor[12] = new VertexPositionColor(new Vector3(0.1f, 1.3f, 0), Color.DarkGreen);
            verticesPositionColor[13] = new VertexPositionColor(new Vector3(-0.1f, 1.1f, 0), Color.DarkGreen);

            //z label
            verticesPositionColor[14] = new VertexPositionColor(new Vector3(0, 0.1f, 1.1f), Color.DarkBlue);
            verticesPositionColor[15] = new VertexPositionColor(new Vector3(0, 0.1f, 1.3f), Color.DarkBlue);
            verticesPositionColor[16] = new VertexPositionColor(new Vector3(0, 0.1f, 1.1f), Color.DarkBlue);
            verticesPositionColor[17] = new VertexPositionColor(new Vector3(0, -0.1f, 1.3f), Color.DarkBlue);
            verticesPositionColor[18] = new VertexPositionColor(new Vector3(0, -0.1f, 1.3f), Color.DarkBlue);
            verticesPositionColor[19] = new VertexPositionColor(new Vector3(0, -0.1f, 1.1f), Color.DarkBlue);

            this.vertexDictionary.Add(AppData.WireframeOriginHelperVertexDataID, new BufferedVertexData<VertexPositionColor>(graphics.GraphicsDevice, verticesPositionColor, Microsoft.Xna.Framework.Graphics.PrimitiveType.LineList, 10));
            #endregion

            #region Wireframe Triangle
            verticesPositionColor = new VertexPositionColor[3];
            verticesPositionColor[0] = new VertexPositionColor(new Vector3(0, 1, 0), Color.Red);
            verticesPositionColor[1] = new VertexPositionColor(new Vector3(1, 0, 0), Color.Green);
            verticesPositionColor[2] = new VertexPositionColor(new Vector3(-1, 0, 0), Color.Blue);
            this.vertexDictionary.Add(AppData.WireframeTriangleVertexDataID, new VertexData<VertexPositionColor>(verticesPositionColor, Microsoft.Xna.Framework.Graphics.PrimitiveType.TriangleStrip, 1));
            #endregion
            #endregion
        }

        private void LoadArchetypePrimitivesToDictionary()
        {
            Transform3D transform = null;
            PrimitiveObject primitiveObject = null;
            EffectParameters effectParameters = null;

            #region Textured Quad Archetype
            //remember we clone because each cube MAY need separate texture, alpha and diffuse color
            effectParameters = this.effectDictionary[AppData.UnlitTexturedEffectID].Clone() as EffectParameters;
            effectParameters.Texture = this.textureDictionary["white"];
            effectParameters.DiffuseColor = Color.White;
            effectParameters.Alpha = 1;

            transform = new Transform3D(Vector3.Zero, Vector3.Zero, Vector3.One, Vector3.UnitZ, Vector3.UnitY);
            primitiveObject = new PrimitiveObject(AppData.UnlitTexturedQuadArchetypeID, ActorType.Decorator,
                     transform, 
                     effectParameters, 
                     StatusType.Drawn | StatusType.Update,
                     this.vertexDictionary[AppData.UnlitTexturedQuadVertexDataID]); //or  we can leave texture null since we will replace it later

            this.objectArchetypeDictionary.Add(AppData.UnlitTexturedQuadArchetypeID, primitiveObject);
            #endregion

            #region Unlit Collidable Cube

            //remember we clone because each cube MAY need separate texture, alpha and diffuse color
            effectParameters = this.effectDictionary[AppData.UnlitTexturedEffectID].Clone() as EffectParameters;
            effectParameters.Texture = this.textureDictionary["white"];
            effectParameters.DiffuseColor = Color.White;
            effectParameters.Alpha = 1;

            transform = new Transform3D(Vector3.Zero, Vector3.Zero, Vector3.One, Vector3.UnitZ, Vector3.UnitY);

            //make the collision primitive - changed slightly to no longer need transform
            BoxCollisionPrimitive collisionPrimitive = new BoxCollisionPrimitive();

            //make a collidable object and pass in the primitive
            primitiveObject = new CollidablePrimitiveObject("collidable unlit cube",
                //this is important as it will determine how we filter collisions in our collidable player CDCR code
                ActorType.CollidablePickup,
                transform,
                effectParameters,
                StatusType.Drawn | StatusType.Update,
                this.vertexDictionary[AppData.UnlitTexturedCubeVertexDataID],
                collisionPrimitive, this.object3DManager);

            this.objectArchetypeDictionary.Add(AppData.UnlitTexturedCubeArchetypeID, primitiveObject);
            #endregion

            //add all the primitive archetypes that your game needs here, then you can just fetch and clone later e.g. in the LevelLoader
        }

        private void LoadBillboardVertices()
        {
            PrimitiveType primitiveType;
            int primitiveCount;
            IVertexData vertexData = null;

            #region Billboard Quad - we must use this type when creating billboards
            // get vertices for textured billboard
            VertexBillboard[] verticesBillboard = VertexFactory.GetVertexBillboard(1, out primitiveType, out primitiveCount);

            //make a vertex data object to store and draw the vertices
            vertexData = new BufferedVertexData<VertexBillboard>(this.graphics.GraphicsDevice, verticesBillboard, primitiveType, primitiveCount);

            //add to the dictionary for use by things like billboards - see InitializeBillboards()
            this.vertexDictionary.Add(AppData.UnlitTexturedBillboardVertexDataID, vertexData);
            #endregion
        }


        #region textures and file paths
        private void LoadFonts()
        {
            this.fontDictionary.Load("hudFont", "Assets/Fonts/hudFont");
            this.fontDictionary.Load("menu", "Assets/Fonts/menu");
            this.fontDictionary.Load("Assets/Fonts/mouse");
#if DEBUG
            this.fontDictionary.Load("debugFont", "Assets/Debug/Fonts/debugFont");
#endif
        }

        private void LoadTextures()
        {
            //used for archetypes
            this.textureDictionary.Load("Assets/Textures/white");

            //animated
            this.textureDictionary.Load("Assets/Textures/Animated/alarm");

            //ui
            this.textureDictionary.Load("Assets/Textures/UI/HUD/reticuleDefault");
            this.textureDictionary.Load("Numbers", "Assets/Textures/Numbers");

            //environment
            this.textureDictionary.Load("Assets/Textures/Props/Crates/crate1"); //demo use of the shorter form of Load() that generates key from asset name
            this.textureDictionary.Load("Assets/Textures/Props/Crates/crate2");
            this.textureDictionary.Load("Assets/Textures/Foliage/Ground/grass1");
            this.textureDictionary.Load("skybox_back", "Assets/Textures/Skybox/back");
            this.textureDictionary.Load("skybox_left", "Assets/Textures/Skybox/left");
            this.textureDictionary.Load("skybox_right", "Assets/Textures/Skybox/right");
            this.textureDictionary.Load("skybox_sky", "Assets/Textures/Skybox/sky");
            this.textureDictionary.Load("skybox_front", "Assets/Textures/Skybox/front");
            this.textureDictionary.Load("Assets/Textures/Foliage/Trees/tree2");
            this.textureDictionary.Load("desk1", "Assets/Textures/Props/Desk/desk1");
            this.textureDictionary.Load("desk2", "Assets/Textures/Props/Desk/desk2");
            this.textureDictionary.Load("cashbox", "Assets/Textures/Props/Desk/box");

            //characters
            this.textureDictionary.Load("character1", "Assets/Textures/Characters/Person1");
            this.textureDictionary.Load("character2", "Assets/Textures/Characters/Person2");
            this.textureDictionary.Load("character3", "Assets/Textures/Characters/Person3");
            this.textureDictionary.Load("character4", "Assets/Textures/Characters/Person4");
            this.textureDictionary.Load("character5", "Assets/Textures/Characters/Person5");
            this.textureDictionary.Load("character6", "Assets/Textures/Characters/Person6");
            this.textureDictionary.Load("character7", "Assets/Textures/Characters/Person7");
            this.textureDictionary.Load("character8", "Assets/Textures/Characters/Person8");
            this.textureDictionary.Load("character9", "Assets/Textures/Characters/Person9");
            this.textureDictionary.Load("character10", "Assets/Textures/Characters/Person10");

            //prop
            this.textureDictionary.Load("tele", "Assets/Textures/Props/Desk/Television");
            this.textureDictionary.Load("fiver", "Assets/Textures/Props/Munny/fiver");
            this.textureDictionary.Load("tenner", "Assets/Textures/Props/Munny/tenner");
            this.textureDictionary.Load("twenny", "Assets/Textures/Props/Munny/twenny");
            this.textureDictionary.Load("fiddy", "Assets/Textures/Props/Munny/fiddy");

            //dual texture demo
            //this.textureDictionary.Load("Assets/Textures/Foliage/Ground/grass_midlevel");
            //this.textureDictionary.Load("Assets/Textures/Foliage/Ground/grass_highlevel");

            //menu - buttons
            this.textureDictionary.Load("Assets/Textures/UI/Menu/Buttons/genericbtn");

            //menu - backgrounds
            this.textureDictionary.Load("Assets/Textures/UI/Menu/Backgrounds/mainmenu");
            this.textureDictionary.Load("Assets/Textures/UI/Menu/Backgrounds/audiomenu");
            this.textureDictionary.Load("Assets/Textures/UI/Menu/Backgrounds/controlsmenu");
            this.textureDictionary.Load("Assets/Textures/UI/Menu/Backgrounds/exitmenuwithtrans");

            //ui (or hud) elements
            this.textureDictionary.Load("Assets/Textures/UI/HUD/reticuleDefault");
            this.textureDictionary.Load("Assets/Textures/UI/HUD/progress_gradient");

            //architecture
            this.textureDictionary.Load("Assets/Textures/Architecture/Buildings/house-low-texture");
            this.textureDictionary.Load("Assets/Textures/Architecture/Walls/wall");

            //dual texture demo - see Main::InitializeCollidableGround()
            this.textureDictionary.Load("Assets/Debug/Textures/checkerboard_greywhite");

            //numbers
            this.textureDictionary.Load("Dot", "Assets/Textures/Numbers/Dot");
            this.textureDictionary.Load("Zero", "Assets/Textures/Numbers/Zero");
            this.textureDictionary.Load("One", "Assets/Textures/Numbers/One");
            this.textureDictionary.Load("Two", "Assets/Textures/Numbers/Two");
            this.textureDictionary.Load("Three", "Assets/Textures/Numbers/Three");
            this.textureDictionary.Load("Four", "Assets/Textures/Numbers/Four");
            this.textureDictionary.Load("Five", "Assets/Textures/Numbers/Five");
            this.textureDictionary.Load("Six", "Assets/Textures/Numbers/Six");
            this.textureDictionary.Load("Seven", "Assets/Textures/Numbers/Seven");
            this.textureDictionary.Load("Eight", "Assets/Textures/Numbers/Eight");
            this.textureDictionary.Load("Nine", "Assets /Textures/Numbers/Nine");

            //debug
            this.textureDictionary.Load("Assets/Debug/Textures/checkerboard");
            this.textureDictionary.Load("Assets/Debug/Textures/ml");
            this.textureDictionary.Load("Assets/Debug/Textures/checkerboard");
            
            //billboards
            this.textureDictionary.Load("Assets/Textures/Billboards/billboardtexture");
            this.textureDictionary.Load("Assets/Textures/Billboards/snow1");
            this.textureDictionary.Load("Assets/Textures/Billboards/chevron1");
            this.textureDictionary.Load("Assets/Textures/Billboards/chevron2");
            this.textureDictionary.Load("Assets/Textures/Billboards/alarm1");
            this.textureDictionary.Load("Assets/Textures/Billboards/alarm2");
            this.textureDictionary.Load("Assets/Textures/Props/tv");

            //Levels
            this.textureDictionary.Load("Assets/Textures/Level/level1");
        }
        #endregion
        private void LoadRails()
        {
            RailParameters railParameters = null;

            //create a simple rail that gains height as the target moves on +ve X-axis - try different rail vectors
            railParameters = new RailParameters("rail", new Vector3(-40, 10, 50), new Vector3(40, 10, 50));
            this.railDictionary.Add(railParameters.ID, railParameters);
        }

        private void LoadTracks()
        {
            Track3D track3D = null;

            //starts away from origin, moves forward and rises, then ends closer to origin and looking down from a height
            track3D = new Track3D(CurveLoopType.Oscillate);
            track3D.Add(new Vector3(0, 10, 200), -Vector3.UnitZ, Vector3.UnitY, 0);
            track3D.Add(new Vector3(0, 20, 150), -Vector3.UnitZ, Vector3.UnitY, 2);
            track3D.Add(new Vector3(0, 40, 100), -Vector3.UnitZ, Vector3.UnitY, 4);

            //set so that the camera looks down at the origin at the end of the curve
            Vector3 finalPosition = new Vector3(0, 80, 50);
            Vector3 finalLook = Vector3.Normalize(Vector3.Zero - finalPosition);

            track3D.Add(finalPosition, finalLook, Vector3.UnitY, 6);
            this.track3DDictionary.Add("push forward 1", track3D);

            //add more transform3D curves here...
        }

        #endregion

        #region Graphics & Effects
        private void InitializeEffects()
        {
            BasicEffect basicEffect = null;
            EffectParameters effectParameters = null;

            //used for UNLIT wireframe primitives
            basicEffect = new BasicEffect(graphics.GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            effectParameters = new EffectParameters(basicEffect);
            this.effectDictionary.Add(AppData.UnlitWireframeEffectID, effectParameters);

            //used for UNLIT textured solid primitives
            basicEffect = new BasicEffect(graphics.GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.TextureEnabled = true;
            effectParameters = new EffectParameters(basicEffect);
            this.effectDictionary.Add(AppData.UnlitTexturedEffectID, effectParameters);

            //used for LIT textured solid primitives
            basicEffect = new BasicEffect(graphics.GraphicsDevice);
            basicEffect.TextureEnabled = true;
            basicEffect.LightingEnabled = true;
            basicEffect.EnableDefaultLighting();
            basicEffect.PreferPerPixelLighting = true;
            effectParameters = new EffectParameters(basicEffect);
            this.effectDictionary.Add(AppData.LitTexturedEffectID, effectParameters);

            //used for UNLIT billboards i.e. cylindrical, spherical, normal, animated, scrolling
            Effect billboardEffect = Content.Load<Effect>("Assets/Effects/Billboard");
            effectParameters = new EffectParameters(billboardEffect);
            this.effectDictionary.Add(AppData.UnlitBillboardsEffectID, effectParameters);

        }
        private void InitializeGraphics()
        {
            this.graphics.PreferredBackBufferWidth = resolution.X;
            this.graphics.PreferredBackBufferHeight = resolution.Y;

            //solves the skybox border problem
            SamplerState samplerState = new SamplerState();
            samplerState.AddressU = TextureAddressMode.Clamp;
            samplerState.AddressV = TextureAddressMode.Clamp;
            this.graphics.GraphicsDevice.SamplerStates[0] = samplerState;

            //enable alpha transparency - see ColorParameters
            this.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            this.graphics.ApplyChanges();
        }
        #endregion

        #region Cameras
        private void InitializeCameras()
        {
            Viewport viewport = new Viewport(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            float aspectRatio = (float)this.resolution.X / this.resolution.Y;
            ProjectionParameters projectionParameters 
                = new ProjectionParameters(MathHelper.PiOver4, aspectRatio, 1, 4000);

            if (this.screenLayoutType == ScreenLayoutType.FirstPerson)
            {
                AddFirstPersonCamera(AppData.CameraIDFirstPerson, viewport, projectionParameters);
            }
            else if(this.screenLayoutType == ScreenLayoutType.MultiTrackFirstPerson)
            {
                //first we start with a track, track finishes
                //doesnt matter where the camera starts because we reset immediately inside the Transform3DCurveController
                Transform3D transform = Transform3D.Zero;

                Camera3D camera3D = new Camera3D(AppData.CameraIDTrack,
                    ActorType.Camera, transform,
                    projectionParameters, viewport,
                    0f, StatusType.Update);

                camera3D.AttachController(new Track3DController("tcc1", ControllerType.Track,
                    this.track3DDictionary["push forward 1"], PlayStatusType.Play));

                camera3D.AttachController(new CameraElapsedTimeController("cetc1",
                    ControllerType.CameraElapsedTime, PlayStatusType.Play, 5000, 
                    AppData.CameraIDFirstPerson));

                this.cameraManager.Add(camera3D);




                //set to a first person
                AddFirstPersonCamera(AppData.CameraIDFirstPerson, viewport, projectionParameters);

                this.cameraManager.SetActiveCamera(
                    camera => camera.ID.Equals(AppData.CameraIDTrack));
            }

            else if (this.screenLayoutType == ScreenLayoutType.MultiFullCycle)
            {
                AddFirstPersonCamera("fpc1", viewport, projectionParameters);

                //since we have lots of cameras, which one is shown first
                this.cameraManager.SetActiveCamera(camera => camera.ID.Equals(
                    "fpc1"));
            }

            else if (this.screenLayoutType == ScreenLayoutType.Flight)
            {
                AddFlightCamera(AppData.CameraIDFlight, viewport, projectionParameters);
            }
            else if (this.screenLayoutType == ScreenLayoutType.Rail)
            {
                AddRailCamera(AppData.CameraIDRail, viewport, projectionParameters);
            }
            else if (this.screenLayoutType == ScreenLayoutType.Track)
            {
                AddTrack3DCamera(AppData.CameraIDTrack, viewport, projectionParameters);
            }
        }

        private void AddMainAndPipCamera(Viewport viewport, ProjectionParameters projectionParameters)
        {
            Camera3D camera3D = null;
            Transform3D transform = null;


            //1st person
            transform = new Transform3D(
                 new Vector3(0, 10, 100), Vector3.Zero,
                 Vector3.One, -Vector3.UnitZ, Vector3.UnitY);
           
            camera3D = new Camera3D("fpc1", ActorType.Camera, transform,
                projectionParameters, viewport,
                1f, StatusType.Update);
           
            camera3D.AttachController(new FirstPersonCameraController(
              "fpcc1", ControllerType.FirstPerson,
              AppData.CameraMoveKeys, AppData.CameraMoveSpeed,
              AppData.CameraStrafeSpeed, AppData.CameraRotationSpeed, this.inputManagerParameters, this.screenCentre, viewport));
           
            //put controller later!
            this.cameraManager.Add(camera3D);



            //add second 1st person camera
            transform = new Transform3D(new Vector3(0, 40, 40), Vector3.One);

            Camera3D cameraCoins = new Camera3D(AppData.CameraIDRail,
                ActorType.Camera, transform,
                projectionParameters, viewport,
                0f, StatusType.Update);

            this.cameraManager.Add(cameraCoins);


        }

        #region camera types
        private void AddTrack3DCamera(string id, Viewport viewport, ProjectionParameters projectionParameters)
        {
            //doesnt matter where the camera starts because we reset immediately inside the Transform3DCurveController
            Transform3D transform = Transform3D.Zero; 

            Camera3D camera3D = new Camera3D(id, 
                ActorType.Camera, transform,
                projectionParameters, viewport,
                0f, StatusType.Update);

            camera3D.AttachController(new Track3DController("tcc1", ControllerType.Track,
                this.track3DDictionary["push forward 1"], PlayStatusType.Play));

            this.cameraManager.Add(camera3D);
        }  
        private void AddRailCamera(string id, Viewport viewport, ProjectionParameters projectionParameters)
        {
            //doesnt matter where the camera starts because we reset immediately inside the RailController
            Transform3D transform = Transform3D.Zero;

            Camera3D camera3D = new Camera3D(id,
                ActorType.Camera, transform,
                ProjectionParameters.StandardMediumFiveThree, viewport,
                0f, StatusType.Update);

            camera3D.AttachController(new RailController("rc1", ControllerType.Rail,
                this.drivableModelObject, this.railDictionary["battlefield 1"]));

            this.cameraManager.Add(camera3D);

        }
      
       
        private void AddFlightCamera(string id, Viewport viewport, ProjectionParameters projectionParameters)
        {
            Transform3D transform = new Transform3D(new Vector3(0, 10, 30), Vector3.Zero, Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);

            Camera3D camera3D = new Camera3D(id,
                ActorType.Camera, transform,
                projectionParameters, viewport,
                0f, StatusType.Update);

            camera3D.AttachController(new FlightCameraController("flight camera controller 1", 
                ControllerType.Flight, AppData.CameraMoveKeys_Alt1, AppData.CameraMoveSpeed,
                AppData.CameraStrafeSpeed, AppData.CameraRotationSpeed, this.inputManagerParameters, this.screenCentre));

            this.cameraManager.Add(camera3D);
        }
        private void AddFirstPersonCamera(string id, Viewport viewport, ProjectionParameters projectionParameters)
        {

            //desk cam
            Transform3D transform = new Transform3D(new Vector3(5, 12, 0), Vector3.Zero, Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);
        
            Camera3D camera3D = new Camera3D("fpc1",
                ActorType.Camera, transform,
                projectionParameters, viewport,
                0f, StatusType.Update);
        
            camera3D.AttachController(new FirstPersonCameraController(
                "fpcc1", ControllerType.FirstPerson,
                AppData.CameraMoveKeys, AppData.CameraMoveSpeed, 
                AppData.CameraStrafeSpeed, AppData.CameraRotationSpeed, this.inputManagerParameters, this.screenCentre, viewport));
        
            this.cameraManager.Add(camera3D);

            //cash money cam
            transform = new Transform3D(new Vector3(11, 10, 5), new Vector3(0, -75, 0), Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);

            Camera3D cameraCash = new Camera3D("fpc2",
                ActorType.Camera, transform,
                projectionParameters, viewport,
                0f, StatusType.Update);

            cameraCash.AttachController(new FirstPersonCameraController(
                "fpcc2", ControllerType.FirstPerson,
                AppData.CameraMoveKeys, AppData.CameraMoveSpeed,
                AppData.CameraStrafeSpeed, AppData.CameraRotationSpeed, this.inputManagerParameters, this.screenCentre, viewport));

            this.cameraManager.Add(cameraCash);


            //coins cam
            transform = new Transform3D(new Vector3(9, 7, -5), Vector3.Zero, Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);

            Camera3D cameraCoin = new Camera3D("fpc3",
                ActorType.Camera, transform,
                projectionParameters, viewport,
                0f, StatusType.Update);

            cameraCoin.AttachController(new FirstPersonCameraController(
                "fpcc3", ControllerType.FirstPerson,
                AppData.CameraMoveKeys, AppData.CameraMoveSpeed,
                AppData.CameraStrafeSpeed, AppData.CameraRotationSpeed, this.inputManagerParameters, this.screenCentre, viewport));

            this.cameraManager.Add(cameraCoin);

        }
        #endregion
        #endregion

        #region Load/Unload, Draw, Update
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            //// Create a new SpriteBatch, which can be used to draw textures.
            //spriteBatch = new SpriteBatch(GraphicsDevice);

            ////since debug needs sprite batch then call here
            //InitializeDebug(true);
        }
 
        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            this.modelDictionary.Dispose();
            this.fontDictionary.Dispose();
            this.textureDictionary.Dispose();

            //only C# dictionary so no Dispose() method to call
            this.railDictionary.Clear();
            this.track3DDictionary.Clear();
            this.objectArchetypeDictionary.Clear();
            this.vertexDictionary.Clear();

        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            ToggleMenu();
            InitializeNumberSystem();
            //InitializeNumberSystem();

#if DEBUG

            ToggleDebugInfo();
            DemoSetControllerPlayStatus();
            DemoSoundManager();
            DemoCycleCamera();
            DemoUIProgressUpdate();
            DemoUIAddRemoveObject();
#endif
            base.Update(gameTime);
        }

        private void DemoUIAddRemoveObject()
        {
            if(this.keyboardManager.IsFirstKeyPress(Keys.F5))
            {
                string strText = "You win!!!!";
                SpriteFont strFont = this.fontDictionary["menu"];
                Vector2 strDim = strFont.MeasureString(strText);
                strDim /= 2.0f;

                Transform2D transform
                    = new Transform2D(
                        (Vector2)this.screenCentre,
                        0, new Vector2(1, 1), 
                        strDim,     //Vector2.Zero,
                        new Integer2(1, 1));

                UITextObject newTextObject
                    = new UITextObject("win msg",
                    ActorType.UIText,
                    StatusType.Drawn | StatusType.Update,
                    transform,
                    Color.Red,
                    SpriteEffects.None,
                    0,
                    strText,
                    strFont);

                newTextObject.AttachController(new
                    UIRotationScaleExpireController("rslc1",
                    ControllerType.UIRotationLerp, 45, 0.5f, 5000, 1.01f));

                EventDispatcher.Publish(new EventData(
                    "",
                    newTextObject, //handle to "win!"
                    EventActionType.OnAddActor2D,
                    EventCategoryType.SystemAdd));
            }
            else if(this.keyboardManager.IsFirstKeyPress(Keys.F6))
            {
                EventDispatcher.Publish(new EventData(
                    "win msg",
                    null,
                    EventActionType.OnRemoveActor2D,
                    EventCategoryType.SystemRemove));
            }

        }

#if DEBUG
        private void DemoUIProgressUpdate()
        {
            //testing event generation for UIProgressController
            if (this.keyboardManager.IsFirstKeyPress(Keys.F9))
            {
                //increase the left progress controller by 2
                object[] additionalEventParams = {
                    AppData.PlayerOneProgressControllerID, -1 };
                EventDispatcher.Publish(new EventData(
                    EventActionType.OnHealthDelta, 
                    EventCategoryType.Player, additionalEventParams));
            }
            else if (this.keyboardManager.IsFirstKeyPress(Keys.F10))
            {
                //increase the left progress controller by 2
                object[] additionalEventParams = {
                    AppData.PlayerOneProgressControllerID, 1 };
                EventDispatcher.Publish(new EventData(EventActionType.OnHealthDelta, EventCategoryType.Player, additionalEventParams));
            }

            if (this.keyboardManager.IsFirstKeyPress(Keys.F11))
            {
                //increase the left progress controller by 2
                object[] additionalEventParams = { AppData.PlayerTwoProgressControllerID, -2 };
                EventDispatcher.Publish(new EventData(EventActionType.OnHealthDelta, EventCategoryType.Player, additionalEventParams));
            }
            else if (this.keyboardManager.IsFirstKeyPress(Keys.F12))
            {
                //increase the left progress controller by 2
                object[] additionalEventParams = { AppData.PlayerTwoProgressControllerID, 2 };
                EventDispatcher.Publish(new EventData(EventActionType.OnHealthDelta, EventCategoryType.Player, additionalEventParams));
            }
        }
        private void DemoCycleCamera()
        {
            if (this.keyboardManager.IsFirstKeyPress(AppData.CycleCameraKey))
            {
                EventDispatcher.Publish(new EventData(EventActionType.OnCameraCycle, EventCategoryType.Camera));
            }
        }
        private void ToggleDebugInfo()
        {
            if (this.keyboardManager.IsFirstKeyPress(AppData.DebugInfoShowHideKey))
            {
                EventDispatcher.Publish(new EventData(EventActionType.OnToggle, EventCategoryType.Debug));
            }
        }
        private void DemoSoundManager()
        {
            if (this.keyboardManager.IsFirstKeyPress(Keys.B))
            {
                //add event to play mouse click
                object[] additionalParameters = { "boing" };
                EventDispatcher.Publish(new EventData(EventActionType.OnPlay, EventCategoryType.Sound2D, additionalParameters));
            }
        }
        private void DemoSetControllerPlayStatus()
        {
            Actor3D torusActor = this.object3DManager.Find(actor => actor.ID.Equals("torus 1"));
            if (torusActor != null && this.keyboardManager.IsFirstKeyPress(Keys.O))
            {
                torusActor.SetControllerPlayStatus(PlayStatusType.Pause, controller => controller.GetControllerType() == ControllerType.Rotation);
            }
            else if (torusActor != null && this.keyboardManager.IsFirstKeyPress(Keys.P))
            {
                torusActor.SetControllerPlayStatus(PlayStatusType.Play, controller => controller.GetControllerType() == ControllerType.Rotation);
            }
        }
#endif

        private void ToggleMenu()
        {
            if (this.keyboardManager.IsFirstKeyPress(AppData.MenuShowHideKey))
            {
                if (this.menuManager.IsVisible)
                    EventDispatcher.Publish(new EventData(EventActionType.OnStart, EventCategoryType.Menu));
                else
                    EventDispatcher.Publish(new EventData(EventActionType.OnPause, EventCategoryType.Menu));
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            base.Draw(gameTime);
        }
        #endregion
    }
}

