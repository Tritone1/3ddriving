#if UNITY_EDITOR
using System;
using DrivingSim.Core;
using DrivingSim.Vehicles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrivingSim.EditorTools
{
    /// <summary>
    /// Connects the CC0 Kenney art packs to the generated gameplay prefabs without
    /// replacing their Rigidbody, WheelCollider, input, fuel, or mission components.
    /// The operation is idempotent and can be repeated from the Driving Sim menu.
    /// </summary>
    [InitializeOnLoad]
    public static class KenneyArtInstaller
    {
        private const string InstallKey = "DrivingSim.KenneyArtInstaller.v2";
        private const string CarFolder = "Assets/ThirdParty/Kenney/CarKit/FBX/";
        private const string RoadFolder = "Assets/ThirdParty/Kenney/CityKitRoads/FBX/";
        private const string DemoScenePath = "Assets/DrivingSim/Scenes/Demo.unity";
        private const string DatabasePath = "Assets/DrivingSim/ScriptableObjects/GameDatabase.asset";
        private const string DataFolder = "Assets/DrivingSim/ScriptableObjects/";
        private const string PrefabFolder = "Assets/DrivingSim/Prefabs/";

        private readonly struct VehicleDefinition
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string ModelName;
            public readonly int Price;
            public readonly string UnlockMissionId;
            public readonly float Mass;
            public readonly float Torque;
            public readonly float Brakes;
            public readonly float TopSpeed;
            public readonly float Grip;
            public readonly float Tank;
            public readonly float Consumption;
            public readonly Vector3 Size;

            public VehicleDefinition(string id, string displayName, string modelName, int price,
                string unlockMissionId, float mass, float torque, float brakes, float topSpeed,
                float grip, float tank, float consumption, Vector3 size)
            {
                Id = id;
                DisplayName = displayName;
                ModelName = modelName;
                Price = price;
                UnlockMissionId = unlockMissionId;
                Mass = mass;
                Torque = torque;
                Brakes = brakes;
                TopSpeed = topSpeed;
                Grip = grip;
                Tank = tank;
                Consumption = consumption;
                Size = size;
            }
        }

        private static readonly VehicleDefinition[] Vehicles =
        {
            new VehicleDefinition("city-hatch", "City Hatch", "hatchback-sports.fbx", 0, "", 1220f, 1250f, 3000f, 175f, 1.05f, 48f, 7.5f, new Vector3(1.82f, 1.45f, 3.9f)),
            new VehicleDefinition("family-sedan", "Family Sedan", "sedan.fbx", 3500, "", 1370f, 1380f, 3200f, 185f, 1.04f, 52f, 8.2f, new Vector3(1.86f, 1.45f, 4.3f)),
            new VehicleDefinition("city-taxi", "City Taxi", "taxi.fbx", 4500, "", 1400f, 1420f, 3300f, 180f, 1.03f, 55f, 8.8f, new Vector3(1.88f, 1.48f, 4.35f)),
            new VehicleDefinition("sport-coupe", "Sport Coupe", "sedan-sports.fbx", 6500, "", 1390f, 1750f, 3800f, 225f, 1.18f, 58f, 10.5f, new Vector3(1.88f, 1.35f, 4.15f)),
            new VehicleDefinition("urban-suv", "Urban SUV", "suv.fbx", 8500, "", 1680f, 1800f, 4000f, 190f, 1.1f, 68f, 11.5f, new Vector3(1.94f, 1.62f, 4.35f)),
            new VehicleDefinition("utility-suv", "Utility SUV", "suv-luxury.fbx", 0, "timed-delivery", 1780f, 1900f, 4200f, 195f, 1.12f, 72f, 12f, new Vector3(1.96f, 1.65f, 4.45f)),
            new VehicleDefinition("cargo-van", "Cargo Van", "van.fbx", 7000, "", 1850f, 1750f, 4100f, 170f, 1.02f, 74f, 12.8f, new Vector3(1.98f, 1.9f, 4.6f)),
            new VehicleDefinition("pickup-truck", "Pickup Truck", "truck.fbx", 9000, "", 1950f, 2050f, 4300f, 180f, 1.08f, 78f, 13.5f, new Vector3(2.02f, 1.78f, 4.7f)),
            new VehicleDefinition("delivery-van", "Delivery Van", "delivery.fbx", 10000, "", 2250f, 2150f, 4500f, 165f, 1f, 86f, 14f, new Vector3(2.08f, 2.2f, 5.1f)),
            new VehicleDefinition("flatbed-truck", "Flatbed Truck", "truck-flat.fbx", 11000, "", 2300f, 2250f, 4700f, 165f, 1.03f, 90f, 15f, new Vector3(2.08f, 1.95f, 5.1f)),
            new VehicleDefinition("police-cruiser", "Police Cruiser", "police.fbx", 12000, "", 1500f, 1950f, 4100f, 230f, 1.2f, 62f, 11f, new Vector3(1.9f, 1.5f, 4.35f)),
            new VehicleDefinition("ambulance", "Ambulance", "ambulance.fbx", 16000, "", 2600f, 2350f, 5000f, 170f, 1.06f, 96f, 16f, new Vector3(2.1f, 2.35f, 5.2f)),
            new VehicleDefinition("track-racer", "Track Racer", "race.fbx", 18000, "", 1180f, 2450f, 4800f, 275f, 1.32f, 56f, 13f, new Vector3(1.92f, 1.18f, 4.25f)),
            new VehicleDefinition("fire-truck", "Fire Truck", "firetruck.fbx", 22000, "", 4200f, 3200f, 6200f, 145f, 0.98f, 120f, 22f, new Vector3(2.35f, 2.7f, 6.2f)),
            new VehicleDefinition("future-racer", "Future Racer", "race-future.fbx", 25000, "", 1120f, 2850f, 5200f, 310f, 1.38f, 54f, 15f, new Vector3(1.96f, 1.12f, 4.35f))
        };

        static KenneyArtInstaller()
        {
            EditorApplication.delayCall += TryAutomaticInstall;
        }

        [MenuItem("Driving Sim/Apply Kenney Art", priority = 30)]
        public static void ApplyKenneyArt()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode, then choose Driving Sim > Apply Kenney Art.");
                return;
            }

            GameObject roadStraight = LoadModel(RoadFolder + "road-straight.fbx");
            GameObject roadCrossroad = LoadModel(RoadFolder + "road-crossroad.fbx");

            if (roadStraight == null || roadCrossroad == null)
            {
                Debug.LogError("Kenney art installation stopped because one or more imported FBX models are missing.");
                return;
            }

            foreach (VehicleDefinition vehicle in Vehicles)
                if (LoadModel(CarFolder + vehicle.ModelName) == null)
                {
                    Debug.LogError("Missing free vehicle model: " + vehicle.ModelName);
                    return;
                }

            Material carMaterial = CreatePaletteMaterial(
                "Assets/DrivingSim/Materials/KenneyCars.mat",
                CarFolder + "Textures/colormap.png");
            Material roadMaterial = CreatePaletteMaterial(
                "Assets/DrivingSim/Materials/KenneyRoads.mat",
                RoadFolder + "Textures/colormap.png");

            CarData[] cars = EnsureVehicleCatalog();
            for (int i = 0; i < Vehicles.Length; i++)
                UpgradeCarPrefab(PrefabFolder + "Car_" + Vehicles[i].Id + ".prefab",
                    LoadModel(CarFolder + Vehicles[i].ModelName), carMaterial, Vehicles[i].Size);
            AssignDatabaseCars(cars);
            UpgradeDemoScene(roadStraight, roadCrossroad, roadMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorPrefs.SetBool(InstallKey, true);
            Debug.Log("DRIVING_SIM_KENNEY_ART_APPLIED: 15 CC0 vehicle prefabs and the Demo road network now use Kenney art.");
        }

        private static void TryAutomaticInstall()
        {
            if (EditorPrefs.GetBool(InstallKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutomaticInstall;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                return;
            }
            ApplyKenneyArt();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.delayCall += TryAutomaticInstall;
        }

        private static CarData[] EnsureVehicleCatalog()
        {
            var cars = new CarData[Vehicles.Length];
            const string templatePrefab = PrefabFolder + "Car_city-hatch.prefab";

            for (int i = 0; i < Vehicles.Length; i++)
            {
                VehicleDefinition definition = Vehicles[i];
                string dataPath = DataFolder + "Car_" + definition.Id + ".asset";
                string prefabPath = PrefabFolder + "Car_" + definition.Id + ".prefab";
                CarData data = AssetDatabase.LoadAssetAtPath<CarData>(dataPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<CarData>();
                    AssetDatabase.CreateAsset(data, dataPath);
                }

                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                {
                    if (!AssetDatabase.CopyAsset(templatePrefab, prefabPath))
                        throw new InvalidOperationException("Could not create vehicle prefab " + prefabPath);
                    AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                SerializedObject dataSo = new SerializedObject(data);
                dataSo.FindProperty("id").stringValue = definition.Id;
                dataSo.FindProperty("displayName").stringValue = definition.DisplayName;
                dataSo.FindProperty("price").intValue = definition.Price;
                dataSo.FindProperty("unlockMissionId").stringValue = definition.UnlockMissionId;
                dataSo.FindProperty("mass").floatValue = definition.Mass;
                dataSo.FindProperty("maxMotorTorque").floatValue = definition.Torque;
                dataSo.FindProperty("maxBrakeTorque").floatValue = definition.Brakes;
                dataSo.FindProperty("topSpeedKph").floatValue = definition.TopSpeed;
                dataSo.FindProperty("tireGrip").floatValue = definition.Grip;
                dataSo.FindProperty("fuelCapacityLitres").floatValue = definition.Tank;
                dataSo.FindProperty("consumptionLitresPer100Km").floatValue = definition.Consumption;
                dataSo.FindProperty("prefab").objectReferenceValue = prefab;
                dataSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);

                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    CarController controller = root.GetComponent<CarController>();
                    if (controller != null)
                    {
                        SerializedObject controllerSo = new SerializedObject(controller);
                        controllerSo.FindProperty("carData").objectReferenceValue = data;
                        controllerSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                    root.name = definition.DisplayName;
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                cars[i] = data;
            }
            return cars;
        }

        private static void AssignDatabaseCars(CarData[] cars)
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null) throw new InvalidOperationException("GameDatabase asset is missing.");
            SerializedObject databaseSo = new SerializedObject(database);
            SerializedProperty list = databaseSo.FindProperty("cars");
            list.arraySize = cars.Length;
            for (int i = 0; i < cars.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = cars[i];
            databaseSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static GameObject LoadModel(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static Material CreatePaletteMaterial(string materialPath, string texturePath)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(materialPath) };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            Texture2D palette = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            material.color = Color.white;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", palette);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", palette);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void UpgradeCarPrefab(string prefabPath, GameObject model, Material material, Vector3 targetSize)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                DisableRenderer(root.transform.Find("Body"));
                DisableRenderer(root.transform.Find("Cabin"));
                DisableRenderer(root.transform.Find("Wheel_FL"));
                DisableRenderer(root.transform.Find("Wheel_FR"));
                DisableRenderer(root.transform.Find("Wheel_RL"));
                DisableRenderer(root.transform.Find("Wheel_RR"));

                Transform realisticVisual = root.transform.Find("RealisticVisual");
                if (realisticVisual != null) UnityEngine.Object.DestroyImmediate(realisticVisual.gameObject);

                Transform oldVisual = root.transform.Find("KenneyVisual");
                if (oldVisual != null) UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);

                GameObject visual = PrefabUtility.InstantiatePrefab(model, root.transform) as GameObject;
                if (visual == null) throw new InvalidOperationException("Could not instantiate " + model.name);
                visual.name = "KenneyVisual";
                visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                visual.transform.localScale = Vector3.one;
                RemoveColliders(visual);
                AssignMaterial(visual, material);
                FitCarVisual(root.transform, visual.transform, targetSize);

                CarRigReferences rig = root.GetComponent<CarRigReferences>();
                if (rig != null)
                {
                    Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
                    SerializedObject rigSo = new SerializedObject(rig);
                    SerializedProperty paintRenderers = rigSo.FindProperty("paintRenderers");
                    paintRenderers.arraySize = renderers.Length;
                    for (int i = 0; i < renderers.Length; i++)
                        paintRenderers.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                    rigSo.ApplyModifiedPropertiesWithoutUndo();
                }

                BoxCollider bodyCollider = root.GetComponent<BoxCollider>();
                if (bodyCollider != null)
                {
                    bodyCollider.center = new Vector3(0f, targetSize.y * 0.48f, 0f);
                    bodyCollider.size = new Vector3(targetSize.x * 0.94f, targetSize.y * 0.82f, targetSize.z * 0.94f);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void FitCarVisual(Transform carRoot, Transform visual, Vector3 targetSize)
        {
            Bounds bounds = CalculateLocalBounds(carRoot, visual.gameObject);
            if (bounds.size.x > bounds.size.z)
            {
                visual.localRotation = Quaternion.Euler(0f, 90f, 0f);
                bounds = CalculateLocalBounds(carRoot, visual.gameObject);
            }

            float scale = Mathf.Min(targetSize.x / Mathf.Max(0.01f, bounds.size.x),
                targetSize.y / Mathf.Max(0.01f, bounds.size.y),
                targetSize.z / Mathf.Max(0.01f, bounds.size.z));
            visual.localScale *= scale;
            bounds = CalculateLocalBounds(carRoot, visual.gameObject);
            Vector3 desiredCenter = new Vector3(0f, 0.12f + bounds.size.y * 0.5f, 0f);
            visual.localPosition += desiredCenter - bounds.center;
        }

        private static void UpgradeDemoScene(GameObject straightModel, GameObject crossroadModel, Material roadMaterial)
        {
            Scene scene = SceneManager.GetSceneByPath(DemoScenePath);
            bool openedForInstall = !scene.IsValid() || !scene.isLoaded;
            if (openedForInstall) scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Additive);

            GameObject existing = FindRoot(scene, "KenneyEnvironment");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

            GameObject environment = new GameObject("KenneyEnvironment");
            SceneManager.MoveGameObjectToScene(environment, scene);

            // The original cubes remain underneath as reliable, inexpensive colliders.
            float[] horizontalZ = { -42f, 0f, 42f };
            float[] verticalX = { -59f, 0f, 59f };
            float[] straightX = { -48f, -36f, -24f, -12f, 12f, 24f, 36f, 48f };
            float[] straightZ = { -30f, -18f, -6f, 6f, 18f, 30f };

            foreach (float z in horizontalZ)
            {
                foreach (float x in straightX)
                    CreateRoadPiece(environment.transform, straightModel, roadMaterial, new Vector3(x, 0.115f, z), 90f, 12f, "Road_H");
            }

            foreach (float x in verticalX)
            {
                foreach (float z in straightZ)
                    CreateRoadPiece(environment.transform, straightModel, roadMaterial, new Vector3(x, 0.12f, z), 0f, 12f, "Road_V");
            }

            foreach (float x in verticalX)
            {
                foreach (float z in horizontalZ)
                    CreateRoadPiece(environment.transform, crossroadModel, roadMaterial, new Vector3(x, 0.125f, z), 0f, 12f, "Intersection");
            }

            AddRoadsideProps(environment.transform, roadMaterial);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedForInstall) EditorSceneManager.CloseScene(scene, true);
        }

        private static void AddRoadsideProps(Transform parent, Material roadMaterial)
        {
            GameObject lightModel = LoadModel(RoadFolder + "light-curved.fbx");
            GameObject signModel = LoadModel(RoadFolder + "road-sign-stop.fbx");
            GameObject barrierModel = LoadModel(RoadFolder + "construction-barrier.fbx");

            Vector3[] lightPositions =
            {
                new Vector3(-48f, 0.12f, -49f), new Vector3(-16f, 0.12f, -49f),
                new Vector3(16f, 0.12f, -49f), new Vector3(48f, 0.12f, -49f),
                new Vector3(-48f, 0.12f, 49f), new Vector3(-16f, 0.12f, 49f),
                new Vector3(16f, 0.12f, 49f), new Vector3(48f, 0.12f, 49f)
            };
            foreach (Vector3 position in lightPositions)
                CreateProp(parent, lightModel, roadMaterial, position, position.z > 0f ? 180f : 0f, 3.2f, "StreetLight");

            CreateProp(parent, signModel, roadMaterial, new Vector3(-7f, 0.12f, -7f), 45f, 2.2f, "StopSign");
            CreateProp(parent, signModel, roadMaterial, new Vector3(7f, 0.12f, 7f), 225f, 2.2f, "StopSign");
            CreateProp(parent, barrierModel, roadMaterial, new Vector3(-66f, 0.12f, -34f), 0f, 2.2f, "Barrier");
            CreateProp(parent, barrierModel, roadMaterial, new Vector3(66f, 0.12f, 34f), 180f, 2.2f, "Barrier");
        }

        private static void CreateRoadPiece(Transform parent, GameObject model, Material material, Vector3 position, float yaw, float footprint, string name)
        {
            GameObject piece = PrefabUtility.InstantiatePrefab(model, parent) as GameObject;
            if (piece == null) return;
            piece.name = name;
            piece.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            piece.transform.localScale = Vector3.one;
            RemoveColliders(piece);
            AssignMaterial(piece, material);
            Bounds bounds = CalculateWorldBounds(piece);
            float largest = Mathf.Max(bounds.size.x, bounds.size.z);
            piece.transform.localScale *= footprint / Mathf.Max(0.01f, largest);
            GameObjectUtility.SetStaticEditorFlags(piece, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        private static void CreateProp(Transform parent, GameObject model, Material material, Vector3 position, float yaw, float targetHeight, string name)
        {
            if (model == null) return;
            GameObject prop = PrefabUtility.InstantiatePrefab(model, parent) as GameObject;
            if (prop == null) return;
            prop.name = name;
            prop.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            prop.transform.localScale = Vector3.one;
            RemoveColliders(prop);
            AssignMaterial(prop, material);
            Bounds bounds = CalculateWorldBounds(prop);
            prop.transform.localScale *= targetHeight / Mathf.Max(0.01f, bounds.size.y);
            bounds = CalculateWorldBounds(prop);
            prop.transform.position += Vector3.up * (position.y - bounds.min.y);
            GameObjectUtility.SetStaticEditorFlags(prop, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static void DisableRenderer(Transform target)
        {
            if (target == null) return;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }

        private static void RemoveColliders(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
        }

        private static void AssignMaterial(GameObject root, Material material)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
        }

        private static Bounds CalculateWorldBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
            Bounds result = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) result.Encapsulate(renderers[i].bounds);
            return result;
        }

        private static Bounds CalculateLocalBounds(Transform reference, GameObject root)
        {
            Bounds world = CalculateWorldBounds(root);
            Vector3 center = reference.InverseTransformPoint(world.center);
            Vector3 size = reference.InverseTransformVector(world.size);
            return new Bounds(center, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)));
        }
    }
}
#endif
