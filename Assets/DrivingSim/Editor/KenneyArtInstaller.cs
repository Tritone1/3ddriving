#if UNITY_EDITOR
using System;
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
    public static class KenneyArtInstaller
    {
        private const string InstallKey = "DrivingSim.KenneyArtInstaller.v1";
        private const string CarFolder = "Assets/ThirdParty/Kenney/CarKit/FBX/";
        private const string RoadFolder = "Assets/ThirdParty/Kenney/CityKitRoads/FBX/";
        private const string DemoScenePath = "Assets/DrivingSim/Scenes/Demo.unity";

        [MenuItem("Driving Sim/Apply Kenney Art", priority = 30)]
        public static void ApplyKenneyArt()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode, then choose Driving Sim > Apply Kenney Art.");
                return;
            }

            GameObject hatch = LoadModel(CarFolder + "hatchback-sports.fbx");
            GameObject sport = LoadModel(CarFolder + "sedan-sports.fbx");
            GameObject suv = LoadModel(CarFolder + "suv-luxury.fbx");
            GameObject roadStraight = LoadModel(RoadFolder + "road-straight.fbx");
            GameObject roadCrossroad = LoadModel(RoadFolder + "road-crossroad.fbx");

            if (hatch == null || sport == null || suv == null || roadStraight == null || roadCrossroad == null)
            {
                Debug.LogError("Kenney art installation stopped because one or more imported FBX models are missing.");
                return;
            }

            Material carMaterial = CreatePaletteMaterial(
                "Assets/DrivingSim/Materials/KenneyCars.mat",
                CarFolder + "Textures/colormap.png");
            Material roadMaterial = CreatePaletteMaterial(
                "Assets/DrivingSim/Materials/KenneyRoads.mat",
                RoadFolder + "Textures/colormap.png");

            UpgradeCarPrefab("Assets/DrivingSim/Prefabs/Car_city-hatch.prefab", hatch, carMaterial, new Vector3(1.82f, 1.45f, 3.9f));
            UpgradeCarPrefab("Assets/DrivingSim/Prefabs/Car_sport-coupe.prefab", sport, carMaterial, new Vector3(1.88f, 1.35f, 4.15f));
            UpgradeCarPrefab("Assets/DrivingSim/Prefabs/Car_utility-suv.prefab", suv, carMaterial, new Vector3(1.92f, 1.62f, 4.25f));
            UpgradeDemoScene(roadStraight, roadCrossroad, roadMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorPrefs.SetBool(InstallKey, true);
            Debug.Log("DRIVING_SIM_KENNEY_ART_APPLIED: Three vehicle prefabs and the Demo road network now use Kenney CC0 art.");
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
