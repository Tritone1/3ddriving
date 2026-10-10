#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using DrivingSim.Fuel;
using DrivingSim.UI;
using DrivingSim.Vehicles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DrivingSim.EditorTools
{
    /// <summary>
    /// Applies the Quaternius CC0 city art while preserving the generated
    /// WheelCollider rig, missions, fuel system, and input wiring.
    /// </summary>
    [InitializeOnLoad]
    public static class RealisticArtInstaller
    {
        private const string InstallKey = "DrivingSim.RealisticArtInstaller.v21";
        private const string DemoScenePath = "Assets/DrivingSim/Scenes/Demo.unity";
        private const string CarPath = "Assets/ThirdParty/Khronos/CarConcept/CarConcept.glb";
        private const string CityRoot = "Assets/ThirdParty/Quaternius/DowntownCityMegaKit/";
        private const string ModelRoot = CityRoot + "Exports/FBX (Unity)/";
        private const string TextureRoot = CityRoot + "Textures/";
        private const string MaterialRoot = "Assets/DrivingSim/Materials/Realistic/";
        private const string StationRoot = "Assets/ThirdParty/3DAssetsDev/PetrolStation/";
        private static int retryCount;
        private static double nextRetryTime;

        private sealed class CityMaterials
        {
            public Material Asphalt;
            public Material Brick;
            public Material Concrete;
            public Material Metal;
            public Material Trim;
            public Material Roof;
            public Material Dirt;
            public Material Marble;
            public Material Glass;
            public Material Decal;
            public Material Grass;
            public Material Sidewalk;
            public Material MarkingWhite;
            public Material MarkingYellow;
        }

        private sealed class StationModels
        {
            public GameObject FuelPump;
            public GameObject PumpIsland;
            public GameObject Canopy;
            public GameObject CanopyColumn;
        }

        static RealisticArtInstaller()
        {
            EditorApplication.delayCall += TryAutomaticInstall;
        }

        [MenuItem("Driving Sim/Apply Realistic Free Art", priority = 29)]
        public static void ApplyRealisticArt()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode, then choose Driving Sim > Apply Realistic Free Art.");
                return;
            }

            AssetDatabase.Refresh();
            GameObject buildingLarge = LoadModel(ModelRoot + "Building_Large_2.fbx");
            GameObject buildingMedium = LoadModel(ModelRoot + "Building_Medium_2_001.fbx");
            GameObject buildingSmall = LoadModel(ModelRoot + "Building_Small_1.fbx");
            StationModels stationModels = new StationModels
            {
                FuelPump = LoadModel(StationRoot + "FuelPumpTwoHose.glb"),
                PumpIsland = LoadModel(StationRoot + "PumpIsland.glb"),
                Canopy = LoadModel(StationRoot + "CanopySection.glb"),
                CanopyColumn = LoadModel(StationRoot + "CanopyColumn.glb")
            };

            if (buildingLarge == null || buildingMedium == null || buildingSmall == null ||
                stationModels.FuelPump == null || stationModels.PumpIsland == null ||
                stationModels.Canopy == null || stationModels.CanopyColumn == null)
            {
                Debug.LogWarning("Realistic art is still importing. The installer will retry after Unity finishes.");
                ScheduleRetry();
                return;
            }

            EnsureFolder(MaterialRoot.TrimEnd('/'));
            ConfigureNormalMaps();
            CityMaterials materials = CreateCityMaterials();

            RepairRealisticWheelPose("Assets/DrivingSim/Prefabs/Car_city-hatch.prefab");
            RepairRealisticWheelPose("Assets/DrivingSim/Prefabs/Car_sport-coupe.prefab");
            RepairRealisticWheelPose("Assets/DrivingSim/Prefabs/Car_utility-suv.prefab");

            UpgradeDemoScene(new[] { buildingLarge, buildingMedium, buildingSmall }, stationModels, materials);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorPrefs.SetBool(InstallKey, true);
            Debug.Log("DRIVING_SIM_REALISTIC_ART_APPLIED: Downtown buildings, collision, textured roads, mobile lighting, and camera-safe two-pump gas stations installed.");
        }

        private static void TryAutomaticInstall()
        {
            if (EditorPrefs.GetBool(InstallKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ScheduleRetry();
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                return;
            }

            ApplyRealisticArt();
        }

        private static void ScheduleRetry()
        {
            if (retryCount++ >= 40) return;
            nextRetryTime = EditorApplication.timeSinceStartup + 1d;
            EditorApplication.update -= RetryOnEditorUpdate;
            EditorApplication.update += RetryOnEditorUpdate;
        }

        private static void RetryOnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup < nextRetryTime) return;
            EditorApplication.update -= RetryOnEditorUpdate;
            TryAutomaticInstall();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.delayCall += TryAutomaticInstall;
        }

        private static void ConfigureNormalMaps()
        {
            string[] names =
            {
                "T_Concrete_Normal.png", "T_Dirt_Normal.png", "T_MarbleFloor_Normal.png",
                "T_MetalConcrete_Normal.png", "T_Ornaments_Normal.png",
                "T_RedBrick_Normal.png", "T_RoofSlate_Normal.png", "T_Trim_Normal.png"
            };

            foreach (string name in names)
            {
                string path = TextureRoot + name;
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType == TextureImporterType.NormalMap) continue;
                importer.textureType = TextureImporterType.NormalMap;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }
        }

        private static CityMaterials CreateCityMaterials()
        {
            return new CityMaterials
            {
                Asphalt = CreateLit("Asphalt", "T_Concrete_Asphalt_BaseColor.png", "T_Concrete_Normal.png", Color.white, 0f, 0.18f),
                Brick = CreateLit("RedBrick", "T_RedBrick_BaseColor.png", "T_RedBrick_Normal.png", Color.white, 0f, 0.28f),
                Concrete = CreateLit("Concrete", "T_Concrete_BaseColor.png", "T_Concrete_Normal.png", Color.white, 0f, 0.3f),
                Metal = CreateLit("MetalConcrete", "T_MetalConcrete_BaseColor.png", "T_MetalConcrete_Normal.png", Color.white, 0.45f, 0.52f),
                Trim = CreateLit("Trim", "T_Trim_BaseColor.png", "T_Trim_Normal.png", Color.white, 0.05f, 0.38f),
                Roof = CreateLit("RoofSlate", "T_RoofSlate_BaseColor.png", "T_RoofSlate_Normal.png", Color.white, 0f, 0.3f),
                Dirt = CreateLit("Dirt", "T_Dirt_BaseColor.png", "T_Dirt_Normal.png", Color.white, 0f, 0.16f),
                Marble = CreateLit("Marble", "T_MarbleFloor_BaseColor.png", "T_MarbleFloor_Normal.png", Color.white, 0f, 0.45f),
                Glass = CreateEmissive("Windows", "T_dark_interior.png", new Color(0.06f, 0.085f, 0.11f), new Color(0.07f, 0.10f, 0.14f)),
                Decal = CreateLit("StreetDecals", "T_Street_Decals.png", null, Color.white, 0f, 0.25f),
                Grass = CreateLit("UrbanGround", "T_Dirt_BaseColor.png", "T_Dirt_Normal.png", new Color(0.18f, 0.31f, 0.16f), 0f, 0.1f),
                Sidewalk = CreateLit("Sidewalk", null, null, new Color(0.43f, 0.45f, 0.46f), 0f, 0.24f),
                MarkingWhite = CreateLit("RoadMarkingWhite", null, null, new Color(0.88f, 0.87f, 0.8f), 0f, 0.2f),
                MarkingYellow = CreateLit("RoadMarkingYellow", null, null, new Color(0.95f, 0.62f, 0.045f), 0f, 0.2f)
            };
        }

        private static Material CreateLit(string name, string baseTexture, string normalTexture,
            Color tint, float metallic, float smoothness)
        {
            string path = MaterialRoot + name + ".mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            Texture2D albedo = string.IsNullOrEmpty(baseTexture) ? null :
                AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + baseTexture);
            Texture2D normal = string.IsNullOrEmpty(normalTexture) ? null :
                AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + normalTexture);
            SetTexture(material, "_BaseMap", "_MainTex", albedo);
            SetColor(material, tint);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (normal != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateEmissive(string name, string texture, Color tint, Color emission)
        {
            Material material = CreateLit(name, texture, null, tint, 0f, 0.78f);
            Texture2D map = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + texture);
            if (material.HasProperty("_EmissionMap")) material.SetTexture("_EmissionMap", map);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void UpgradeCarPrefab(string prefabPath, GameObject model, Color paint,
            Vector3 targetSize, string variantName)
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
                DisableRenderer(root.transform.Find("KenneyVisual"));

                Transform oldVisual = root.transform.Find("RealisticVisual");
                if (oldVisual != null) UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);

                GameObject visual = InstantiateAsset(model, root.transform);
                if (visual == null) throw new InvalidOperationException("Could not instantiate " + model.name);
                visual.name = "RealisticVisual";
                visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                visual.transform.localScale = Vector3.one;
                RemoveColliders(visual);
                FitCarVisual(root.transform, visual.transform, targetSize);
                Transform trademarkPlate = FindDeepChild(visual.transform, "License Plate");
                if (trademarkPlate != null) trademarkPlate.gameObject.SetActive(false);
                CreateCarMaterialOverrides(visual, paint, variantName);
                WireRealisticWheelVisuals(root, visual);
                ConfigureGameplayFuel(root);

                CarRigReferences rig = root.GetComponent<CarRigReferences>();
                if (rig != null)
                {
                    Renderer[] visualRenderers = visual.GetComponentsInChildren<Renderer>(true);
                    SerializedObject rigSo = new SerializedObject(rig);
                    SerializedProperty paintRenderers = rigSo.FindProperty("paintRenderers");
                    paintRenderers.arraySize = visualRenderers.Length;
                    for (int i = 0; i < visualRenderers.Length; i++)
                        paintRenderers.GetArrayElementAtIndex(i).objectReferenceValue = visualRenderers[i];
                    rigSo.ApplyModifiedPropertiesWithoutUndo();
                }

                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreateCarMaterialOverrides(GameObject visual, Color paint, string variantName)
        {
            string folder = MaterialRoot + "Cars/" + variantName;
            EnsureFolder(folder);
            var clones = new Dictionary<Material, Material>();
            int index = 0;

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] assigned = renderer.sharedMaterials;
                for (int i = 0; i < assigned.Length; i++)
                {
                    Material source = assigned[i];
                    if (source == null) continue;
                    if (!clones.TryGetValue(source, out Material clone))
                    {
                        string safeName = Sanitize(source.name);
                        string path = folder + "/" + index++ + "_" + safeName + ".mat";
                        clone = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (clone == null)
                        {
                            clone = new Material(source) { name = variantName + "_" + source.name };
                            AssetDatabase.CreateAsset(clone, path);
                        }
                        else
                        {
                            clone.CopyPropertiesFromMaterial(source);
                        }

                        ConfigureCarMaterial(clone, source.name, paint);
                        clone.enableInstancing = true;
                        EditorUtility.SetDirty(clone);
                        clones[source] = clone;
                    }
                    assigned[i] = clone;
                }
                renderer.sharedMaterials = assigned;
            }
        }

        private static void ConfigureCarMaterial(Material material, string sourceName, Color paint)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            string label = sourceName ?? string.Empty;

            if (label.IndexOf("Paint", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                material.shader = lit;
                Color finalPaint = label.IndexOf("Paint 2", StringComparison.OrdinalIgnoreCase) >= 0
                    ? Color.Lerp(paint, Color.black, 0.48f)
                    : paint;
                SetColor(material, finalPaint);
                SetTexture(material, "_BaseMap", "_MainTex", null);
                if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", null);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.72f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.82f);
                material.DisableKeyword("_NORMALMAP");
                SetOpaque(material);
                return;
            }

            if (label.IndexOf("Glass", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                material.shader = lit;
                SetTexture(material, "_BaseMap", "_MainTex", null);
                SetColor(material, new Color(0.035f, 0.065f, 0.09f, 0.42f));
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.08f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.92f);
                SetTransparent(material);
            }
        }

        private static void SetOpaque(Material material)
        {
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = (int)RenderQueue.Geometry;
        }

        private static void SetTransparent(Material material)
        {
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void WireRealisticWheelVisuals(GameObject carRoot, GameObject visual)
        {
            CarController controller = carRoot.GetComponent<CarController>();
            if (controller == null) return;

            string[] modelWheelNames = { "WheelFrontL", "WheelFrontR", "WheelRearL", "WheelRearR" };
            var available = new List<Transform>(4);
            foreach (string wheelName in modelWheelNames)
            {
                Transform wheel = FindDeepChild(visual.transform, wheelName);
                if (wheel != null) available.Add(wheel);
            }
            if (available.Count != 4)
            {
                WireOriginalWheelVisuals(carRoot);
                return;
            }

            WheelCollider flCollider = FindWheelCollider(carRoot.transform, "WheelCollider_FL");
            WheelCollider frCollider = FindWheelCollider(carRoot.transform, "WheelCollider_FR");
            WheelCollider rlCollider = FindWheelCollider(carRoot.transform, "WheelCollider_RL");
            WheelCollider rrCollider = FindWheelCollider(carRoot.transform, "WheelCollider_RR");
            if (flCollider == null || frCollider == null || rlCollider == null || rrCollider == null) return;

            RemoveOldWheelDriver(carRoot.transform, "RealWheel_FL");
            RemoveOldWheelDriver(carRoot.transform, "RealWheel_FR");
            RemoveOldWheelDriver(carRoot.transform, "RealWheel_RL");
            RemoveOldWheelDriver(carRoot.transform, "RealWheel_RR");

            Transform fl = TakeClosestWheel(flCollider, available);
            Transform fr = TakeClosestWheel(frCollider, available);
            Transform rl = TakeClosestWheel(rlCollider, available);
            Transform rr = TakeClosestWheel(rrCollider, available);

            // The showcase source model ships with its front wheels posed at an
            // angle. Match each front wheel to the straight rear wheel on the
            // same side before recording the WheelCollider bind-pose offset.
            if (fl != null && rl != null) fl.rotation = rl.rotation;
            if (fr != null && rr != null) fr.rotation = rr.rotation;

            // The generated physics rig used placeholder wheel dimensions. Move
            // each WheelCollider onto the imported wheel pivot and derive its
            // radius from that mesh so the contact patch and visible wheel agree.
            AlignWheelCollider(flCollider, fl);
            AlignWheelCollider(frCollider, fr);
            AlignWheelCollider(rlCollider, rl);
            AlignWheelCollider(rrCollider, rr);

            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty axles = serialized.FindProperty("axles");
            if (axles == null || axles.arraySize < 2) return;
            ConfigureWheelBinding(axles.GetArrayElementAtIndex(0), "left", flCollider, fl);
            ConfigureWheelBinding(axles.GetArrayElementAtIndex(0), "right", frCollider, fr);
            ConfigureWheelBinding(axles.GetArrayElementAtIndex(1), "left", rlCollider, rl);
            ConfigureWheelBinding(axles.GetArrayElementAtIndex(1), "right", rrCollider, rr);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RepairRealisticWheelPose(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform visual = root.transform.Find("RealisticVisual");
                if (visual == null) return;
                WireRealisticWheelVisuals(root, visual.gameObject);
                CarRigReferences rig = root.GetComponent<CarRigReferences>();
                if (rig != null)
                {
                    Renderer[] visualRenderers = visual.GetComponentsInChildren<Renderer>(true);
                    SerializedObject rigSo = new SerializedObject(rig);
                    SerializedProperty paintRenderers = rigSo.FindProperty("paintRenderers");
                    paintRenderers.arraySize = visualRenderers.Length;
                    for (int i = 0; i < visualRenderers.Length; i++)
                        paintRenderers.GetArrayElementAtIndex(i).objectReferenceValue = visualRenderers[i];
                    rigSo.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AlignWheelCollider(WheelCollider collider, Transform visual)
        {
            if (collider == null || visual == null) return;

            collider.transform.position = visual.position;

            Bounds bounds = CalculateWorldBounds(visual.gameObject);
            // World-space Y is unchanged by steering, unlike an axis-aligned X/Z
            // bound, so it gives every matching tire the same physical radius.
            float worldRadius = bounds.extents.y;
            float worldScale = Mathf.Max(0.0001f, Mathf.Abs(collider.transform.lossyScale.y));
            float localRadius = worldRadius / worldScale;
            if (localRadius >= 0.15f && localRadius <= 0.75f)
                collider.radius = localRadius;

            EditorUtility.SetDirty(collider);
        }

        private static Transform TakeClosestWheel(WheelCollider collider, List<Transform> candidates)
        {
            Transform closest = null;
            float closestDistance = float.MaxValue;
            foreach (Transform candidate in candidates)
            {
                float distance = (candidate.position - collider.transform.position).sqrMagnitude;
                if (distance >= closestDistance) continue;
                closestDistance = distance;
                closest = candidate;
            }
            if (closest == null) return null;
            candidates.Remove(closest);
            return closest;
        }

        private static void RemoveOldWheelDriver(Transform carRoot, string name)
        {
            Transform old = carRoot.Find(name);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        }

        private static void ConfigureWheelBinding(SerializedProperty axle, string side,
            WheelCollider collider, Transform visual)
        {
            if (collider == null || visual == null) return;
            axle.FindPropertyRelative(side + "Visual").objectReferenceValue = visual;
            axle.FindPropertyRelative(side + "VisualPositionOffset").vector3Value =
                Quaternion.Inverse(collider.transform.rotation) * (visual.position - collider.transform.position);
            axle.FindPropertyRelative(side + "VisualRotationOffset").quaternionValue =
                Quaternion.Inverse(collider.transform.rotation) * visual.rotation;
        }

        private static WheelCollider FindWheelCollider(Transform root, string name)
        {
            Transform target = root.Find(name);
            return target != null ? target.GetComponent<WheelCollider>() : null;
        }

        private static void WireOriginalWheelVisuals(GameObject carRoot)
        {
            CarController controller = carRoot.GetComponent<CarController>();
            if (controller == null) return;
            Transform fl = carRoot.transform.Find("Wheel_FL");
            Transform fr = carRoot.transform.Find("Wheel_FR");
            Transform rl = carRoot.transform.Find("Wheel_RL");
            Transform rr = carRoot.transform.Find("Wheel_RR");
            if (fl == null || fr == null || rl == null || rr == null) return;

            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty axles = serialized.FindProperty("axles");
            if (axles == null || axles.arraySize < 2) return;
            WheelCollider flCollider = FindWheelCollider(carRoot.transform, "WheelCollider_FL");
            WheelCollider frCollider = FindWheelCollider(carRoot.transform, "WheelCollider_FR");
            WheelCollider rlCollider = FindWheelCollider(carRoot.transform, "WheelCollider_RL");
            WheelCollider rrCollider = FindWheelCollider(carRoot.transform, "WheelCollider_RR");
            ConfigureWheelBinding(axles.GetArrayElementAtIndex(0), "left", flCollider, fl);
            ConfigureWheelBinding(axles.GetArrayElementAtIndex(0), "right", frCollider, fr);
            ConfigureWheelBinding(axles.GetArrayElementAtIndex(1), "left", rlCollider, rl);
            ConfigureWheelBinding(axles.GetArrayElementAtIndex(1), "right", rrCollider, rr);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureGameplayFuel(GameObject carRoot)
        {
            FuelSystem fuel = carRoot.GetComponent<FuelSystem>();
            if (fuel == null) return;

            SerializedObject serialized = new SerializedObject(fuel);
            SerializedProperty multiplier = serialized.FindProperty("gameplayConsumptionMultiplier");
            if (multiplier != null) multiplier.floatValue = 100f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void UpgradeDemoScene(GameObject[] buildings, StationModels stationModels, CityMaterials materials)
        {
            Scene scene = SceneManager.GetSceneByPath(DemoScenePath);
            bool openedForInstall = !scene.IsValid() || !scene.isLoaded;
            if (openedForInstall) scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Additive);

            DestroyRoot(scene, "KenneyEnvironment");
            DestroyRoot(scene, "RealisticEnvironment");
            GameObject environment = new GameObject("RealisticEnvironment");
            SceneManager.MoveGameObjectToScene(environment, scene);

            RestyleBaseScene(scene, materials);
            BuildRoadNetwork(environment.transform, materials);
            BuildCityBlocks(environment.transform, buildings, materials);
            AddStreetProps(environment.transform, materials);
            BuildGasStations(scene, stationModels, materials);
            ImproveHud(scene);
            ImproveLighting(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedForInstall) EditorSceneManager.CloseScene(scene, true);
        }

        private static void RestyleBaseScene(Scene scene, CityMaterials materials)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Ground") AssignMaterial(root, materials.Grass);
                if (root.name.StartsWith("Road_", StringComparison.Ordinal) ||
                    root.name.StartsWith("Intersection_", StringComparison.Ordinal))
                {
                    AssignMaterial(root, materials.Asphalt);
                    Vector3 position = root.transform.position;
                    position.y = 0f;
                    root.transform.position = position;
                    Vector3 scale = root.transform.localScale;
                    scale.y = 0.2f;
                    root.transform.localScale = scale;
                }
            }
        }

        private static void BuildRoadNetwork(Transform parent, CityMaterials materials)
        {
            float[] horizontalRoads = { -42f, 0f, 42f };
            float[] verticalRoads = { -59f, 0f, 59f };
            float[] horizontalSegmentCenters = { -29.5f, 29.5f };
            float[] verticalSegmentCenters = { -21f, 21f };

            // Sidewalks and markings stop before intersections instead of repeating a
            // complete modular road tile across the driving surface.
            foreach (float z in horizontalRoads)
            {
                foreach (float x in horizontalSegmentCenters)
                {
                    CreateHorizontalSidewalk(parent, materials.Sidewalk, x, z, -1f);
                    CreateHorizontalSidewalk(parent, materials.Sidewalk, x, z, 1f);
                    CreateVisualBox(parent, "Yellow Center Line", new Vector3(x, 0.112f, z - 0.13f), new Vector3(47f, 0.025f, 0.11f), materials.MarkingYellow);
                    CreateVisualBox(parent, "Yellow Center Line", new Vector3(x, 0.112f, z + 0.13f), new Vector3(47f, 0.025f, 0.11f), materials.MarkingYellow);
                    CreateVisualBox(parent, "White Edge Line", new Vector3(x, 0.112f, z - 5.15f), new Vector3(47f, 0.025f, 0.12f), materials.MarkingWhite);
                    CreateVisualBox(parent, "White Edge Line", new Vector3(x, 0.112f, z + 5.15f), new Vector3(47f, 0.025f, 0.12f), materials.MarkingWhite);
                }
            }

            foreach (float x in verticalRoads)
            {
                foreach (float z in verticalSegmentCenters)
                {
                    CreateVisualBox(parent, "Sidewalk", new Vector3(x - 6.75f, 0.16f, z), new Vector3(1.5f, 0.22f, 30f), materials.Sidewalk);
                    CreateVisualBox(parent, "Sidewalk", new Vector3(x + 6.75f, 0.16f, z), new Vector3(1.5f, 0.22f, 30f), materials.Sidewalk);
                    CreateVisualBox(parent, "Yellow Center Line", new Vector3(x - 0.13f, 0.112f, z), new Vector3(0.11f, 0.025f, 30f), materials.MarkingYellow);
                    CreateVisualBox(parent, "Yellow Center Line", new Vector3(x + 0.13f, 0.112f, z), new Vector3(0.11f, 0.025f, 30f), materials.MarkingYellow);
                    CreateVisualBox(parent, "White Edge Line", new Vector3(x - 5.15f, 0.112f, z), new Vector3(0.12f, 0.025f, 30f), materials.MarkingWhite);
                    CreateVisualBox(parent, "White Edge Line", new Vector3(x + 5.15f, 0.112f, z), new Vector3(0.12f, 0.025f, 30f), materials.MarkingWhite);
                }
            }

            foreach (float x in verticalRoads)
            {
                foreach (float z in horizontalRoads)
                {
                    for (int stripe = -3; stripe <= 3; stripe++)
                    {
                        float offset = stripe * 1.15f;
                        // On horizontal roads, bars point with X traffic and are spaced across Z.
                        CreateVisualBox(parent, "Crosswalk", new Vector3(x - 7.4f, 0.112f, z + offset), new Vector3(3f, 0.025f, 0.56f), materials.MarkingWhite);
                        CreateVisualBox(parent, "Crosswalk", new Vector3(x + 7.4f, 0.112f, z + offset), new Vector3(3f, 0.025f, 0.56f), materials.MarkingWhite);
                        // On vertical roads, bars point with Z traffic and are spaced across X.
                        CreateVisualBox(parent, "Crosswalk", new Vector3(x + offset, 0.112f, z - 7.4f), new Vector3(0.56f, 0.025f, 3f), materials.MarkingWhite);
                        CreateVisualBox(parent, "Crosswalk", new Vector3(x + offset, 0.112f, z + 7.4f), new Vector3(0.56f, 0.025f, 3f), materials.MarkingWhite);
                    }
                }
            }
        }

        private static void CreateHorizontalSidewalk(Transform parent, Material material,
            float segmentCenterX, float roadZ, float side)
        {
            float sidewalkZ = roadZ + side * 6.75f;
            bool eastStationEntrance = Mathf.Approximately(roadZ, -42f) && side > 0f && segmentCenterX > 0f;
            bool westStationEntrance = Mathf.Approximately(roadZ, 42f) && side < 0f && segmentCenterX < 0f;

            if (eastStationEntrance)
            {
                // Leave x=41.5..53 clear so the east station has a road-level driveway.
                CreateVisualBox(parent, "Sidewalk", new Vector3(23.75f, 0.16f, sidewalkZ),
                    new Vector3(35.5f, 0.22f, 1.5f), material);
                return;
            }

            if (westStationEntrance)
            {
                // Mirror the driveway opening at the west station.
                CreateVisualBox(parent, "Sidewalk", new Vector3(-23.75f, 0.16f, sidewalkZ),
                    new Vector3(35.5f, 0.22f, 1.5f), material);
                return;
            }

            CreateVisualBox(parent, "Sidewalk", new Vector3(segmentCenterX, 0.16f, sidewalkZ),
                new Vector3(47f, 0.22f, 1.5f), material);
        }

        private static void BuildCityBlocks(Transform parent, GameObject[] models, CityMaterials materials)
        {
            Vector3[] positions =
            {
                new Vector3(-48f, 0f, 61f), new Vector3(-24f, 0f, 61f), new Vector3(0f, 0f, 61f),
                new Vector3(24f, 0f, 61f), new Vector3(48f, 0f, 61f),
                new Vector3(-48f, 0f, -61f), new Vector3(-24f, 0f, -61f), new Vector3(0f, 0f, -61f),
                new Vector3(24f, 0f, -61f), new Vector3(48f, 0f, -61f),
                new Vector3(-77f, 0f, -30f), new Vector3(-77f, 0f, 0f), new Vector3(-77f, 0f, 30f),
                new Vector3(77f, 0f, -30f), new Vector3(77f, 0f, 0f), new Vector3(77f, 0f, 30f),
                new Vector3(-29f, 0f, -21f), new Vector3(29f, 0f, -21f),
                new Vector3(-29f, 0f, 21f), new Vector3(29f, 0f, 21f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                float yaw = positions[i].z > 45f ? 180f : positions[i].z < -45f ? 0f :
                    positions[i].x > 65f ? 270f : positions[i].x < -65f ? 90f : (i % 2) * 180f;
                float width = i >= 16 ? 21f : 20f;
                float depth = i >= 16 ? 14f : 16f;
                float height = 19f + (i % 4) * 3.5f;
                CreateBuilding(parent, models[i % models.Length], materials, positions[i], yaw,
                    new Vector3(width, height, depth), "Downtown Building " + (i + 1));
            }
        }

        private static void AddStreetProps(Transform parent, CityMaterials materials)
        {
            GameObject planter = LoadModel(ModelRoot + "Prop_Planter_Single.fbx");
            Vector3[] planterPositions =
            {
                new Vector3(-10f, 0f, -10f), new Vector3(10f, 0f, -10f),
                new Vector3(-10f, 0f, 10f), new Vector3(10f, 0f, 10f),
                new Vector3(-50f, 0f, 10f), new Vector3(50f, 0f, -10f)
            };
            foreach (Vector3 position in planterPositions)
                CreateProp(parent, planter, materials, position, 0f, 0.75f, "Concrete Planter");
        }

        private static void BuildGasStations(Scene scene, StationModels models, CityMaterials materials)
        {
            DestroyRoot(scene, "GasStation_East");
            DestroyRoot(scene, "GasStation_West");

            CreateGasStation(scene, "GasStation_East", new Vector3(48f, 0f, -32f), 0f, models, materials);
            CreateGasStation(scene, "GasStation_West", new Vector3(-48f, 0f, 32f), 180f, models, materials);
        }

        private static void CreateGasStation(Scene scene, string name, Vector3 position, float yaw,
            StationModels models, CityMaterials materials)
        {
            GameObject root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            // The forecourt overlaps the road edge at exactly the same height. There
            // is no driveway cube or raised curb for the WheelColliders to catch on.
            CreateStationBox(root.transform, "Forecourt", new Vector3(0f, 0f, 0f),
                new Vector3(11.5f, 0.2f, 11.5f), materials.Concrete, true);
            CreateStationBox(root.transform, "Shop", new Vector3(0f, 1.65f, 4.45f),
                new Vector3(8.4f, 3.3f, 2.5f), materials.Brick, true);
            CreateStationBox(root.transform, "Shop Fascia", new Vector3(0f, 3.08f, 3.16f),
                new Vector3(8.5f, 0.45f, 0.12f), materials.Trim, false);
            CreateStationBox(root.transform, "Shop Window Left", new Vector3(-2.15f, 1.65f, 3.15f),
                new Vector3(2.9f, 1.75f, 0.08f), materials.Glass, false);
            CreateStationBox(root.transform, "Shop Window Right", new Vector3(2.15f, 1.65f, 3.15f),
                new Vector3(2.9f, 1.75f, 0.08f), materials.Glass, false);

            GameObject canopy = InstantiateStationAsset(models.Canopy, root.transform, "Station Canopy",
                new Vector3(0f, 1.3f, 0.55f), Quaternion.identity, new Vector3(1.2f, 1f, 0.8f));
            DisableCanopyBuiltInSupports(canopy);
            if (canopy != null) canopy.AddComponent<StationCanopyOcclusion>();

            Vector3[] columnPositions =
            {
                new Vector3(-3.25f, 0f, 2.95f), new Vector3(3.25f, 0f, 2.95f)
            };
            for (int i = 0; i < columnPositions.Length; i++)
            {
                InstantiateStationAsset(models.CanopyColumn, root.transform, "Canopy Column " + (i + 1),
                    columnPositions[i], Quaternion.identity, new Vector3(1f, 1.28f, 1f));
                CreateStationCollider(root.transform, "Canopy Column Collider " + (i + 1),
                    columnPositions[i] + Vector3.up * 2.95f, new Vector3(0.65f, 5.9f, 0.65f));
            }
            InstantiateStationAsset(models.PumpIsland, root.transform, "Pump Island", Vector3.zero, Quaternion.identity);
            InstantiateStationAsset(models.FuelPump, root.transform, "Fuel Pump 1",
                new Vector3(-1.35f, 0f, 0f), Quaternion.identity);
            InstantiateStationAsset(models.FuelPump, root.transform, "Fuel Pump 2",
                new Vector3(1.35f, 0f, 0f), Quaternion.identity);

            CreateStationCollider(root.transform, "Pump Collider 1", new Vector3(-1.35f, 1.14f, 0f),
                new Vector3(0.78f, 2.28f, 0.96f));
            CreateStationCollider(root.transform, "Pump Collider 2", new Vector3(1.35f, 1.14f, 0f),
                new Vector3(0.78f, 2.28f, 0.96f));
            CreateStationCollider(root.transform, "Pump Island Collider", new Vector3(0f, 0.22f, 0f),
                new Vector3(4.8f, 0.44f, 1.23f));

            GameObject zone = new GameObject("Highlighted Refuel Zone");
            zone.transform.SetParent(root.transform, false);
            zone.transform.localPosition = new Vector3(0f, 0f, -2.45f);
            BoxCollider zoneCollider = zone.AddComponent<BoxCollider>();
            zoneCollider.isTrigger = true;
            zoneCollider.center = new Vector3(0f, 1.35f, 0f);
            zoneCollider.size = new Vector3(9f, 2.7f, 4.2f);
            zone.AddComponent<GasStation>();
            zone.AddComponent<GasStationZoneIndicator>();
        }

        private static GameObject InstantiateStationAsset(GameObject asset, Transform parent, string name,
            Vector3 localPosition, Quaternion localRotation, Vector3? localScale = null)
        {
            GameObject instance = InstantiateAsset(asset, parent);
            if (instance == null) return null;
            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale ?? Vector3.one;
            RemoveColliders(instance);
            SetStatic(instance);
            return instance;
        }

        private static void DisableCanopyBuiltInSupports(GameObject canopy)
        {
            if (canopy == null) return;
            foreach (Renderer renderer in canopy.GetComponentsInChildren<Renderer>(true))
            {
                string rendererName = renderer.gameObject.name;
                if (rendererName.EndsWith("_0", StringComparison.Ordinal) ||
                    rendererName.EndsWith("_1", StringComparison.Ordinal) ||
                    rendererName.EndsWith("_2", StringComparison.Ordinal) ||
                    rendererName.EndsWith("_3", StringComparison.Ordinal))
                    renderer.enabled = false;
            }
        }

        private static void CreateStationBox(Transform parent, string name, Vector3 localPosition,
            Vector3 scale, Material material, bool keepCollider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = scale;
            AssignMaterial(box, material);
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            SetStatic(box);
        }

        private static void CreateStationCollider(Transform parent, string name, Vector3 localPosition, Vector3 size)
        {
            GameObject colliderObject = new GameObject(name);
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.localPosition = localPosition;
            BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static void CreateRoadPiece(Transform parent, GameObject model, CityMaterials materials,
            Vector3 position, float yaw, float footprint, string name)
        {
            GameObject piece = InstantiateAsset(model, parent);
            if (piece == null) return;
            piece.name = name;
            piece.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            piece.transform.localScale = Vector3.one;
            RemoveColliders(piece);
            AssignCityMaterials(piece, materials);
            Bounds bounds = CalculateWorldBounds(piece);
            piece.transform.localScale *= footprint / Mathf.Max(0.01f, Mathf.Max(bounds.size.x, bounds.size.z));
            bounds = CalculateWorldBounds(piece);
            piece.transform.position += Vector3.up * (position.y - bounds.min.y);
            SetStatic(piece);
        }

        private static void CreateBuilding(Transform parent, GameObject model, CityMaterials materials,
            Vector3 position, float yaw, Vector3 targetSize, string name)
        {
            GameObject building = InstantiateAsset(model, parent);
            if (building == null) return;
            building.name = name;
            building.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            building.transform.localScale = Vector3.one;
            RemoveColliders(building);
            AssignCityMaterials(building, materials);
            Bounds bounds = CalculateWorldBounds(building);
            float scale = Mathf.Min(targetSize.x / Mathf.Max(0.01f, bounds.size.x),
                targetSize.y / Mathf.Max(0.01f, bounds.size.y),
                targetSize.z / Mathf.Max(0.01f, bounds.size.z));
            building.transform.localScale *= scale;
            bounds = CalculateWorldBounds(building);
            building.transform.position += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            AddBuildingCollider(building);
            SetStatic(building);
        }

        private static void AddBuildingCollider(GameObject building)
        {
            Bounds localBounds = CalculateLocalBounds(building.transform, building);
            BoxCollider collider = building.AddComponent<BoxCollider>();
            collider.center = localBounds.center;
            collider.size = localBounds.size;
            collider.isTrigger = false;
        }

        private static void CreateProp(Transform parent, GameObject model, CityMaterials materials,
            Vector3 position, float yaw, float height, string name)
        {
            if (model == null) return;
            GameObject prop = InstantiateAsset(model, parent);
            if (prop == null) return;
            prop.name = name;
            prop.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            prop.transform.localScale = Vector3.one;
            RemoveColliders(prop);
            AssignCityMaterials(prop, materials);
            Bounds bounds = CalculateWorldBounds(prop);
            prop.transform.localScale *= height / Mathf.Max(0.01f, bounds.size.y);
            bounds = CalculateWorldBounds(prop);
            prop.transform.position += Vector3.up * (position.y - bounds.min.y);
            SetStatic(prop);
        }

        private static void ImproveHud(Scene scene)
        {
            GameObject hud = FindRoot(scene, "HUD");
            GameObject systems = FindRoot(scene, "GameSystems");
            MobileInputState input = systems != null ? systems.GetComponent<MobileInputState>() : null;
            if (hud == null || input == null) return;

            Transform reverseTransform = hud.transform.Find("Reverse");
            if (reverseTransform != null) UnityEngine.Object.DestroyImmediate(reverseTransform.gameObject);
            CreateGearSelector(hud.transform, input);
            CreateEngineStartStopButton(hud.transform);

            HudController hudController = hud.GetComponent<HudController>();
            if (hudController != null)
            {
                SerializedObject hudSo = new SerializedObject(hudController);
                SerializedProperty mobileInput = hudSo.FindProperty("mobileInput");
                if (mobileInput != null) mobileInput.objectReferenceValue = input;
                hudSo.ApplyModifiedPropertiesWithoutUndo();
            }

            StylePedalControl(hud.transform.Find("Throttle"), PedalGraphic.PedalStyle.Accelerator,
                new Vector2(-75f, 118f), new Vector2(86f, 162f), "GAS");
            StylePedalControl(hud.transform.Find("Brake"), PedalGraphic.PedalStyle.Brake,
                new Vector2(-190f, 105f), new Vector2(112f, 112f), "BRAKE");

            Transform handbrake = hud.transform.Find("Handbrake");
            if (handbrake != null)
                ConfigureRect(handbrake as RectTransform, new Vector2(1f, 0f), new Vector2(-48f, 280f), new Vector2(66f, 56f));

            Transform moneyTransform = hud.transform.Find("Money");
            if (moneyTransform != null)
            {
                RectTransform moneyRect = moneyTransform as RectTransform;
                ConfigureRect(moneyRect, new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(260f, 56f));
                moneyRect.pivot = Vector2.one;
                Text money = moneyTransform.GetComponent<Text>();
                if (money != null)
                {
                    money.fontSize = 28;
                    money.fontStyle = FontStyle.Bold;
                    money.alignment = TextAnchor.MiddleRight;
                    money.raycastTarget = false;
                }

                Outline outline = moneyTransform.GetComponent<Outline>();
                if (outline == null) outline = moneyTransform.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            Transform panelTransform = hud.transform.Find("FuelPanel");
            if (panelTransform == null)
            {
                GameObject panel = new GameObject("FuelPanel", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(hud.transform, false);
                ConfigureRect(panel.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(-180f, 78f), new Vector2(250f, 92f));
                panel.GetComponent<Image>().color = new Color(0.018f, 0.025f, 0.035f, 0.82f);
                panel.transform.SetAsFirstSibling();
            }

            Transform gaugeBackground = hud.transform.Find("FuelGaugeBackground");
            if (gaugeBackground == null)
            {
                GameObject background = new GameObject("FuelGaugeBackground", typeof(RectTransform), typeof(Image));
                background.transform.SetParent(hud.transform, false);
                ConfigureRect(background.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(-180f, 57f), new Vector2(208f, 22f));
                background.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.065f, 0.96f);
                background.transform.SetSiblingIndex(1);
            }

            Transform fuelTextTransform = hud.transform.Find("Fuel");
            if (fuelTextTransform != null)
            {
                ConfigureRect(fuelTextTransform as RectTransform, new Vector2(0.5f, 0f), new Vector2(-180f, 94f), new Vector2(260f, 42f));
                Text text = fuelTextTransform.GetComponent<Text>();
                if (text != null)
                {
                    text.fontSize = 22;
                    text.alignment = TextAnchor.MiddleCenter;
                }
                fuelTextTransform.SetAsLastSibling();
            }

            Transform fuelFillTransform = hud.transform.Find("FuelFill");
            if (fuelFillTransform != null)
            {
                ConfigureRect(fuelFillTransform as RectTransform, new Vector2(0.5f, 0f), new Vector2(-180f, 57f), new Vector2(200f, 14f));
                Image fill = fuelFillTransform.GetComponent<Image>();
                if (fill != null)
                {
                    fill.color = new Color(0.1f, 0.84f, 0.34f, 1f);
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Horizontal;
                }
                fuelFillTransform.SetAsLastSibling();
            }

            Transform warningTransform = hud.transform.Find("LowFuel");
            if (warningTransform != null)
            {
                ConfigureRect(warningTransform as RectTransform, new Vector2(0.5f, 0f), new Vector2(-180f, 137f), new Vector2(250f, 44f));
                warningTransform.SetAsLastSibling();
            }
        }

        private static void CreateGearSelector(Transform hud, MobileInputState input)
        {
            Transform previous = hud.Find("GearSelector");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            GameObject panel = new GameObject("GearSelector", typeof(RectTransform), typeof(GearSelectorUI));
            panel.transform.SetParent(hud, false);
            ConfigureRect(panel.transform as RectTransform, new Vector2(1f, 0f), new Vector2(-150f, 280f), new Vector2(220f, 300f));

            GameObject interactionObject = new GameObject("InteractionArea", typeof(RectTransform), typeof(Image));
            interactionObject.transform.SetParent(panel.transform, false);
            ConfigureRect(interactionObject.transform as RectTransform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(220f, 300f));
            Image interaction = interactionObject.GetComponent<Image>();
            interaction.color = new Color(0f, 0f, 0f, 0f);
            interaction.raycastTarget = true;

            GearSelectorUI selector = panel.GetComponent<GearSelectorUI>();
            SerializedObject selectorSo = new SerializedObject(selector);
            selectorSo.FindProperty("input").objectReferenceValue = input;
            selectorSo.FindProperty("normalColor").colorValue = new Color(0f, 0f, 0f, 0f);
            selectorSo.FindProperty("selectedColor").colorValue = new Color(1f, 0.28f, 0.02f, 0.5f);
            selectorSo.ApplyModifiedPropertiesWithoutUndo();

            string[] names = { "P", "R", "N", "D" };
            float[] yPositions = { 75f, 25f, -25f, -75f };
            for (int i = 0; i < names.Length; i++)
            {
                GameObject buttonObject = new GameObject(names[i], typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(panel.transform, false);
                ConfigureRect(buttonObject.transform as RectTransform, new Vector2(0.5f, 0.5f),
                    new Vector2(55f, yPositions[i]), new Vector2(52f, 38f));
                Image background = buttonObject.GetComponent<Image>();
                background.color = names[i] == "D"
                    ? new Color(1f, 0.28f, 0.02f, 0.5f)
                    : new Color(0f, 0f, 0f, 0f);
                Button button = buttonObject.GetComponent<Button>();
                button.targetGraphic = background;
                button.transition = Selectable.Transition.None;

                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(buttonObject.transform, false);
                RectTransform labelRect = labelObject.transform as RectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                Text label = labelObject.GetComponent<Text>();
                label.text = names[i];
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 22;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = names[i] == "D" ? Color.black : Color.white;
                label.raycastTarget = false;
            }

            panel.transform.SetAsLastSibling();
        }

        private static void CreateEngineStartStopButton(Transform hud)
        {
            Transform previous = hud.Find("EngineStartStop");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            GameObject buttonObject = new GameObject("EngineStartStop", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(hud, false);
            ConfigureRect(buttonObject.transform as RectTransform, new Vector2(1f, 0f),
                new Vector2(-330f, 430f), new Vector2(200f, 82f));
            Image background = buttonObject.GetComponent<Image>();
            background.color = new Color(0.75f, 0.22f, 0.08f, 0.94f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.transform as RectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            Text label = labelObject.GetComponent<Text>();
            label.text = "PARK TO\nSTOP ENGINE";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            buttonObject.transform.SetAsLastSibling();
        }

        private static void StylePedalControl(Transform control, PedalGraphic.PedalStyle style,
            Vector2 position, Vector2 size, string labelText)
        {
            if (control == null) return;

            ConfigureRect(control as RectTransform, new Vector2(1f, 0f), position, size);

            Image oldBackground = control.GetComponent<Image>();
            if (oldBackground != null) UnityEngine.Object.DestroyImmediate(oldBackground);

            PedalGraphic pedal = control.GetComponent<PedalGraphic>();
            if (pedal == null) pedal = control.gameObject.AddComponent<PedalGraphic>();
            Color pedalColor = style == PedalGraphic.PedalStyle.ReverseGear
                ? new Color(0.12f, 0.22f, 0.34f, 0.96f)
                : new Color(0.34f, 0.37f, 0.40f, 0.98f);
            pedal.Configure(style, pedalColor);
            pedal.raycastTarget = true;

            Shadow shadow = control.GetComponent<Shadow>();
            if (shadow == null) shadow = control.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(3f, -5f);
            shadow.useGraphicAlpha = true;

            Transform previousGrooves = control.Find("PedalGrooves");
            if (previousGrooves != null) UnityEngine.Object.DestroyImmediate(previousGrooves.gameObject);

            if (style != PedalGraphic.PedalStyle.ReverseGear)
            {
                GameObject grooves = new GameObject("PedalGrooves", typeof(RectTransform));
                grooves.transform.SetParent(control, false);
                RectTransform groovesRect = grooves.transform as RectTransform;
                groovesRect.anchorMin = Vector2.zero;
                groovesRect.anchorMax = Vector2.one;
                groovesRect.offsetMin = Vector2.zero;
                groovesRect.offsetMax = Vector2.zero;

                float grooveWidth = style == PedalGraphic.PedalStyle.Accelerator ? 48f : 76f;
                float spacing = style == PedalGraphic.PedalStyle.Accelerator ? 24f : 21f;
                float startY = style == PedalGraphic.PedalStyle.Accelerator ? 38f : 23f;
                for (int i = 0; i < 4; i++)
                {
                    GameObject groove = new GameObject("Grip_" + (i + 1), typeof(RectTransform), typeof(Image));
                    groove.transform.SetParent(grooves.transform, false);
                    ConfigureRect(groove.transform as RectTransform, new Vector2(0.5f, 0.5f),
                        new Vector2(0f, startY - spacing * i), new Vector2(grooveWidth, 5f));
                    Image grooveImage = groove.GetComponent<Image>();
                    grooveImage.color = new Color(0.08f, 0.09f, 0.1f, 0.82f);
                    grooveImage.raycastTarget = false;
                }
            }

            Text label = control.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = labelText;
                label.fontSize = style == PedalGraphic.PedalStyle.ReverseGear ? 18 : 16;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.raycastTarget = false;
                ConfigureRect(label.rectTransform, new Vector2(0.5f, 0.5f),
                    style == PedalGraphic.PedalStyle.ReverseGear ? Vector2.zero : new Vector2(0f, -size.y * 0.34f),
                    new Vector2(size.x, style == PedalGraphic.PedalStyle.ReverseGear ? size.y : 28f));
                label.transform.SetAsLastSibling();
            }
        }

        private static void ConfigureRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            if (rect == null) return;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ImproveLighting(Scene scene)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.62f, 0.72f, 0.82f);
            RenderSettings.fogDensity = 0.0022f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.70f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.39f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.15f, 0.16f);

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                string path = MaterialRoot + "CitySkybox.mat";
                Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (sky == null)
                {
                    sky = new Material(skyShader) { name = "City Skybox" };
                    AssetDatabase.CreateAsset(sky, path);
                }
                sky.SetFloat("_SunSize", 0.035f);
                sky.SetFloat("_AtmosphereThickness", 0.9f);
                sky.SetColor("_SkyTint", new Color(0.55f, 0.68f, 0.86f));
                sky.SetColor("_GroundColor", new Color(0.28f, 0.3f, 0.32f));
                sky.SetFloat("_Exposure", 1.05f);
                RenderSettings.skybox = sky;
                EditorUtility.SetDirty(sky);
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Light light = root.GetComponent<Light>();
                if (light != null && light.type == LightType.Directional)
                {
                    light.color = new Color(1f, 0.94f, 0.84f);
                    light.intensity = 1.15f;
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.82f;
                    root.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
                }

                Camera camera = root.GetComponent<Camera>();
                if (camera != null)
                {
                    camera.fieldOfView = 62f;
                    camera.farClipPlane = 450f;
                    camera.allowHDR = true;
                    camera.allowMSAA = true;
                }
            }

            QualitySettings.shadowDistance = 75f;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.antiAliasing = 4;
        }

        private static void AssignCityMaterials(GameObject root, CityMaterials materials)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] assigned = renderer.sharedMaterials;
                for (int i = 0; i < assigned.Length; i++)
                {
                    string source = ((assigned[i] != null ? assigned[i].name : string.Empty) + " " + renderer.name).ToLowerInvariant();
                    if (source.Contains("decal") || source.Contains("line") || source.Contains("crosswalk")) assigned[i] = materials.Decal;
                    else if (source.Contains("brick")) assigned[i] = materials.Brick;
                    else if (source.Contains("roof") || source.Contains("slate")) assigned[i] = materials.Roof;
                    else if (source.Contains("ornament") || source.Contains("trim") || source.Contains("cornice")) assigned[i] = materials.Trim;
                    else if (source.Contains("metal") || source.Contains("hardware")) assigned[i] = materials.Metal;
                    else if (source.Contains("marble") || source.Contains("floor")) assigned[i] = materials.Marble;
                    else if (source.Contains("dirt")) assigned[i] = materials.Dirt;
                    else if (source.Contains("glass") || source.Contains("window") || source.Contains("interior")) assigned[i] = materials.Glass;
                    else if (source.Contains("street") || source.Contains("asphalt") || source.Contains("road")) assigned[i] = materials.Asphalt;
                    else assigned[i] = materials.Concrete;
                }
                renderer.sharedMaterials = assigned;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private static void FitCarVisual(Transform carRoot, Transform visual, Vector3 targetSize)
        {
            Bounds bounds = CalculateLocalBounds(carRoot, visual.gameObject);
            if (bounds.size.y > bounds.size.x && bounds.size.y > bounds.size.z)
            {
                // CarConcept is imported lengthwise on Y. This maps local height to
                // Unity Y and its front (negative local Y) to Unity forward (+Z).
                visual.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                bounds = CalculateLocalBounds(carRoot, visual.gameObject);
            }
            else if (bounds.size.x > bounds.size.z)
            {
                visual.localRotation = Quaternion.Euler(0f, 90f, 0f);
                bounds = CalculateLocalBounds(carRoot, visual.gameObject);
            }
            float scale = Mathf.Min(targetSize.x / Mathf.Max(0.01f, bounds.size.x),
                targetSize.y / Mathf.Max(0.01f, bounds.size.y),
                targetSize.z / Mathf.Max(0.01f, bounds.size.z));
            visual.localScale *= scale;
            bounds = CalculateLocalBounds(carRoot, visual.gameObject);
            visual.localPosition += new Vector3(0f, 0.1f + bounds.size.y * 0.5f, 0f) - bounds.center;
        }

        private static void CreateVisualBox(Transform parent, string name, Vector3 position,
            Vector3 scale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, true);
            box.transform.SetPositionAndRotation(position, Quaternion.identity);
            box.transform.localScale = scale;
            Collider collider = box.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            AssignMaterial(box, material);
            SetStatic(box);
        }

        private static GameObject InstantiateAsset(GameObject asset, Transform parent)
        {
            if (asset == null) return null;
            GameObject instance = PrefabUtility.InstantiatePrefab(asset, parent) as GameObject;
            return instance != null ? instance : UnityEngine.Object.Instantiate(asset, parent);
        }

        private static GameObject LoadModel(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

        private static void DestroyRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    return;
                }
            }
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
                Material[] assigned = renderer.sharedMaterials;
                for (int i = 0; i < assigned.Length; i++) assigned[i] = material;
                renderer.sharedMaterials = assigned;
            }
        }

        private static void SetTexture(Material material, string primary, string fallback, Texture texture)
        {
            if (material.HasProperty(primary)) material.SetTexture(primary, texture);
            if (material.HasProperty(fallback)) material.SetTexture(fallback, texture);
        }

        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("baseColorFactor")) material.SetColor("baseColorFactor", color);
            material.color = color;
        }

        private static void SetStatic(GameObject root)
        {
            GameObjectUtility.SetStaticEditorFlags(root,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
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

        private static Transform FindDeepChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value.Replace('/', '_').Replace('\\', '_');
        }

        private static void EnsureFolder(string folder)
        {
            string normalized = folder.Replace('\\', '/').TrimEnd('/');
            string[] parts = normalized.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
