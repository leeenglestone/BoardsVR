using StereoKit;
using StereoKit.Framework;
using System;
using System.Collections.Generic;
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
        static Tex cubemap = null;
        static Pose windowPose = new Pose(0, 0.1f, -0.3f, Quat.LookDir(-Vec3.Forward));
        static Tex defaultSkyTex;

        // Floor
        Matrix4x4 floorTransform = Matrix.TS(new Vector3(0, -1.5f, 0), new Vector3(30, 0.1f, 30));
        Material floorMaterial;

        Matrix logoTransform = Matrix.TRS(new Vector3(0, 1.1f, -1f), Quat.LookDir(0, 0, 1), new Vector3(2f, 0.5f, 1f));
        Material logoMaterial;

        // Board positions
        Pose swotBoardPose = new Pose(-2f, 0f, 0f, Quat.LookDir(1f, 0, 1f));
        Pose kanbanBoardPose = new Pose(0f, 0f, -1f, Quat.LookDir(0, 0, 1f));
        Pose businessModelCanvasBoardPose = new Pose(2.75f, 0f, 0f, Quat.LookDir(-1f, 0, 1f));

        // todo: refactor and remove 1000 card limit
        // todo: add persistance
        static List<Pose> cardPoses = new List<Pose>();
        static List<CardColor> cardColours = new List<CardColor>();
        static string[] cardTitles = new string[1000];
        static string[] cardDescriptions = new string[1000];

        Model kanbanBoardModel;
        Model swotBoardModel;
        Model businessModelCanvasBoardModel;

        Model greenCardModel;
        Model blueCardModel;
        Model redCardModel;
        Model yellowCardModel;

        Material greenMaterial;
        Material blueMaterial;
        Material redMaterial;
        Material yellowMaterial;

        HandMenuRadial handMenu;

        static bool showEditWindow = false;
        static int cardEditNumber = 0;

        public void Init()
        {
            defaultSkyTex = Renderer.SkyTex;

            // Create assets used by the app
            floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;

            // Create logo
            logoMaterial = Material.Default.Copy();
            logoMaterial[MatParamName.DiffuseTex] = Tex.FromFile("BoardsVR.png");
            logoMaterial.Transparency = Transparency.Blend;

            // Init boards
            kanbanBoardModel = Model.FromFile("kanban-board.glb");
            swotBoardModel = Model.FromFile("SWOT.glb");
            businessModelCanvasBoardModel = Model.FromFile("business-model-canvas.glb");

            // Tried it with some handwriting fonts but doesn't look very good..
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
            greenCardModel = Model.FromMesh(
                    Mesh.GenerateRoundedCube(new Vec3(0.1f, 0.1f, 0.01f), 0.001f),
                    greenMaterial);

            blueCardModel = Model.FromMesh(
                   Mesh.GenerateRoundedCube(new Vec3(0.1f, 0.1f, 0.01f), 0.001f),
                   blueMaterial);

            redCardModel = Model.FromMesh(
                   Mesh.GenerateRoundedCube(new Vec3(0.1f, 0.1f, 0.01f), 0.001f),
                   redMaterial);

            yellowCardModel = Model.FromMesh(
                   Mesh.GenerateRoundedCube(new Vec3(0.1f, 0.1f, 0.01f), 0.001f),
                   yellowMaterial);

            // Add initial cards
            var exampleCards = Card.GetCardsExampleCards();
            for (int x = 0; x < exampleCards.Count; x++)
            {
                cardPoses.Add(new Pose(0, 0, -0.5f, Quat.LookDir(0, 0, 1)));
                cardColours.Add(exampleCards[x].Colour);
                cardTitles[x] = exampleCards[x].Title;
                cardDescriptions[x] = exampleCards[x].Description;
            }

            // Radial Menu
            Action<string> changeScene = new Action<string>(LoadSkyImage);

            handMenu = SK.AddStepper(new HandMenuRadial(
                new HandRadialLayer("Root",
                    new HandMenuItem("Scene", null, null, "Scene")),
                new HandRadialLayer("Scene",
                    new HandMenuItem("Normal", null, () => changeScene(null)),
                    new HandMenuItem("Farm Field", null, () => changeScene("belfast_farmhouse_4k.hdr")),
                    new HandMenuItem("Night Sky", null, () => changeScene("dikhololo_night_4k.hdr")),
                    new HandMenuItem("St Peters", null, () => changeScene("st_peters_square_night_4k.hdr")),
                    new HandMenuItem("Back", null, null, HandMenuAction.Back)))
                );
        }

        public void Step()
        {
            // Floor grid
            if (SK.System.displayType == Display.Opaque)
                Default.MeshCube.Draw(floorMaterial, floorTransform);

            // Use HandleBegin if you want to be able to move the boards around.
            // Might re-enable this if in edit mode?
            //UI.HandleBegin("KanbanBoard", ref kanbanBoardPose, kanbanBoard.Bounds);
            kanbanBoardModel.Draw(kanbanBoardPose.ToMatrix(0.5f));
            swotBoardModel.Draw(swotBoardPose.ToMatrix(0.5f));
            businessModelCanvasBoardModel.Draw(businessModelCanvasBoardPose.ToMatrix(0.5f));
            //UI.HandleEnd();

            // Draw BoardsVR logo
            Default.MeshQuad.Draw(logoMaterial, logoTransform);

            if (showHandMenus)
            {
                //DrawHandMenu(Handed.Right);
                DrawHandMenu(Handed.Left);
            }

            // For every card
            for (int x = 0; x < cardPoses.Count; x++)
            {
                Pose pose = cardPoses[x];

                UI.Handle($"Cube{x}", ref pose, greenCardModel.Bounds);
                {
                    DrawCard(pose, x, ref cardTitles[x], ref cardDescriptions[x]);

                    // todo: Must be able to refactor this
                    if (cardColours[x] == CardColor.Red)
                    {
                        redCardModel.Draw(pose.ToMatrix());
                    }
                    else if (cardColours[x] == CardColor.Yellow)
                    {
                        yellowCardModel.Draw(pose.ToMatrix());
                    }
                    else if (cardColours[x] == CardColor.Green)
                    {
                        greenCardModel.Draw(pose.ToMatrix());
                    }
                    else if (cardColours[x] == CardColor.Blue)
                    {
                        blueCardModel.Draw(pose.ToMatrix());
                    }
                }

                cardPoses[x] = pose;
            }

            // Show edit window?
            if (showEditWindow)
            {
                // Retreive and show card details..
                UI.WindowBegin($"Edit Card #{cardEditNumber}", ref windowPose, new Vec2(20, 0) * U.cm);

                UI.Label("Title");
                UI.Input("Title", ref cardTitles[cardEditNumber]);

                UI.Label("Description");
                UI.Input("Description", ref cardDescriptions[cardEditNumber]);

                if (UI.Button("Close"))
                {
                    showEditWindow = false;
                }

                UI.WindowEnd();
            }
        }

        // todo: Implement remove card functionality
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
        private static void DrawCard(Pose pose, int number, ref string title, ref string description)
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
                    // Add edit button below card?
                    if (UI.ButtonAt($"Edit #{number}", new Vec3(0.03f, -0.06f, 0), new Vec2(0.06f, 0.03f)))
                    {
                        showEditWindow = true;
                        cardEditNumber = number;
                    }
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

            // And make a hand menu!
            UI.WindowBegin("HandMenu", ref menuPose, size * U.cm, UIWin.Empty);

            var direction = Quat.LookDir(0, 0, 1);

            DrawButton(Color.Hex(0x00FF00FF), "Green", cardPoses, menuPose.position, direction, CardColor.Green);
            DrawButton(Color.Hex(0xFFFF00FF), "Yellow", cardPoses, menuPose.position, direction, CardColor.Yellow);
            DrawButton(Color.Hex(0xFF0000FF), "Red", cardPoses, menuPose.position, direction, CardColor.Red);
            DrawButton(Color.Hex(0x0000FFFF), "Blue", cardPoses, menuPose.position, direction, CardColor.Blue);

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

        static void DrawButton(Color color, string text, List<Pose> poses, Vec3 position, Quat rotation, CardColor cardColour)
        {
            UI.PushTint(color);

            if (UI.Button(text))
            {
                poses.Add(new Pose(position, rotation));
                cardColours.Add(cardColour);
            }

            UI.PopTint();
        }

        void LoadSkyImage(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                Renderer.SkyTex = defaultSkyTex;
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
                // SWOT cards

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
                new Card("Other XR platforms", "", CardColor.Red),



                // Kanban board cards

                // Backlog
                new Card("Enable Scenic Backgrounds","", CardColor.Green),
                new Card("Multi Coloured Cards","", CardColor.Green),
                new Card("Edit Mode","", CardColor.Yellow),
                new Card("Custom Coloured Cards","", CardColor.Yellow),
                new Card("Save and Load Boards & Cards","", CardColor.Red),
                new Card("Remove Cards","", CardColor.Yellow),
                new Card("Multi User Experience","", CardColor.Red),
                new Card("Change Card Colour","", CardColor.Yellow),
                new Card("Create Example Boards","", CardColor.Green),
                new Card("Multiple scene options","", CardColor.Yellow),
                new Card("Better Lighting","", CardColor.Red),
                new Card("Board Titles","", CardColor.Yellow),

                // In Progress

                // Done

                // Business Model Canvas cards

                // Possible other boards
                // Important/Urgent

                /*
                */
            };
        }
    }
}