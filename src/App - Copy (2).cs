using StereoKit;
using StereoKitApp.Examples;
using StereoKitTest;
using System.Collections.Generic;
//using StereoKitApp.Examples;
using System.Numerics;

namespace StereoKitApp
{
    public class App
    {
        public SKSettings Settings => new SKSettings
        {
            appName = "StereoKit Template",
            assetsFolder = "Assets",
            displayPreference = DisplayMode.MixedReality
        };

        //public static PassthroughFBExt passthrough;
        bool showHandMenus = true;

        //Pose cubePose = new Pose(0, 0, -0.5f, Quat.Identity);
        //Model cube;
        Matrix4x4 floorTransform = Matrix.TS(new Vector3(0, -1.5f, 0), new Vector3(30, 0.1f, 30));
        Material floorMaterial;
        Model kanbanBoard;

        // Maybe still too far away?
        Pose kanbanBoardPose = new Pose(0f, 0f, -3f, Quat.LookDir(0, 0, 0));

        DemoHands demoHands;
        DemoNodes demoNodes;
        DemoControllers demoControllers;
        DemoUI demoUI;
        DemoWelcome demoWelcome;
        DemoTextInput demoTextInput;
        DemoText demoText;
        DemoSky demoSky;
        //DemoFBPassthrough demoFBPassthrough;
        DemoLines demoLines;
        DemoLineRender demoLineRender;
        DemoMaterial demoMaterial;
        DemoGeo demoGeo;
        DemoTextures demoTextures;
        DemoMath demoMath;
        DemoPhysics demoPhysics;
        DemoManyObjects demoManyObjects;

        static List<Model> models = new List<Model>();
        static List<Pose> poses = new List<Pose>();

        Pose windowPose = new Pose(0.5f, 0, -0.5f, Quat.LookDir(-1, 0, 1));
        Pose windowPose2 = new Pose(-0.5f, 0, -0.5f, Quat.LookDir(-1, 0, 1));

        public void Init()
        {
            // Create assets used by the app
            //cube = Model.FromMesh(
            //    Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
            //    Default.MaterialUI);

            kanbanBoard = Model.FromFile("kanban-board.glb");

            floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;

            //demoHands = new DemoHands();
            //demoHands.Initialize();

            //demoNodes = new DemoNodes();
            //demoNodes.Initialize();

            //demoControllers = new DemoControllers();
            //demoControllers.Initialize();

            //demoUI = new DemoUI();
            //demoUI.Initialize();

            //demoWelcome = new DemoWelcome();
            //demoWelcome.Initialize();

            demoTextInput = new DemoTextInput();
            demoTextInput.Initialize();

            //demoText = new DemoText();
            //demoText.Initialize();

            //demoSky = new DemoSky();
            //demoSky.Initialize();

            //demoFBPassthrough = new DemoFBPassthrough();
            //demoFBPassthrough.Initialize();

            //demoLines = new DemoLines();
            //demoLines.Initialize();

            //demoLineRender = new DemoLineRender();
            //demoLineRender.Initialize();

            //demoMaterial = new DemoMaterial();
            //demoMaterial.Initialize();

            //demoGeo = new DemoGeo();
            //demoGeo.Initialize();

            //demoTextures = new DemoTextures();
            //demoTextures.Initialize();

            //demoMath = new DemoMath();
            //demoMath.Initialize();

            //demoPhysics = new DemoPhysics();
            //demoPhysics.Initialize();

            //demoManyObjects= new DemoManyObjects(); 
            //demoManyObjects.Initialize();

            Model cube = Model.FromMesh(
                    Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
                    Default.MaterialUI);

            poses.Add(new Pose(0, 0, -0.5f, Quat.Identity));
            models.Add(cube);

        }

        public void Step()
        {

            if (SK.System.displayType == Display.Opaque)
                Default.MeshCube.Draw(floorMaterial, floorTransform);

            //UI.Handle("Cube", ref cubePose, cube.Bounds);
            //cube.Draw(cubePose.ToMatrix());

            UI.HandleBegin("KanbanBoard", ref kanbanBoardPose, kanbanBoard.Bounds);
            kanbanBoard.Draw(kanbanBoardPose.ToMatrix());
            UI.HandleEnd();


            UI.Toggle("Menu", ref showHandMenus);

            if (showHandMenus)
            {
                DrawHandMenu(Handed.Right);
                DrawHandMenu(Handed.Left);
            }

            var model = models[0];
            Pose curr = poses[0];
            UI.Handle("Cube", ref curr, model.Bounds);

            for (int x = 0; x < models.Count; x++)
            {
                model = models[x];
                Pose pose = poses[x];
                model.Draw(pose.ToMatrix());
            }

            poses.RemoveAt(poses.Count - 1);
            poses.Insert(0, curr);


            UI.WindowBegin("Here is window 2", ref windowPose2, new Vec2(0.2f, 0.2f), UIWin.Body);
            UI.Text("Here is some different text different text different text");
            if (UI.Button("Edit"))
            {

            }
            UI.WindowEnd();



            //demoHands.Update();
            //demoNodes.Update();
            //demoControllers.Update();
            //demoUI.Update();
            //demoWelcome.Update();
            //demoTextInput.Update();
            //demoText.Update();
            //demoSky.Update();
            //demoFBPassthrough.Update();
            //demoLines.Update();
            //demoLineRender.Update();
            //demoMaterial.Update();
            //demoGeo.Update();
            //demoTextures.Update();
            //demoMath.Update();
            //demoPhysics.Update();
            //demoManyObjects.Update();
        }

        static bool HandFacingHead(Handed handed)
        {
            Hand hand = Input.Hand(handed);
            if (!hand.IsTracked)
                return false;

            Vec3 palmDirection = (hand.palm.Forward).Normalized;
            Vec3 directionToHead = (Input.Head.position - hand.palm.position).Normalized;

            return Vec3.Dot(palmDirection, directionToHead) > 0.5f;
        }


        public static void DrawHandMenu(Handed handed)
        {
            if (!HandFacingHead(handed))
                return;

            // Decide the size and offset of the menu
            Vec2 size = new Vec2(4, 16);
            float offset = handed == Handed.Left ? -2 - size.x : 2 + size.x;

            // Position the menu relative to the side of the hand
            Hand hand = Input.Hand(handed);
            Vec3 at = hand[FingerId.Little, JointId.KnuckleMajor].position;
            Vec3 down = hand[FingerId.Little, JointId.Root].position;
            Vec3 across = hand[FingerId.Index, JointId.KnuckleMajor].position;

            Pose menuPose = new Pose(
                at,
                Quat.LookAt(at, across, at - down) * Quat.FromAngles(0, handed == Handed.Left ? 90 : -90, 0));
            menuPose.position += menuPose.Right * offset * U.cm;
            menuPose.position += menuPose.Up * (size.y / 2) * U.cm;

            // And make a menu!
            UI.WindowBegin("HandMenu", ref menuPose, size * U.cm, UIWin.Empty);
            if (UI.Button("New Cube"))
            {                
                Model cube = Model.FromMesh(
                    Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
                    Default.MaterialUI);

                poses.Add(new Pose(0, 0, -0.5f, Quat.Identity));

                models.Add(cube);
            }
           
            if (UI.Button("Exit"))
            {
                SK.Quit();
            }

            UI.WindowEnd();
        }

    }
}