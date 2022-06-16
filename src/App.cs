using StereoKit;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace StereoKitApp
{
    public class App
    {
        public SKSettings Settings => new SKSettings
        {
            appName = "VR Boards",
            assetsFolder = "Assets",
            displayPreference = DisplayMode.MixedReality
        };

        bool showHandMenus = true;
        static bool isEditMode = false;
        static TextStyle headingStyle;
        static TextStyle bodyStyle;

        // Floor
        Matrix4x4 floorTransform = Matrix.TS(new Vector3(0, -1.5f, 0), new Vector3(30, 0.1f, 30));
        Material floorMaterial;

        //Matrix4x4 logoTransform = Matrix.TS(new Vector3(0, 2f, -1f), new Vector3(1f, 0.5f, 1));

        Matrix logoTransform = Matrix.TRS(new Vector3(0, 1.1f, -1f), Quat.LookDir(0, 0, 1), new Vector3(2f, 0.5f, 1f));
        Material logoMaterial;

        // Board positions        
        Pose swotBoardPose = new Pose(-2f, 0f, 0f, Quat.LookDir(1f, 0, 1f));
        Pose kanbanBoardPose = new Pose(0f, 0f, -1f, Quat.LookDir(0, 0, 1f));
        Pose businessModelCanvasBoardPose = new Pose(2.75f, 0f, 0f, Quat.LookDir(-1f, 0, 1f));

        // todo: refactor
        static List<Pose> poses = new List<Pose>();
        static List<string> colours = new List<string>();
        //static List<string> titles = new List<string>();
        static string[] titles = new string[1000];
        static string[] descriptions = new string[1000];
        //static List<string> descriptions = new List<string>();

        Model kanbanBoard;
        Model swotBoard;
        Model businessModelCanvasBoard;

        Model greenModel;
        Model blueModel;
        Model redModel;
        Model yellowModel;

        Material greenMaterial;
        Material blueMaterial;
        Material redMaterial;
        Material yellowMaterial;

        public void Init()
        {
            // Create assets used by the app           
            floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;

            // Create logo
            logoMaterial = Material.Default.Copy();
            logoMaterial[MatParamName.DiffuseTex] = Tex.FromFile("BoardsVR.png");
            logoMaterial.Transparency = Transparency.Blend;
            //logoMaterial[MatParamName.ColorTint] = Color.HSV(0.6f, 0.7f, 1f);

            // Init boards
            kanbanBoard = Model.FromFile("kanban-board.glb");
            swotBoard = Model.FromFile("SWOT.glb");
            businessModelCanvasBoard = Model.FromFile("business-model-canvas.glb");

            //Note: Can only retrieve font if run on Windows emulator, not headset?
            headingStyle = Text.MakeStyle(
                //Font.FromFile("C:/Windows/Fonts/segoeprb.ttf") ?? 
                Default.Font,
                1 * U.cm,
                Color.Black);

            bodyStyle = Text.MakeStyle(
                //Font.FromFile("C:/Windows/Fonts/segoepr.ttf") ?? 
                Default.Font,
                0.75f * U.cm,
                Color.Black);

            // Init materials
            greenMaterial = Material.Default.Copy();
            blueMaterial = Material.Default.Copy();
            redMaterial = Material.Default.Copy();
            yellowMaterial = Material.Default.Copy();

            greenMaterial[MatParamName.ColorTint] = Color.HSV(0.3f, 0.4f, 1.0f);
            blueMaterial[MatParamName.ColorTint] = Color.HSV(0.5f, 0.5f, 1f);
            redMaterial[MatParamName.ColorTint] = Color.HSV(0f, 0.5f, 1f);
            yellowMaterial[MatParamName.ColorTint] = Color.HSV(0.14f, 0.28f, 1f);

            // Init models
            greenModel = Model.FromMesh(
                    Mesh.GenerateRoundedCube(new Vec3(0.1f, 0.1f, 0.01f), 0.001f),
                    greenMaterial);

            blueModel = Model.FromMesh(
                   Mesh.GenerateRoundedCube(new Vec3(0.1f, 0.1f, 0.01f), 0.001f),
                   blueMaterial);

            redModel = Model.FromMesh(
                   Mesh.GenerateRoundedCube(new Vec3(0.1f, 0.1f, 0.01f), 0.001f),
                   redMaterial);

            yellowModel = Model.FromMesh(
                   Mesh.GenerateRoundedCube(new Vec3(0.1f, 0.1f, 0.01f), 0.001f),
                   yellowMaterial);

            // Add the first card
            poses.Add(new Pose(0, 0, -0.5f, Quat.LookDir(0, 0, 1)));
            colours.Add("Green");
            titles[0] = "Example Heading";
            descriptions[0] = "Here is a much longer body, perhaps with multiple lines worth of text. To be honest it could get quite long for a card";
        }

        public void Step()
        {
            if (SK.System.displayType == Display.Opaque)
                Default.MeshCube.Draw(floorMaterial, floorTransform);

            //UI.HandleBegin("KanbanBoard", ref kanbanBoardPose, kanbanBoard.Bounds);
            kanbanBoard.Draw(kanbanBoardPose.ToMatrix(0.5f));
            swotBoard.Draw(swotBoardPose.ToMatrix(0.5f));
            businessModelCanvasBoard.Draw(businessModelCanvasBoardPose.ToMatrix(0.5f));
            //UI.HandleEnd();

            // Draw logo
            Default.MeshQuad.Draw(logoMaterial, logoTransform);

            if (showHandMenus)
            {
                //DrawHandMenu(Handed.Right);
                DrawHandMenu(Handed.Left);
            }

            for (int x = 0; x < poses.Count; x++)
            {
                Pose pose = poses[x];

                UI.Handle($"Cube{x}", ref pose, greenModel.Bounds);
                {
                    if (isEditMode)
                    {
                        DrawHeadingAndBodyEntry(pose, x, ref titles[x], ref descriptions[x]);
                    }
                    else
                    {
                        DrawHeadingAndBody(pose, x, ref titles[x], ref descriptions[x]);
                    }

                    if (colours[x] == "Red")
                    {
                        redModel.Draw(pose.ToMatrix());
                    }
                    else if (colours[x] == "Yellow")
                    {
                        yellowModel.Draw(pose.ToMatrix());
                    }
                    else if (colours[x] == "Green")
                    {
                        greenModel.Draw(pose.ToMatrix());
                    }
                    else if (colours[x] == "Blue")
                    {
                        blueModel.Draw(pose.ToMatrix());
                    }
                }

                poses[x] = pose;
            }
        }

        private static void DrawHeadingAndBodyEntry(Pose pose, int number, ref string title, ref string description)
        {
            // https://stereokit.net/Pages/Reference/UI/Input.html            

            var layoutStart = V.XYZ(0.025f, 0.05f, -0.01f);

            UI.PushSurface(pose, layoutStart, default);
            {
                Vec2 inputHeadingSize = V.XY(8 * U.cm, 0);
                Vec2 inputBodySize = V.XY(8 * U.cm, 5 * U.cm);

                UI.Input($"HeadingInput{number}", ref title, inputHeadingSize, TextContext.Text);
                UI.Input($"BodyInput{number}", ref description, inputBodySize, TextContext.Text);

                if (UI.Button("Delete"))
                {
                    Remove(number);
                }
            }
            UI.PopSurface();
        }

        private static void Remove(int id)
        {
            var titleList = titles.ToList();
            titleList.RemoveAt(id);
            titles = titleList.ToArray();

            var descriptionList = descriptions.ToList();
            descriptionList.RemoveAt(id);
            descriptions = descriptionList.ToArray();

            poses.RemoveAt(id);
            colours.RemoveAt(id);
        }

        private static void DrawHeadingAndBody(Pose pose, int number, ref string title, ref string description)
        {
            Vec2 size = new Vec2(0.095f, 0.095f);

            UI.PushSurface(pose, default);
            {
                Text.Add(title,

                    Matrix.TR(new Vec3(0, -0.0f, -0.008f), Quat.LookDir(0, 0, -1)), // Quat.LookDir(Vec3.Forward)
                    size,
                    TextFit.Squeeze,
                    headingStyle,
                    TextAlign.Center | TextAlign.XCenter,
                    TextAlign.XCenter);

                Text.Add(description,
                    Matrix.TR(new Vec3(0, -0.015f, -0.008f), Quat.LookDir(0, 0, -1)),
                    size,
                    TextFit.Wrap,
                    bodyStyle,
                    TextAlign.Center | TextAlign.XCenter,
                    TextAlign.XCenter);
            }
            UI.PopSurface();
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
            if (UI.Button("Green"))
            {
                // menuPose.position

                poses.Add(new Pose(menuPose.position, Quat.LookDir(0, 0, 1)));
                colours.Add("Green");
                //titles.Add("");
                //descriptions.Add("");
            }

            if (UI.Button("Yellow"))
            {
                poses.Add(new Pose(menuPose.position, Quat.LookDir(0, 0, 1)));
                colours.Add("Yellow");
                //titles.Add("");
                //descriptions.Add("");
            }

            if (UI.Button("Red"))
            {
                poses.Add(new Pose(menuPose.position, Quat.LookDir(0, 0, 1)));
                colours.Add("Red");
                //titles.Add("");
                //descriptions.Add("");
            }

            if (UI.Button("Blue"))
            {
                poses.Add(new Pose(menuPose.position, Quat.LookDir(0, 0, 1)));
                colours.Add("Blue");
                //titles.Add("");
                //descriptions.Add("");
            }

            if (UI.Button("Toggle Edit Mode"))
            {
                isEditMode = !isEditMode;
            }

            if (UI.Button("Exit"))
            {
                SK.Quit();
            }

            UI.WindowEnd();
        }
    }

    class Card
    {
        public Card(string title, string description, string colour)
        {
            Title = title;
            Description = description;
            Colour = colour;
        }

        public string Title { get; }
        public string Description { get; }
        public string Colour { get; }

        public static List<Card> GetCardsExampleCards()
        {
            return new List<Card>
            {
                new Card("title", "description", "Green"),
                new Card("title", "description", "Yellow"),
                new Card("title", "description", "Blue"),
                new Card("title", "description", "Red"),
            };
        }
    }
}