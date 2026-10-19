using System;
using System.IO;
using System.Linq;
using SpaghettiWires.CameraControl;
using SpaghettiWires.DebugTools;
using SpaghettiWires.Game;
using SpaghettiWires.Map;
using SpaghettiWires.Sandbox;
using SpaghettiWires.Simulation;
using SpaghettiWires.Tools;
using SpaghettiWires.UI;
using SpaghettiWires.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SpaghettiWires.EditorTools
{
    /// <summary>
    /// Buduje scenę gry z siatką, widokami, narzędziami i paskiem narzędzi oraz zastępcze modele
    /// domu i centrali. Słup to drewniany słup z <see cref="WoodenPoleBuilder"/>.
    /// Uruchamiane raz; dalej scenę można edytować ręcznie.
    /// </summary>
    public static class GameSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string PlaceholdersFolder = "Assets/_Project/Prefabs/Placeholders";
        private const string MaterialsFolder = "Assets/_Project/Materials/Placeholders";
        private const string UIFolder = "Assets/_Project/UI";
        private const string VolumeProfilePath = "Assets/_Project/Settings/SampleSceneProfile.asset";

        private const float CellSize = 4f;
        private static readonly Vector2Int GridSize = new Vector2Int(25, 25);

        /// <summary>Przebudowuje tylko modele węzłów (dom, słup, centrala). Scena odwołuje się do nich, więc zmiany widać od razu.</summary>
        [MenuItem("Tools/Spaghetti Wires/Zbuduj modele węzłów")]
        public static void BuildNodePrefabs()
        {
            var materials = new Materials();
            BuildHousePrefab(materials);
            WoodenPoleBuilder.Build();
            BuildExchangePrefab(materials);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Spaghetti Wires/Zbuduj scenę gry")]
        public static void Build()
        {
            BuildNodePrefabs();
            var materials = new Materials();
            var panelSettings = BuildPanelSettings();
            AssetDatabase.SaveAssets();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Nowa scena zwalnia nieużywane assety, więc prefaby wczytujemy dopiero teraz.
            var house = LoadNodePrefab("House");
            var pole = AssetDatabase.LoadAssetAtPath<NodeView>(WoodenPoleBuilder.PrefabPath)
                ?? throw new InvalidOperationException("Wooden pole prefab was not saved.");
            var exchange = LoadNodePrefab("Exchange");

            var camera = CreateCamera();
            CreateLight();
            CreateVolume();
            CreateGround(materials);

            var grid = new GameObject("Map").AddComponent<MapGridBehaviour>();
            SetField(grid, "cellSize", CellSize);
            SetField(grid, "size", GridSize);

            var overlay = CreateChild<GridOverlay>(grid.transform, "GridOverlay");
            overlay.GetComponent<MeshRenderer>().sharedMaterial = materials.Overlay;
            overlay.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            SetField(overlay, "grid", grid);

            var cellHighlight = CreateChild<CellHighlight>(grid.transform, "CellHighlight");
            var marker = CreatePrimitive(PrimitiveType.Cube, "Marker", cellHighlight.transform, Vector3.zero, Vector3.one, materials.Overlay);
            marker.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            SetField(cellHighlight, "grid", grid);
            SetField(cellHighlight, "marker", marker.GetComponent<MeshRenderer>());

            var views = new GameObject("Views").AddComponent<ViewRegistry>();
            SetField(views, "grid", grid);
            SetField(views, "housePrefab", house);
            SetField(views, "polePrefab", pole);
            SetField(views, "exchangePrefab", exchange);
            SetField(views, "cableMaterial", materials.Cable);

            var sandbox = new GameObject("Sandbox (tymczasowe)").AddComponent<SandboxBuildActions>();
            SetField(sandbox, "grid", grid);
            SetField(sandbox, "views", views);

            var ui = new GameObject("UI").AddComponent<UIDocument>();
            ui.panelSettings = panelSettings;

            var toolsObject = new GameObject("Tools");
            var pointer = toolsObject.AddComponent<ScenePointer>();
            SetField(pointer, "sceneCamera", camera);
            SetField(pointer, "grid", grid);
            SetField(pointer, "views", views);

            var preview = CreateChild<CablePreview>(toolsObject.transform, "CablePreview");
            var previewLine = preview.GetComponent<LineRenderer>();
            previewLine.sharedMaterial = materials.Cable;
            previewLine.enabled = false;

            var tools = toolsObject.AddComponent<ToolController>();
            SetField(tools, "pointer", pointer);
            SetField(tools, "gridOverlay", overlay);
            SetField(tools, "cellHighlight", cellHighlight);
            SetField(tools, "cablePreview", preview);

            var toolbar = ui.gameObject.AddComponent<ToolbarView>();
            SetField(toolbar, "tools", tools);
            SetField(toolbar, "styleSheet", LoadStyleSheet("Toolbar"));

            var hud = CreateDocument<HudView>("HUD", panelSettings);
            SetField(hud, "styleSheet", LoadStyleSheet("Hud"));

            var poleTooltip = CreateDocument<PoleTooltipView>("PoleTooltip", panelSettings);
            SetField(poleTooltip, "pointer", pointer);
            SetField(poleTooltip, "tools", tools);
            SetField(poleTooltip, "sceneCamera", camera);
            SetField(poleTooltip, "styleSheet", LoadStyleSheet("Hud"));

            var cablePrice = CreateDocument<CablePriceView>("CablePrice", panelSettings);
            SetField(cablePrice, "tools", tools);
            SetField(cablePrice, "sceneCamera", camera);
            SetField(cablePrice, "styleSheet", LoadStyleSheet("Hud"));

            // Nad resztą UI, żeby zasłona przykrywała też pasek narzędzi.
            var gameOver = CreateDocument<GameOverView>("GameOver", panelSettings, sortingOrder: 10f);
            SetField(gameOver, "styleSheet", LoadStyleSheet("GameOver"));
            SetList(pointer, "blockingDocuments", ui, gameOver.GetComponent<UIDocument>());

            AddCallSimulation();

            var session = new GameObject("GameSession").AddComponent<GameSession>();
            SetField(session, "hud", hud);
            SetField(session, "gameOver", gameOver);

            // Narzędzia płacą przez nakładkę, która dalej przekazuje akcje do sandboxa.
            var paidActions = session.gameObject.AddComponent<PaidBuildActions>();
            paidActions.Inner = sandbox;
            SetField(paidActions, "session", session);
            SetField(paidActions, "grid", grid);
            SetField(paidActions, "views", views);
            EditorUtility.SetDirty(paidActions);
            tools.BuildActions = paidActions;
            EditorUtility.SetDirty(tools);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GameSceneBuilder] Zapisano {ScenePath}");
        }

        /// <summary>
        /// Dokłada symulację rozmów do istniejącej sceny gry, bez przebudowy reszty sceny (zachowuje ręczne zmiany).
        /// Nic nie robi, jeśli scena już ją ma.
        /// </summary>
        [MenuItem("Tools/Spaghetti Wires/Dodaj symulację rozmów do sceny")]
        public static void AddCallSimulationToScene()
        {
            EditorSceneManager.OpenScene(ScenePath);
            if (AddCallSimulation())
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log($"[GameSceneBuilder] Dodano symulację rozmów do {ScenePath}");
            }
        }

        /// <summary>Symulacja rozmów, jej widoki i wymuszanie rozmów do testów (PLAN etap 5).</summary>
        private static bool AddCallSimulation()
        {
            if (Object.FindAnyObjectByType<CallSimulationRunner>() != null)
                return false;

            var sandbox = Object.FindAnyObjectByType<SandboxBuildActions>();
            var views = Object.FindAnyObjectByType<ViewRegistry>();
            var pointer = Object.FindAnyObjectByType<ScenePointer>();
            var tools = Object.FindAnyObjectByType<ToolController>();
            var panelSettings = Object.FindAnyObjectByType<UIDocument>().panelSettings;

            var simulationObject = new GameObject("Call Simulation");
            var runner = simulationObject.AddComponent<CallSimulationRunner>();
            runner.NetworkSource = sandbox;
            EditorUtility.SetDirty(runner);

            var traffic = simulationObject.AddComponent<CallTrafficView>();
            SetField(traffic, "simulation", runner);
            SetField(traffic, "views", views);

            var debugInput = simulationObject.AddComponent<CallDebugInput>();
            SetField(debugInput, "pointer", pointer);
            SetField(debugInput, "simulation", runner);
            SetField(debugInput, "tools", tools);

            var indicators = CreateDocument<CallIndicatorsView>("CallIndicators", panelSettings);
            SetField(indicators, "simulation", runner);
            SetField(indicators, "views", views);
            SetField(indicators, "sceneCamera", Camera.main);
            SetField(indicators, "styleSheet", LoadStyleSheet("Calls"));
            return true;
        }

        /// <summary>
        /// Do uruchamiania z wiersza poleceń: przebudowuje modele węzłów, otwiera scenę gry, układa przykładową sieć
        /// w trybie edycji (bez zapisu sceny) i renderuje podgląd ogólny oraz zbliżenie na słup
        /// do plików „{-previewPath}_overview.png” i „{-previewPath}_closeup.png”.
        /// </summary>
        public static void BuildAndRenderPreview()
        {
            BuildNodePrefabs();
            EditorSceneManager.OpenScene(ScenePath);

            var sandbox = Object.FindAnyObjectByType<SandboxBuildActions>();
            var views = Object.FindAnyObjectByType<ViewRegistry>();
            var grid = Object.FindAnyObjectByType<MapGridBehaviour>();
            sandbox.SpawnStartHouses();

            // Słup na środku, magistrala do drugiego słupa i kable do domów w zasięgu, aż słup się zapełni.
            var center = grid.Grid.Size / 2;
            sandbox.PlacePole(center);
            int poleId = views.Nodes.Count;
            sandbox.PlacePole(center + new Vector2Int(-2, 1));
            sandbox.LayCable(poleId, poleId + 1, 4);
            foreach (var node in views.Nodes.ToArray())
            {
                if (node.Kind == NodeVisualKind.House && sandbox.CheckCable(poleId, node.Id, 1).IsValid)
                    sandbox.LayCable(poleId, node.Id, 1);
            }

            var overlay = Object.FindAnyObjectByType<GridOverlay>();
            overlay.Rebuild();
            overlay.Visible = true;
            Object.FindAnyObjectByType<CellHighlight>().Show(center + new Vector2Int(2, -2), ToolContext.ValidColor);

            string prefix = GetCommandLineArg("-previewPath") ?? "Temp/GamePreview";
            var camera = Camera.main;
            Render(camera, $"{prefix}_overview.png");

            views.TryGetNode(poleId, out var pole);
            var focus = pole.transform.position + Vector3.up * 5f;
            var rotation = Quaternion.Euler(25f, 30f, 0f);
            camera.transform.SetPositionAndRotation(focus - rotation * Vector3.forward * 12f, rotation);
            Render(camera, $"{prefix}_closeup.png");
        }

        #region Scene objects

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.68f, 0.8f);
            camera.fieldOfView = 30f;
            camera.farClipPlane = 500f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<IsometricCameraController>();

            // To samo ustawienie, które kontroler kamery przyjmie na starcie (pitch 35°, yaw 45°, 35 m).
            var rotation = Quaternion.Euler(35f, 45f, 0f);
            cameraObject.transform.SetPositionAndRotation(-(rotation * Vector3.forward) * 35f, rotation);
            return camera;
        }

        private static void CreateLight()
        {
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateVolume()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
                return;

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        private static void CreateGround(Materials materials)
        {
            var size = new Vector3(GridSize.x * CellSize + 20f, 0.2f, GridSize.y * CellSize + 20f);
            CreatePrimitive(PrimitiveType.Cube, "Ground", null, new Vector3(0f, -0.1f, 0f), size, materials.Ground);
        }

        #endregion

        #region Placeholder prefabs

        private static void BuildHousePrefab(Materials materials)
        {
            const float width = 3.2f, height = 4f, roof = 0.2f;
            var root = new GameObject("House");
            CreatePrimitive(PrimitiveType.Cube, "Body", root.transform, new Vector3(0f, height / 2f, 0f), new Vector3(width, height, width), materials.Wall);
            CreatePrimitive(PrimitiveType.Cube, "Roof", root.transform, new Vector3(0f, height + roof / 2f, 0f), new Vector3(width + 0.2f, roof, width + 0.2f), materials.Roof);

            // Wszystkie kable domu przychodzą do jednego uchwytu na ścianie (dom jest losowo obracany przy stawianiu).
            AddWallAnchor(root, new Vector3(0f, height - 0.4f, width / 2f), Vector3.forward, materials.Roof);

            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, (height + roof) / 2f, 0f);
            collider.size = new Vector3(width, height + roof, width);
            SaveNodePrefab(root, "House");
        }

        private static void BuildExchangePrefab(Materials materials)
        {
            const float width = 3.6f, height = 6f;
            var root = new GameObject("Exchange");
            CreatePrimitive(PrimitiveType.Cube, "Body", root.transform, new Vector3(0f, height / 2f, 0f), new Vector3(width, height, width), materials.Exchange);
            CreatePrimitive(PrimitiveType.Cylinder, "Mast", root.transform, new Vector3(0f, height + 1.5f, 0f), new Vector3(0.15f, 1.5f, 0.15f), materials.Roof);

            // Po trzy uchwyty na każdej ścianie, bez limitu kabli. Kabel bierze najbliższy.
            foreach (var side in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                var along = Vector3.Cross(Vector3.up, side);
                foreach (float offset in new[] { -1.1f, 0f, 1.1f })
                    AddWallAnchor(root, side * (width / 2f) + along * offset + Vector3.up * (height - 0.6f), side, materials.Roof);
            }

            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, height / 2f, 0f);
            collider.size = new Vector3(width, height, width);
            SaveNodePrefab(root, "Exchange");
        }

        /// <summary>Uchwyt kabla wystający ze ściany i punkt przyczepu na jego końcu.</summary>
        private static void AddWallAnchor(GameObject root, Vector3 wallPoint, Vector3 outward, Material material)
        {
            const float length = 0.16f, size = 0.08f;
            int index = root.transform.childCount;

            var bracket = CreatePrimitive(PrimitiveType.Cube, $"CableBracket_{index}", root.transform,
                wallPoint + outward * (length / 2f), Vector3.one * size, material);
            bracket.transform.localRotation = Quaternion.LookRotation(outward);
            bracket.transform.localScale = new Vector3(size, size, length);

            var anchor = new GameObject($"CableAnchor_{index}");
            anchor.transform.SetParent(root.transform, false);
            anchor.transform.localPosition = wallPoint + outward * length;
        }

        private static void SaveNodePrefab(GameObject root, string name)
        {
            root.AddComponent<NodeView>();
            EnsureFolder(PlaceholdersFolder);
            PrefabUtility.SaveAsPrefabAsset(root, $"{PlaceholdersFolder}/{name}.prefab");
            Object.DestroyImmediate(root);
        }

        private static NodeView LoadNodePrefab(string name) =>
            AssetDatabase.LoadAssetAtPath<NodeView>($"{PlaceholdersFolder}/{name}.prefab")
            ?? throw new InvalidOperationException($"Placeholder prefab '{name}' was not saved.");

        #endregion

        #region Assets

        private sealed class Materials
        {
            public readonly Material Ground = Lit("Ground", new Color(0.36f, 0.4f, 0.33f));
            public readonly Material Wall = Lit("HouseWall", new Color(0.82f, 0.74f, 0.6f));
            public readonly Material Roof = Lit("HouseRoof", new Color(0.42f, 0.3f, 0.26f));
            public readonly Material Exchange = Lit("Exchange", new Color(0.55f, 0.6f, 0.68f));

            // Kable i podglądy biorą kolor z wierzchołków (LineRenderer, linie siatki), więc shader cząsteczek.
            public readonly Material Cable = Unlit("Cable", transparent: false);
            public readonly Material Overlay = Unlit("Overlay", transparent: true);

            private static Material Lit(string name, Color color)
            {
                var material = GetOrCreate(name, "Universal Render Pipeline/Lit");
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Smoothness", 0.15f);
                return material;
            }

            private static Material Unlit(string name, bool transparent)
            {
                var material = GetOrCreate(name, "Universal Render Pipeline/Particles/Unlit");
                material.SetColor("_BaseColor", Color.white);
                if (transparent)
                {
                    material.SetFloat("_Surface", 1f);
                    material.SetFloat("_Blend", 0f);
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.renderQueue = (int)RenderQueue.Transparent;
                }
                return material;
            }

            private static Material GetOrCreate(string name, string shader)
            {
                EnsureFolder(MaterialsFolder);
                string path = $"{MaterialsFolder}/{name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(Shader.Find(shader));
                    AssetDatabase.CreateAsset(material, path);
                }
                EditorUtility.SetDirty(material);
                return material;
            }
        }

        private static StyleSheet LoadStyleSheet(string name) =>
            AssetDatabase.LoadAssetAtPath<StyleSheet>($"{UIFolder}/{name}.uss")
            ?? throw new InvalidOperationException($"Style sheet '{name}.uss' not found in {UIFolder}.");

        private static PanelSettings BuildPanelSettings()
        {
            string path = $"{UIFolder}/PanelSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(settings, path);
            }

            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>($"{UIFolder}/RuntimeTheme.tss");
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.match = 0.5f;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        #endregion

        #region Helpers

        /// <summary>Osobny UIDocument na wspólnym panelu. Wyższy <paramref name="sortingOrder"/> rysuje się na wierzchu.</summary>
        private static T CreateDocument<T>(string name, PanelSettings panelSettings, float sortingOrder = 0f) where T : Component
        {
            var document = new GameObject(name).AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.sortingOrder = sortingOrder;
            return document.gameObject.AddComponent<T>();
        }

        private static T CreateChild<T>(Transform parent, string name) where T : Component
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.AddComponent<T>();
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent,
            Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part;
        }

        // Pola komponentów są prywatne ([SerializeField]), więc ustawiamy je jak Inspector.
        private static void SetField(Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field)
                ?? throw new ArgumentException($"{target.GetType().Name} has no serialized field '{field}'.");

            switch (value)
            {
                case Object reference: property.objectReferenceValue = reference; break;
                case float number: property.floatValue = number; break;
                case Vector2Int vector: property.vector2IntValue = vector; break;
                default: throw new ArgumentException($"Unsupported value type {value?.GetType().Name}.");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetList(Object target, string field, params Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static void Render(Camera camera, string path)
        {
            var target = new RenderTexture(1280, 720, 24);
            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log($"[GameSceneBuilder] Podgląd: {path}");
        }

        private static string GetCommandLineArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        #endregion
    }
}
