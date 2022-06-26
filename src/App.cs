using StereoKit;
using StereoKit.Framework;
using System;
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
        static bool isEditMode = true;
        static TextStyle headingStyle;
        static TextStyle bodyStyle;
        static Tex cubemap = null;
        static Pose windowPose = new Pose(0, 0.1f, -0.3f, Quat.LookDir(-Vec3.Forward));

        // Floor
        Matrix4x4 floorTransform = Matrix.TS(new Vector3(0, -1.5f, 0), new Vector3(30, 0.1f, 30));
        Material floorMaterial;

        Matrix logoTransform = Matrix.TRS(new Vector3(0, 1.1f, -1f), Quat.LookDir(0, 0, 1), new Vector3(2f, 0.5f, 1f));
        Material logoMaterial;

        // Board positions        
        Pose swotBoardPose = new Pose(-2f, 0f, 0f, Quat.LookDir(1f, 0, 1f));
        Pose kanbanBoardPose = new Pose(0f, 0f, -1f, Quat.LookDir(0, 0, 1f));
        Pose businessModelCanvasBoardPose = new Pose(2.75f, 0f, 0f, Quat.LookDir(-1f, 0, 1f));

        // todo: refactor
        static List<Pose> poses = new List<Pose>();
        static List<CardColor> colours = new List<CardColor>();
        static string[] titles = new string[1000];
        static string[] descriptions = new string[1000];

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

        HandMenuRadial handMenu;

        static bool showEditWindow = false;
        static int cardEditNumber = 0;

        public void Init()
        {
            // Skymap
            //LoadSkyImage("belfast_farmhouse_4k.hdr");

            // Create assets used by the app           
            floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;

            // Create logo
            logoMaterial = Material.Default.Copy();
            logoMaterial[MatParamName.DiffuseTex] = Tex.FromFile("BoardsVR.png");
            logoMaterial.Transparency = Transparency.Blend;

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
            //poses.Add(new Pose(0, 0, -0.5f, Quat.LookDir(0, 0, 1)));
            //colours.Add("Green");
            //titles[0] = "Example Heading";
            //descriptions[0] = "Here is a much longer body, perhaps with multiple lines worth of text. To be honest it could get quite long for a card";

            // Add initial cards
            var exampleCards = Card.GetCardsExampleCards();//.Take(1).ToList();
            for (int x = 0; x < exampleCards.Count; x++)
            {
                poses.Add(new Pose(0, 0, -0.5f, Quat.LookDir(0, 0, 1)));
                colours.Add(exampleCards[x].Colour);
                titles[x] = exampleCards[x].Title;
                descriptions[x] = exampleCards[x].Description;
            }

            // Radial Menu
            /*
            handMenu = SK.AddStepper(new HandMenuRadial(
                new HandRadialLayer("Root",
                    new HandMenuItem("File", null, null, "File"),
                    new HandMenuItem("Edit", null, null, "Edit"),
                    new HandMenuItem("About", null, () => Log.Info(SK.VersionName)),
                    new HandMenuItem("Cancel", null, null)),
                new HandRadialLayer("File",
                    new HandMenuItem("New", null, () => Log.Info("New")),
                    new HandMenuItem("Open", null, () => Log.Info("Open")),
                    new HandMenuItem("Close", null, () => Log.Info("Close")),
                    new HandMenuItem("Back", null, null, HandMenuAction.Back)),
                new HandRadialLayer("Edit",
                    new HandMenuItem("Copy", null, () => Log.Info("Copy")),
                    new HandMenuItem("Paste", null, () => Log.Info("Paste")),
                    new HandMenuItem("Back", null, null, HandMenuAction.Back)))
                );*/

            Action<string> changeScene = new Action<string>(LoadSkyImage);

            handMenu = SK.AddStepper(new HandMenuRadial(
                new HandRadialLayer("Root",
                    new HandMenuItem("Scene", null, null, "Scene")),
                //new HandMenuItem("Edit", null, null, "Edit"),
                //new HandMenuItem("About", null, () => Log.Info(SK.VersionName)),
                //new HandMenuItem("Cancel", null, null)),
                new HandRadialLayer("Scene",
                    new HandMenuItem("Normal", null, () => changeScene(null)),
                    new HandMenuItem("Scene 1", null, () => changeScene("belfast_farmhouse_4k.hdr")),
                    //new HandMenuItem("Scene 2", null, () => Log.Info("Close")),
                    new HandMenuItem("Back", null, null, HandMenuAction.Back)))
                );
        }

        public void Step()
        {
            // Floor grid
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

            // Test Window
            //UI.WindowBegin($"SomeWindow", ref windowPose, new Vec2(20, 0) * U.cm);
            //// Label
            //UI.Label("Test Label");
            //// Text
            //UI.WindowEnd();

            // For every card
            for (int x = 0; x < poses.Count; x++)
            {
                Pose pose = poses[x];

                UI.Handle($"Cube{x}", ref pose, greenModel.Bounds);
                {
                    DrawHeadingAndBody(pose, x, ref titles[x], ref descriptions[x]);

                    if (colours[x] == CardColor.Red)
                    {
                        redModel.Draw(pose.ToMatrix());
                    }
                    else if (colours[x] == CardColor.Yellow)
                    {
                        yellowModel.Draw(pose.ToMatrix());
                    }
                    else if (colours[x] == CardColor.Green)
                    {
                        greenModel.Draw(pose.ToMatrix());
                    }
                    else if (colours[x] == CardColor.Blue)
                    {
                        blueModel.Draw(pose.ToMatrix());
                    }
                }

                poses[x] = pose;
            }

            // Show edit window?
            if (showEditWindow)
            {
                // Retreive card details..

                UI.WindowBegin($"Edit Card #{cardEditNumber}", ref windowPose, new Vec2(20, 0) * U.cm);

                UI.Label($"Title");
                UI.Input("Title", ref titles[cardEditNumber]);

                UI.Label($"Description");
                UI.Input("Description", ref descriptions[cardEditNumber]);

                if (UI.Button("Close"))
                {
                    showEditWindow = false;
                }

                UI.WindowEnd();
            }
        }

        //private static void Remove(int id)
        //{
        //    var titleList = titles.ToList();
        //    titleList.RemoveAt(id);
        //    titles = titleList.ToArray();

        //    var descriptionList = descriptions.ToList();
        //    descriptionList.RemoveAt(id);
        //    descriptions = descriptionList.ToArray();

        //    poses.RemoveAt(id);
        //    colours.RemoveAt(id);
        //}

        private static void DrawHeadingAndBody(Pose pose, int number, ref string title, ref string description)
        {
            // Gives a bit of padding around the edges
            Vec2 size = new Vec2(0.090f, 0.090f);

            var headingPose = new Vec3(0, -0.0f, -0.008f);
            var headingTextFit = TextFit.Squeeze;
            var headingAlign = TextAlign.XCenter;
            var descriptionPose = new Vec3(0, -0.015f, -0.008f);

            // If no description, move heading to center
            if (string.IsNullOrWhiteSpace(description))
            {
                headingTextFit = TextFit.Wrap;
                headingAlign = TextAlign.Center;
            }

            UI.PushSurface(pose, default);
            {

                Text.Add(title,
                    Matrix.TR(headingPose, Quat.LookDir(0, 0, -1)),
                    size,
                    headingTextFit,
                    headingStyle,
                    TextAlign.Center | TextAlign.XCenter,
                    headingAlign);

                if (!string.IsNullOrWhiteSpace(description))
                {
                    Text.Add(description,
                        Matrix.TR(descriptionPose, Quat.LookDir(0, 0, -1)),
                        size,
                        TextFit.Wrap,
                        bodyStyle,
                        TextAlign.Center | TextAlign.XCenter,
                        TextAlign.XCenter);
                }

                if (isEditMode)
                {
                    //Vec2 windowSize = new Vec2(10f, 5f);

                    // Only show if in edit mode?
                    // Hierarchy puts in correct position but click is off center?
                    //Hierarchy.Push(Matrix.T(new Vec3(0.02f, -0.06f, 0)));

                    // Add edit button below card?
                    if (UI.ButtonAt($"Edit #{number}", new Vec3(0.03f, -0.06f, 0), new Vec2(0.06f, 0.03f)))
                    {
                        showEditWindow = true;
                        cardEditNumber = number;
                    }

                    //Hierarchy.Pop();
                }

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

            // Green
            UI.PushTint(Color.Hex(0x00FF00FF));

            if (UI.Button("Green"))
            {
                poses.Add(new Pose(menuPose.position, Quat.LookDir(0, 0, 1)));
                colours.Add(CardColor.Green);
            }

            UI.PopTint();

            // Yellow
            UI.PushTint(Color.Hex(0xFFFF00FF));

            if (UI.Button("Yellow"))
            {
                poses.Add(new Pose(menuPose.position, Quat.LookDir(0, 0, 1)));
                colours.Add(CardColor.Yellow);
            }

            UI.PopTint();

            // Red
            UI.PushTint(Color.Hex(0xFF0000FF));

            if (UI.Button("Red"))
            {
                poses.Add(new Pose(menuPose.position, Quat.LookDir(0, 0, 1)));
                colours.Add(CardColor.Red);
            }

            UI.PopTint();

            // Blue
            UI.PushTint(Color.Hex(0x0000FFFF));


            if (UI.Button("Blue"))
            {
                poses.Add(new Pose(menuPose.position, Quat.LookDir(0, 0, 1)));
                colours.Add(CardColor.Blue);
            }

            UI.PopTint();

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

        void LoadSkyImage(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                Renderer.SkyTex = null;
                cubemap = null;
                return;
            }

            cubemap = Tex.FromCubemapEquirectangular(file);

            Renderer.SkyTex = cubemap;
        }
    }

    public enum CardColor
    {
        Blue,
        Green,
        Red,
        Yellow
    }

    class Card
    {
        public Card(string title, string description, CardColor colour)
        {
            Title = title;
            Description = description;
            Colour = colour;
        }

        public string Title { get; }
        public string Description { get; }

        public CardColor Colour { get; set; }

        public static List<Card> GetCardsExampleCards()
        {
            // These are all added at the same place and have to be manually moved into position currently
            return new List<Card>
            {
                // SWOT of StereKit

                // Strengths
                new Card("Code first", "", CardColor.Green),
                new Card("Small code footprint", "", CardColor.Green),
                new Card("Constantly improving", "", CardColor.Green),
                new Card("Open source", "", CardColor.Green),
                
                // Weaknesses
                new Card("Not yet popular", "", CardColor.Blue),
                new Card("Community samples limited", "", CardColor.Blue),

                // Opportunities
                new Card("C# code on Quests!", "", CardColor.Yellow),
                new Card("Great to share!", "", CardColor.Yellow),

                // Threats
                new Card("Microsoft XR politics", "", CardColor.Red),
                new Card("Other XR platforms", "", CardColor.Red),



                // Kanban board of items

                // Backlog
                new Card("Multiple Backgroundss","", CardColor.Green),
                new Card("Multi Coloured Cards","", CardColor.Green),
                new Card("Edit Mode","", CardColor.Green),
                //new Card("","", CardColor.Green),

                // In Progress


                // Done

                /*
                */
            };
        }
    }
}