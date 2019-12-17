using GDApp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDLibrary
{
    //Draws the bounding volume for your primitive objects
    public class PrimitiveDebugDrawer : PausableDrawableGameComponent
    {
        private CameraManager cameraManager;
        private ObjectManager objectManager;
        private BasicEffect wireframeEffect;
        private bool bPauseDraw = true; //start with menu so dont show debug
        private bool bShowCDCRSurfaces;
        private bool bShowZones;

        //temp vars
        private IVertexData vertexData = null;
        private SphereCollisionPrimitive coll;
        private Matrix world;


        public PrimitiveDebugDrawer(Game game, bool bShowCDCRSurfaces, bool bShowZones,
            CameraManager cameraManager, ObjectManager objectManager,
            EventDispatcher eventDispatcher, StatusType statusType)
            : base(game, eventDispatcher, statusType)
        {
            this.cameraManager = cameraManager;
            this.objectManager = objectManager;
            this.bShowCDCRSurfaces = bShowCDCRSurfaces;
            this.bShowZones = bShowZones;

            //used to draw bounding volumes
            this.wireframeEffect = new BasicEffect(Game.GraphicsDevice);
            this.wireframeEffect.VertexColorEnabled = true;
        }


        protected override void ApplyDraw(GameTime gameTime)
        {
            //set so we dont see the bounding volume through the object is encloses - disable to see result
            Game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            foreach (IActor actor in this.objectManager.OpaqueDrawList)
            {
                DrawSurfaceOrZonePrimitive(gameTime, actor);
            }
            foreach (IActor actor in this.objectManager.TransparentDrawList)
            {
                DrawSurfaceOrZonePrimitive(gameTime, actor);
            }
        }

        private void DrawSurfaceOrZonePrimitive(GameTime gameTime, IActor actor)
        {
            if (actor is CollidablePrimitiveObject && bShowCDCRSurfaces)
                DrawBoundingPrimitive(gameTime, (actor as CollidablePrimitiveObject).CollisionPrimitive, Color.White); //collidable object volumes are White
            else if (actor is SimpleZoneObject && bShowZones)
                DrawBoundingPrimitive(gameTime, (actor as SimpleZoneObject).CollisionPrimitive, Color.Red);        //collidable zone volumes are red
        }

        private void DrawBoundingPrimitive(GameTime gameTime, ICollisionPrimitive collisionPrimitive, Color color)
        {
            if (collisionPrimitive is SphereCollisionPrimitive)
            {
                int primitiveCount = 0;
                PrimitiveType primitiveType;
                vertexData = new VertexData<VertexPositionColor>(
                                VertexFactory.GetSphereVertices(1, 10, out primitiveType, out primitiveCount),
                                primitiveType, primitiveCount);

                coll = collisionPrimitive as SphereCollisionPrimitive;
                world = Matrix.Identity * Matrix.CreateScale(coll.BoundingSphere.Radius) * Matrix.CreateTranslation(coll.BoundingSphere.Center);
                this.wireframeEffect.World = world;
                this.wireframeEffect.View = this.cameraManager.ActiveCamera.View;
                this.wireframeEffect.Projection = this.cameraManager.ActiveCamera.ProjectionParameters.Projection;
                this.wireframeEffect.DiffuseColor = Color.White.ToVector3();
                this.wireframeEffect.CurrentTechnique.Passes[0].Apply();
                vertexData.Draw(gameTime, this.wireframeEffect);
            }
            else 
            {
                BoxCollisionPrimitive coll = collisionPrimitive as BoxCollisionPrimitive;
                BoundingBoxBuffers buffers = BoundingBoxDrawer.CreateBoundingBoxBuffers(coll.BoundingBox, this.GraphicsDevice);
                BoundingBoxDrawer.DrawBoundingBox(buffers, this.wireframeEffect, this.GraphicsDevice, this.cameraManager.ActiveCamera);
            }
        }
    }
}
