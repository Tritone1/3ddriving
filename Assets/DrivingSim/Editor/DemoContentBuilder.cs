#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using DrivingSim.CameraSystem;
using DrivingSim.Core;
using DrivingSim.Economy;
using DrivingSim.Fuel;
using DrivingSim.Garage;
using DrivingSim.Missions;
using DrivingSim.Save;
using DrivingSim.UI;
using DrivingSim.Vehicles;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace DrivingSim.Editor
{
    public static class DemoContentBuilder
    {
        private const string Root = "Assets/DrivingSim";
        private const string DataPath = Root + "/ScriptableObjects";
        private const string PrefabPath = Root + "/Prefabs";
        private const string MaterialPath = Root + "/Materials";
        private const string ScenePath = Root + "/Scenes";

        [MenuItem("Driving Sim/Create Demo Content")]
        public static void CreateDemoContent()
        {
            EnsureFolders();
            ConfigureMobilePlayerSettings();
            CreateUrpPipeline();
            Material road = CreateMaterial("Road", new Color(0.12f, 0.13f, 0.14f));
            Material grass = CreateMaterial("Grass", new Color(0.16f, 0.38f, 0.14f));
            Material station = CreateMaterial("Station", new Color(0.12f, 0.58f, 0.72f));
            Material marker = CreateMaterial("Marker", new Color(1f, 0.62f, 0.05f));

            UpgradeData[] upgrades =
            {
                CreateUpgrade("engine", "Engine", UpgradeType.Engine, 3, 1800, 1.65f, 0.10f),
                CreateUpgrade("tires", "Sport Tires", UpgradeType.Tires, 3, 1300, 1.6f, 0.09f),
                CreateUpgrade("brakes", "Performance Brakes", UpgradeType.Brakes, 3, 1100, 1.55f, 0.12f),
                CreateUpgrade("turbo", "Turbo", UpgradeType.Turbo, 3, 2600, 1.8f, 0.13f),
                CreateUpgrade("fuel-tank", "Fuel Tank", UpgradeType.FuelTank, 3, 900, 1.5f, 8f)
            };

            MissionData[] missions =
            {
                CreateMission("first-checkpoint", "First Checkpoint", "Follow the ring across town.", MissionType.ReachCheckpoint, "checkpoint-a", 0f, 0f, 1200, ""),
                CreateMission("timed-delivery", "Harbor Delivery", "Reach the depot before time expires.", MissionType.TimedDelivery, "delivery", 0f, 75f, 2600, "utility-suv"),
                CreateMission("clean-run", "Clean Run", "Drive 1.2 km without a hard collision.", MissionType.DistanceWithoutCrash, "", 1200f, 0f, 3400, "")
            };

            CarData hatch = CreateCar("city-hatch", "City Hatch", 0, "", 1220f, 1250f, 3000f, 175f, 1.05f, 48f, 7.5f);
            CarData coupe = CreateCar("sport-coupe", "Sport Coupe", 14500, "", 1390f, 1750f, 3800f, 225f, 1.18f, 58f, 10.5f);
            CarData suv = CreateCar("utility-suv", "Utility SUV", 0, "timed-delivery", 1780f, 1900f, 4200f, 195f, 1.12f, 72f, 12f);
            CarData[] cars = { hatch, coupe, suv };

            Color[] colors = { new Color(0.85f, 0.18f, 0.06f), new Color(0.05f, 0.25f, 0.8f), new Color(0.82f, 0.82f, 0.78f) };
            for (int i = 0; i < cars.Length; i++)
            {
                GameObject prefab = CreateCarPrefab(cars[i], colors[i], i);
                SetObject(cars[i], "prefab", prefab);
            }

            GameDatabase database = LoadOrCreate<GameDatabase>(DataPath + "/GameDatabase.asset");
            SetArray(database, "cars", cars);
            SetArray(database, "missions", missions);
            SetArray(database, "upgrades", upgrades);

            CreateDemoScene(database, road, grass, station, marker);
            CreateGarageScene(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Driving Sim", "Demo content created. Open Assets/DrivingSim/Scenes/Demo.unity and press Play.", "OK");
        }

        [MenuItem("Driving Sim/Validate Generated Content")]
        public static void ValidateGeneratedContent()
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DataPath + "/GameDatabase.asset");
            Require(database != null, "GameDatabase is missing.");
            Require(database.Cars.Count == 3, $"Expected 3 cars, found {database.Cars.Count}.");
            Require(database.Missions.Count == 3, $"Expected 3 missions, found {database.Missions.Count}.");
            Require(database.Upgrades.Count == 5, $"Expected 5 upgrades, found {database.Upgrades.Count}.");

            foreach (CarData car in database.Cars)
            {
                Require(car != null, "Database contains a null car definition.");
                Require(car.Prefab != null, $"{car.DisplayName} has no prefab.");
                Require(car.Prefab.GetComponent<CarController>() != null, $"{car.DisplayName} prefab has no CarController.");
                Require(car.Prefab.GetComponent<FuelSystem>() != null, $"{car.DisplayName} prefab has no FuelSystem.");
                Require(car.Prefab.GetComponentsInChildren<WheelCollider>(true).Length == 4, $"{car.DisplayName} must contain four WheelColliders.");
            }

            Require(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(MaterialPath + "/MobileURP.asset") != null, "URP pipeline asset is missing.");
            Scene demo = EditorSceneManager.OpenScene(ScenePath + "/Demo.unity", OpenSceneMode.Single);
            Require(demo.IsValid(), "Demo scene could not be opened.");
            Require(UnityEngine.Object.FindFirstObjectByType<GameBootstrap>() != null, "Demo scene has no GameBootstrap.");
            Require(UnityEngine.Object.FindFirstObjectByType<MissionManager>() != null, "Demo scene has no MissionManager.");
            Require(UnityEngine.Object.FindFirstObjectByType<HudController>() != null, "Demo scene has no HUD.");
            Require(UnityEngine.Object.FindObjectsByType<GasStation>(FindObjectsSortMode.None).Length == 2, "Demo scene must contain two gas stations.");
            Require(UnityEngine.Object.FindObjectsByType<MissionTarget>(FindObjectsSortMode.None).Length >= 2, "Demo scene needs mission targets.");

            Scene garage = EditorSceneManager.OpenScene(ScenePath + "/Garage.unity", OpenSceneMode.Single);
            Require(garage.IsValid(), "Garage scene could not be opened.");
            Require(UnityEngine.Object.FindFirstObjectByType<GarageController>() != null, "Garage scene has no GarageController.");
            EditorSceneManager.OpenScene(ScenePath + "/Demo.unity", OpenSceneMode.Single);
            Debug.Log("DRIVING_SIM_VALIDATION_PASS: 3 cars, 3 missions, 5 upgrades, 2 scenes, 2 gas stations, URP, HUD, garage, and four-wheel prefabs verified.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Driving Sim validation failed: " + message);
        }

        private static void EnsureFolders()
        {
            string[] folders = { "ScriptableObjects", "Prefabs", "Scenes", "UI", "Materials", "Audio", "Art" };
            foreach (string folder in folders)
            {
                string path = Root + "/" + folder;
                if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(Root, folder);
            }
        }

        private static void ConfigureMobilePlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.resizeableActivity = false;
        }

        private static CarData CreateCar(string id, string display, int price, string unlock, float mass, float torque, float brakes, float topSpeed, float grip, float tank, float consumption)
        {
            CarData data = LoadOrCreate<CarData>($"{DataPath}/Car_{id}.asset");
            Set(data, "id", id); Set(data, "displayName", display); Set(data, "price", price); Set(data, "unlockMissionId", unlock);
            Set(data, "mass", mass); Set(data, "maxMotorTorque", torque); Set(data, "maxBrakeTorque", brakes); Set(data, "topSpeedKph", topSpeed);
            Set(data, "tireGrip", grip); Set(data, "fuelCapacityLitres", tank); Set(data, "consumptionLitresPer100Km", consumption);
            return data;
        }

        private static MissionData CreateMission(string id, string display, string description, MissionType type, string target, float distance, float time, int reward, string unlock)
        {
            MissionData data = LoadOrCreate<MissionData>($"{DataPath}/Mission_{id}.asset");
            Set(data, "id", id); Set(data, "displayName", display); Set(data, "description", description); Set(data, "type", (int)type);
            Set(data, "targetId", target); Set(data, "targetDistanceMetres", distance); Set(data, "timeLimitSeconds", time); Set(data, "rewardMoney", reward); Set(data, "unlockCarId", unlock);
            return data;
        }

        private static UpgradeData CreateUpgrade(string id, string display, UpgradeType type, int levels, int price, float priceMultiplier, float value)
        {
            UpgradeData data = LoadOrCreate<UpgradeData>($"{DataPath}/Upgrade_{id}.asset");
            Set(data, "id", id); Set(data, "displayName", display); Set(data, "type", (int)type); Set(data, "maxLevel", levels);
            Set(data, "basePrice", price); Set(data, "priceMultiplier", priceMultiplier); Set(data, "valuePerLevel", value);
            return data;
        }

        private static GameObject CreateCarPrefab(CarData data, Color color, int style)
        {
            GameObject root = new GameObject(data.DisplayName);
            root.AddComponent<Rigidbody>();
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.65f, 0f);
            box.size = new Vector3(1.85f + style * 0.08f, 1.15f + style * 0.08f, 4.1f + style * 0.25f);
            PlayerCarInput input = root.AddComponent<PlayerCarInput>();
            CarController controller = root.AddComponent<CarController>();
            root.AddComponent<FuelSystem>();
            AudioSource audio = root.AddComponent<AudioSource>();
            audio.spatialBlend = 0.75f;
            root.AddComponent<EngineAudio>();

            Material bodyMaterial = CreateMaterial("Car_" + data.Id, color);
            GameObject body = Primitive("Body", root.transform, PrimitiveType.Cube, new Vector3(0f, 0.78f, 0f), new Vector3(1.8f + style * 0.08f, 0.65f + style * 0.05f, 4f + style * 0.2f), bodyMaterial, false);
            Primitive("Cabin", root.transform, PrimitiveType.Cube, new Vector3(0f, 1.35f, -0.18f), new Vector3(1.55f, 0.62f, 1.85f), bodyMaterial, false);

            float x = 0.9f + style * 0.03f;
            float z = 1.35f + style * 0.1f;
            WheelCollider fl = CreateWheelCollider(root.transform, "WheelCollider_FL", new Vector3(-x, 0.48f, z));
            WheelCollider fr = CreateWheelCollider(root.transform, "WheelCollider_FR", new Vector3(x, 0.48f, z));
            WheelCollider rl = CreateWheelCollider(root.transform, "WheelCollider_RL", new Vector3(-x, 0.48f, -z));
            WheelCollider rr = CreateWheelCollider(root.transform, "WheelCollider_RR", new Vector3(x, 0.48f, -z));
            Transform flv = CreateWheelVisual(root.transform, "Wheel_FL", new Vector3(0, 0.00f, 0));
            Transform frv = CreateWheelVisual(root.transform, "Wheel_FR", new Vector3(0, 0.00f, 0));
            Transform rlv = CreateWheelVisual(root.transform, "Wheel_RL", rl.transform.localPosition);
            Transform rrv = CreateWheelVisual(root.transform, "Wheel_RR", rr.transform.localPosition);

            Transform chase = new GameObject("ChaseAnchor").transform;
            chase.SetParent(root.transform, false); chase.localPosition = new Vector3(0f, 2.1f, -4.3f); chase.localRotation = Quaternion.Euler(14f, 0f, 0f);
            Transform hood = new GameObject("HoodAnchor").transform;
            hood.SetParent(root.transform, false); hood.localPosition = new Vector3(0f, 1.35f, 1.25f);
            CarRigReferences rig = root.AddComponent<CarRigReferences>();

            SerializedObject carSo = new SerializedObject(controller);
            carSo.FindProperty("carData").objectReferenceValue = data;
            carSo.FindProperty("inputSourceComponent").objectReferenceValue = input;
            SerializedProperty axles = carSo.FindProperty("axles");
            axles.arraySize = 2;
            ConfigureAxle(axles.GetArrayElementAtIndex(0), fl, fr, flv, frv, true, true, false);
            ConfigureAxle(axles.GetArrayElementAtIndex(1), rl, rr, rlv, rrv, false, true, true);
            carSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject rigSo = new SerializedObject(rig);
            rigSo.FindProperty("chaseCameraAnchor").objectReferenceValue = chase;
            rigSo.FindProperty("hoodCameraAnchor").objectReferenceValue = hood;
            SerializedProperty renderers = rigSo.FindProperty("paintRenderers");
            renderers.arraySize = 2;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = body.GetComponent<Renderer>();
            renderers.GetArrayElementAtIndex(1).objectReferenceValue = root.transform.Find("Cabin").GetComponent<Renderer>();
            rigSo.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabPath}/Car_{data.Id}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void ConfigureAxle(SerializedProperty axle, WheelCollider left, WheelCollider right, Transform leftVisual, Transform rightVisual, bool steering, bool powered, bool handbrake)
        {
            axle.FindPropertyRelative("leftCollider").objectReferenceValue = left;
            axle.FindPropertyRelative("rightCollider").objectReferenceValue = right;
            axle.FindPropertyRelative("leftVisual").objectReferenceValue = leftVisual;
            axle.FindPropertyRelative("rightVisual").objectReferenceValue = rightVisual;
            axle.FindPropertyRelative("leftVisualPositionOffset").vector3Value =
                Quaternion.Inverse(left.transform.rotation) * (leftVisual.position - left.transform.position);
            axle.FindPropertyRelative("rightVisualPositionOffset").vector3Value =
                Quaternion.Inverse(right.transform.rotation) * (rightVisual.position - right.transform.position);
            axle.FindPropertyRelative("leftVisualRotationOffset").quaternionValue =
                Quaternion.Inverse(left.transform.rotation) * leftVisual.rotation;
            axle.FindPropertyRelative("rightVisualRotationOffset").quaternionValue =
                Quaternion.Inverse(right.transform.rotation) * rightVisual.rotation;
            axle.FindPropertyRelative("steering").boolValue = steering;
            axle.FindPropertyRelative("powered").boolValue = powered;
            axle.FindPropertyRelative("handbrake").boolValue = handbrake;
        }

        private static WheelCollider CreateWheelCollider(Transform parent, string name, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false); go.transform.localPosition = localPosition;
            WheelCollider wheel = go.AddComponent<WheelCollider>();
            wheel.radius = 0.36f; wheel.mass = 24f; wheel.suspensionDistance = 0.2f;
            JointSpring spring = wheel.suspensionSpring; spring.spring = 32000f; spring.damper = 4500f; spring.targetPosition = 0.5f; wheel.suspensionSpring = spring;
            return wheel;
        }

        private static Transform CreateWheelVisual(Transform parent, string name, Vector3 localPosition)
        {
            GameObject wheel = Primitive(name, parent, PrimitiveType.Cylinder, localPosition, new Vector3(0.72f, 0.24f, 0.72f), null, false);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            return wheel.transform;
        }

        private static void CreateDemoScene(GameDatabase database, Material road, Material grass, Material stationMaterial, Material markerMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Primitive("Ground", null, PrimitiveType.Cube, new Vector3(0f, -0.55f, 0f), new Vector3(180f, 1f, 140f), grass, true);
            Primitive("Road_North", null, PrimitiveType.Cube, new Vector3(0f, 0f, 42f), new Vector3(130f, 0.2f, 12f), road, true);
            Primitive("Road_South", null, PrimitiveType.Cube, new Vector3(0f, 0f, -42f), new Vector3(130f, 0.2f, 12f), road, true);
            Primitive("Road_East", null, PrimitiveType.Cube, new Vector3(59f, 0f, 0f), new Vector3(12f, 0.2f, 84f), road, true);
            Primitive("Road_West", null, PrimitiveType.Cube, new Vector3(-59f, 0f, 0f), new Vector3(12f, 0.2f, 84f), road, true);
            // Keep every driveable collider at exactly the same surface height. Even
            // a 2 cm step is visible as a bump when a WheelCollider crosses it.
            Primitive("Intersection_NS", null, PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(12f, 0.2f, 84f), road, true);
            Primitive("Intersection_EW", null, PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(130f, 0.2f, 12f), road, true);

            GameObject spawn = new GameObject("PlayerSpawn"); spawn.transform.SetPositionAndRotation(new Vector3(-38f, 1f, -42f), Quaternion.Euler(0f, 90f, 0f));
            CreateTarget("checkpoint-a", new Vector3(48f, 0f, 42f));
            CreateTarget("delivery", new Vector3(-58f, 0f, 28f));
            CreateGasStation("GasStation_East", new Vector3(48f, 1.5f, -34f), stationMaterial);
            CreateGasStation("GasStation_West", new Vector3(-48f, 1.5f, 34f), stationMaterial);

            GameObject systems = new GameObject("GameSystems");
            SaveService saves = systems.AddComponent<SaveService>();
            EconomyService economy = systems.AddComponent<EconomyService>();
            MissionManager missionManager = systems.AddComponent<MissionManager>();
            MobileInputState mobile = systems.AddComponent<MobileInputState>();
            Set(mobile, "mode", (int)MobileInputState.SteeringMode.Buttons);
            GameBootstrap bootstrap = systems.AddComponent<GameBootstrap>();
            systems.AddComponent<MobileQualityPreset>();

            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            Camera camera = cameraGo.AddComponent<Camera>(); camera.fieldOfView = 65f; camera.farClipPlane = 350f;
            cameraGo.AddComponent<AudioListener>();
            DrivingCameraController drivingCamera = cameraGo.AddComponent<DrivingCameraController>();
            cameraGo.transform.position = new Vector3(-38f, 5f, -50f);

            GameObject sun = new GameObject("Directional Light");
            Light light = sun.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.15f; light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            HudController hud = CreateHud(mobile, missionManager, saves);
            GameObject marker = Primitive("MissionMarker", null, PrimitiveType.Cylinder, Vector3.up * 3f, new Vector3(3f, 0.2f, 3f), markerMaterial, false);
            MissionWaypointMarker markerController = marker.AddComponent<MissionWaypointMarker>();
            SetObject(markerController, "missionManager", missionManager);

            SerializedObject bootSo = new SerializedObject(bootstrap);
            bootSo.FindProperty("database").objectReferenceValue = database;
            bootSo.FindProperty("saveService").objectReferenceValue = saves;
            bootSo.FindProperty("economy").objectReferenceValue = economy;
            bootSo.FindProperty("missionManager").objectReferenceValue = missionManager;
            bootSo.FindProperty("mobileInput").objectReferenceValue = mobile;
            bootSo.FindProperty("drivingCamera").objectReferenceValue = drivingCamera;
            bootSo.FindProperty("hud").objectReferenceValue = hud;
            bootSo.FindProperty("spawnPoint").objectReferenceValue = spawn.transform;
            bootSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject economySo = new SerializedObject(economy);
            economySo.FindProperty("saveService").objectReferenceValue = saves; economySo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath + "/Demo.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath + "/Demo.unity", true),
                new EditorBuildSettingsScene(ScenePath + "/Garage.unity", true)
            };
        }

        private static HudController CreateHud(MobileInputState mobile, MissionManager missions, SaveService saves)
        {
            GameObject canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920f, 1080f);
            HudController hud = canvasGo.AddComponent<HudController>();
            Text speed = CreateText(canvasGo.transform, "Speed", "000 km/h", new Vector2(0.5f, 0f), new Vector2(0f, 90f), 34, TextAnchor.MiddleCenter);
            Text gear = CreateText(canvasGo.transform, "Gear", "G1", new Vector2(0.5f, 0f), new Vector2(0f, 48f), 24, TextAnchor.MiddleCenter);
            Text fuel = CreateText(canvasGo.transform, "Fuel", "FUEL 48.0 / 48.0 L  100%", new Vector2(0.5f, 0f), new Vector2(-180f, 92f), 24, TextAnchor.MiddleCenter);
            Text money = CreateText(canvasGo.transform, "Money", "$5,000", new Vector2(1f, 1f), new Vector2(-150f, -55f), 28, TextAnchor.MiddleRight);
            money.rectTransform.pivot = Vector2.one;
            money.rectTransform.anchoredPosition = new Vector2(-28f, -24f);
            money.rectTransform.sizeDelta = new Vector2(260f, 56f);
            Text mission = CreateText(canvasGo.transform, "Mission", "Mission", new Vector2(0.5f, 1f), new Vector2(0f, -80f), 26, TextAnchor.MiddleCenter);
            Text warning = CreateText(canvasGo.transform, "LowFuel", "LOW FUEL", new Vector2(0.5f, 0f), new Vector2(-180f, 135f), 26, TextAnchor.MiddleCenter);
            warning.color = new Color(1f, 0.25f, 0.1f);
            Image fuelFill = CreateImage(canvasGo.transform, "FuelFill", new Vector2(0.5f, 0f), new Vector2(-180f, 58f), new Vector2(200f, 16f), new Color(0.15f, 0.85f, 0.25f));
            fuelFill.type = Image.Type.Filled; fuelFill.fillMethod = Image.FillMethod.Horizontal;
            Image arrow = CreateImage(canvasGo.transform, "DirectionArrow", new Vector2(0.5f, 0.5f), new Vector2(0f, 270f), new Vector2(38f, 70f), new Color(1f, 0.65f, 0f));

            CreateHoldButton(canvasGo.transform, "Throttle", "GAS", new Vector2(1f, 0f), new Vector2(-110f, 150f), mobile, HoldButton.Action.Throttle);
            CreateHoldButton(canvasGo.transform, "Brake", "BRAKE", new Vector2(1f, 0f), new Vector2(-280f, 105f), mobile, HoldButton.Action.Brake);
            CreateGearSelector(canvasGo.transform, mobile);
            CreateHoldButton(canvasGo.transform, "Left", "<", new Vector2(0f, 0f), new Vector2(90f, 110f), mobile, HoldButton.Action.Left);
            CreateHoldButton(canvasGo.transform, "Right", ">", new Vector2(0f, 0f), new Vector2(250f, 110f), mobile, HoldButton.Action.Right);
            CreateHoldButton(canvasGo.transform, "Handbrake", "HB", new Vector2(1f, 0f), new Vector2(-110f, 310f), mobile, HoldButton.Action.Handbrake);
            CreateHoldButton(canvasGo.transform, "Refuel", "REFUEL / R", new Vector2(0f, 0.5f), new Vector2(110f, 0f), mobile, HoldButton.Action.Refuel);

            GameObject missionPanel = CreateImage(canvasGo.transform, "MissionPanel", new Vector2(0f, 1f), new Vector2(260f, -250f), new Vector2(470f, 250f), new Color(0f, 0f, 0f, 0.65f)).gameObject;
            Text missionTitle = CreateText(missionPanel.transform, "Title", "Mission", new Vector2(0.5f, 1f), new Vector2(0f, -35f), 25, TextAnchor.MiddleCenter);
            Text missionDescription = CreateText(missionPanel.transform, "Description", "Choose a mission", new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), 18, TextAnchor.MiddleCenter);
            Text missionReward = CreateText(missionPanel.transform, "Reward", "Reward", new Vector2(0.5f, 0.5f), new Vector2(0f, -15f), 18, TextAnchor.MiddleCenter);
            Text missionResult = CreateText(canvasGo.transform, "MissionResult", "SUCCESS", new Vector2(0.5f, 0.5f), Vector2.zero, 46, TextAnchor.MiddleCenter);
            missionResult.gameObject.SetActive(false);
            MissionView missionView = missionPanel.AddComponent<MissionView>();
            SerializedObject missionSo = new SerializedObject(missionView);
            missionSo.FindProperty("manager").objectReferenceValue = missions;
            missionSo.FindProperty("title").objectReferenceValue = missionTitle;
            missionSo.FindProperty("description").objectReferenceValue = missionDescription;
            missionSo.FindProperty("reward").objectReferenceValue = missionReward;
            missionSo.FindProperty("result").objectReferenceValue = missionResult;
            missionSo.ApplyModifiedPropertiesWithoutUndo();
            Button previousMission = CreateButton(missionPanel.transform, "Previous", "<", new Vector2(0f, 0f), new Vector2(55f, 35f), new Vector2(70f, 55f));
            Button nextMission = CreateButton(missionPanel.transform, "Next", ">", new Vector2(1f, 0f), new Vector2(-55f, 35f), new Vector2(70f, 55f));
            Button startMission = CreateButton(missionPanel.transform, "Start", "START", new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(170f, 55f));
            UnityEventTools.AddPersistentListener(previousMission.onClick, missionView.Previous);
            UnityEventTools.AddPersistentListener(nextMission.onClick, missionView.Next);
            UnityEventTools.AddPersistentListener(startMission.onClick, missionView.StartSelected);

            GameObject mainMenu = CreateImage(canvasGo.transform, "MainMenu", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 340f), new Color(0.02f, 0.03f, 0.05f, 0.94f)).gameObject;
            CreateText(mainMenu.transform, "Title", "MOBILE DRIVING", new Vector2(0.5f, 1f), new Vector2(0f, -80f), 38, TextAnchor.MiddleCenter);
            Button playButton = CreateButton(mainMenu.transform, "Play", "DRIVE", new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(260f, 75f));
            GameObject pauseMenu = CreateImage(canvasGo.transform, "PauseMenu", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 260f), new Color(0.02f, 0.03f, 0.05f, 0.94f)).gameObject;
            pauseMenu.SetActive(false);
            CreateText(pauseMenu.transform, "Title", "PAUSED", new Vector2(0.5f, 1f), new Vector2(0f, -55f), 34, TextAnchor.MiddleCenter);
            Button resumeButton = CreateButton(pauseMenu.transform, "Resume", "RESUME", new Vector2(0.5f, 0.5f), new Vector2(0f, -15f), new Vector2(220f, 65f));
            Button pauseButton = CreateButton(canvasGo.transform, "Pause", "II", new Vector2(1f, 1f), new Vector2(-55f, -130f), new Vector2(70f, 60f));
            GameObject settingsPanel = new GameObject("SettingsPanel"); settingsPanel.transform.SetParent(canvasGo.transform, false); settingsPanel.SetActive(false);
            MenuController menu = canvasGo.AddComponent<MenuController>();
            SerializedObject menuSo = new SerializedObject(menu);
            menuSo.FindProperty("mainMenu").objectReferenceValue = mainMenu;
            menuSo.FindProperty("pauseMenu").objectReferenceValue = pauseMenu;
            menuSo.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            menuSo.FindProperty("saveService").objectReferenceValue = saves;
            menuSo.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(playButton.onClick, menu.Play);
            UnityEventTools.AddBoolPersistentListener(pauseButton.onClick, menu.SetPaused, true);
            UnityEventTools.AddBoolPersistentListener(resumeButton.onClick, menu.SetPaused, false);

            SerializedObject hudSo = new SerializedObject(hud);
            hudSo.FindProperty("missions").objectReferenceValue = missions;
            hudSo.FindProperty("mobileInput").objectReferenceValue = mobile;
            hudSo.FindProperty("speedText").objectReferenceValue = speed;
            hudSo.FindProperty("gearText").objectReferenceValue = gear;
            hudSo.FindProperty("fuelText").objectReferenceValue = fuel;
            hudSo.FindProperty("fuelFill").objectReferenceValue = fuelFill;
            hudSo.FindProperty("lowFuelWarning").objectReferenceValue = warning.gameObject;
            hudSo.FindProperty("moneyText").objectReferenceValue = money;
            hudSo.FindProperty("missionText").objectReferenceValue = mission;
            hudSo.FindProperty("directionArrow").objectReferenceValue = arrow.rectTransform;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return hud;
        }

        private static void CreateGarageScene(GameDatabase database)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject systems = new GameObject("GarageSystems");
            SaveService saves = systems.AddComponent<SaveService>();
            EconomyService economy = systems.AddComponent<EconomyService>();
            GarageController garage = systems.AddComponent<GarageController>();
            Transform preview = new GameObject("PreviewTurntable").transform; preview.gameObject.AddComponent<PreviewTurntable>();

            GameObject cameraGo = new GameObject("Main Camera"); cameraGo.tag = "MainCamera";
            Camera camera = cameraGo.AddComponent<Camera>(); cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.SetPositionAndRotation(new Vector3(0f, 2.2f, -7f), Quaternion.Euler(8f, 0f, 0f));
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            GameObject lightGo = new GameObject("Key Light"); Light light = lightGo.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.5f; lightGo.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

            garage.Configure(database, saves, economy, preview);
            economy.Configure(saves);
            EditorSceneManager.SaveScene(scene, ScenePath + "/Garage.unity");
        }

        private static void CreateTarget(string id, Vector3 position)
        {
            GameObject go = new GameObject("Target_" + id); go.transform.position = position;
            MissionTarget target = go.AddComponent<MissionTarget>(); Set(target, "id", id);
        }

        private static void CreateGasStation(string name, Vector3 position, Material material)
        {
            GameObject go = Primitive(name, null, PrimitiveType.Cube, position, new Vector3(8f, 3f, 8f), material, true);
            Collider collider = go.GetComponent<Collider>(); collider.isTrigger = true;
            go.AddComponent<GasStation>();
        }

        private static GameObject Primitive(string name, Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 scale, Material material, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type); go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition; go.transform.localScale = scale;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static Text CreateText(Transform parent, string name, string value, Vector2 anchor, Vector2 position, int fontSize, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = new Vector2(420f, 80f); rect.anchoredPosition = position;
            Text text = go.GetComponent<Text>(); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize; text.alignment = alignment; text.color = Color.white;
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size; rect.anchoredPosition = position;
            Image image = go.GetComponent<Image>(); image.color = color; return image;
        }

        private static void CreateHoldButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, MobileInputState input, HoldButton.Action action)
        {
            Image image = CreateImage(parent, name, anchor, position, new Vector2(135f, 110f), new Color(0f, 0f, 0f, 0.5f));
            HoldButton button = image.gameObject.AddComponent<HoldButton>();
            SetObject(button, "input", input); Set(button, "action", (int)action);
            Text text = CreateText(image.transform, "Label", label, new Vector2(0.5f, 0.5f), Vector2.zero, 22, TextAnchor.MiddleCenter); text.raycastTarget = false;
        }

        private static void CreateGearSelector(Transform parent, MobileInputState input)
        {
            const string shifterPath = "Assets/DrivingSim/UI/Generated/AutomaticShifter.png";
            Sprite artwork = AssetDatabase.LoadAssetAtPath<Sprite>(shifterPath);
            Image panel = CreateImage(parent, "GearSelector", new Vector2(1f, 0f), new Vector2(-225f, 350f),
                new Vector2(420f, 420f), Color.white);
            panel.sprite = artwork;
            panel.preserveAspect = true;
            panel.raycastTarget = false;
            GearSelectorUI selector = panel.gameObject.AddComponent<GearSelectorUI>();
            SetObject(selector, "input", input);

            string[] gears = { "P", "R", "N", "D" };
            Image interaction = CreateImage(panel.transform, "InteractionArea", new Vector2(0.5f, 0.5f),
                new Vector2(52f, -40f), new Vector2(78f, 178f), new Color(0f, 0f, 0f, 0f));
            interaction.raycastTarget = true;

            float[] yPositions = { 17f, -20f, -58f, -96f };
            for (int i = 0; i < gears.Length; i++)
            {
                Image hitArea = CreateImage(panel.transform, gears[i], new Vector2(0.5f, 0.5f),
                    new Vector2(52f, yPositions[i]), new Vector2(44f, 32f),
                    gears[i] == "D" ? new Color(1f, 0.28f, 0.02f, 0.5f) : new Color(0f, 0f, 0f, 0f));
                Button button = hitArea.gameObject.AddComponent<Button>();
                button.targetGraphic = hitArea;
                button.transition = Selectable.Transition.None;
            }
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image image = CreateImage(parent, name, anchor, position, size, new Color(0.12f, 0.35f, 0.68f, 0.95f));
            Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Text text = CreateText(image.transform, "Label", label, new Vector2(0.5f, 0.5f), Vector2.zero, 20, TextAnchor.MiddleCenter); text.raycastTarget = false;
            return button;
        }

        private static void CreateUrpPipeline()
        {
            string pipelinePath = MaterialPath + "/MobileURP.asset";
            RenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                Type pipelineType = FindType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset");
                Type rendererType = FindType("UnityEngine.Rendering.Universal.UniversalRendererData");
                if (pipelineType == null || rendererType == null)
                {
                    Debug.LogWarning("URP package is not imported yet. Re-run Create Demo Content after Package Manager finishes.");
                    return;
                }
                ScriptableObject renderer = ScriptableObject.CreateInstance(rendererType);
                AssetDatabase.CreateAsset(renderer, MaterialPath + "/MobileRenderer.asset");
                pipeline = ScriptableObject.CreateInstance(pipelineType) as RenderPipelineAsset;
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
                SerializedObject pipelineSo = new SerializedObject(pipeline);
                SerializedProperty list = pipelineSo.FindProperty("m_RendererDataList");
                if (list != null)
                {
                    list.arraySize = 1;
                    list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                }
                SerializedProperty defaultRenderer = pipelineSo.FindProperty("m_DefaultRendererIndex");
                if (defaultRenderer != null) defaultRenderer.intValue = 0;
                pipelineSo.ApplyModifiedPropertiesWithoutUndo();
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }

        private static Type FindType(string fullName)
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null) return type;
            }
            return null;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = $"{MaterialPath}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader); AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color); material.color = color; EditorUtility.SetDirty(material); return material;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }

        private static void Set(UnityEngine.Object target, string property, string value) { SerializedObject so = new SerializedObject(target); so.FindProperty(property).stringValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Set(UnityEngine.Object target, string property, int value) { SerializedObject so = new SerializedObject(target); so.FindProperty(property).intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Set(UnityEngine.Object target, string property, float value) { SerializedObject so = new SerializedObject(target); so.FindProperty(property).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetObject(UnityEngine.Object target, string property, UnityEngine.Object value) { SerializedObject so = new SerializedObject(target); so.FindProperty(property).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }

        private static void SetArray<T>(UnityEngine.Object target, string property, T[] values) where T : UnityEngine.Object
        {
            SerializedObject so = new SerializedObject(target); SerializedProperty array = so.FindProperty(property); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
    }
}
#endif
