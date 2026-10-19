using System;
using System.IO;
using SpaghettiWires.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaghettiWires.EditorTools
{
    /// <summary>
    /// Buduje prefab najprostszego drewnianego słupa telefonicznego z prymitywów Unity.
    /// 1 jednostka = 1 metr. Punkt (0, 0, 0) prefabu to podstawa słupa na ziemi.
    /// Na szczycie każdego izolatora jest pusty obiekt „CableAnchor_N” – punkt zaczepienia kabla.
    /// To prefab słupa używany w scenie gry (przez <see cref="NodeView"/>).
    /// </summary>
    public static class WoodenPoleBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Poles/WoodenPole.prefab";
        private const string MaterialsFolder = "Assets/_Project/Materials";

        // Wymiary w metrach.
        private const float PoleHeight = 7f;
        private const float PoleDiameter = 0.24f;
        private const float CrossarmHeight = 6.6f; // wysokość środka poprzeczki
        private const float CrossarmLength = 1.4f;
        private const float CrossarmThickness = 0.1f;
        private const float BraceThickness = 0.04f;
        private const float BraceReach = 0.4f; // jak daleko od słupa zastrzał podpiera poprzeczkę
        private const int InsulatorCount = 4;
        private const float InsulatorMargin = 0.12f; // odstęp skrajnego izolatora od końca poprzeczki
        private const float InsulatorDiameter = 0.07f;
        private const float InsulatorHeight = 0.12f;

        private static readonly Color WoodColor = new(0.40f, 0.27f, 0.16f);
        private static readonly Color InsulatorColor = new(0.85f, 0.87f, 0.82f);

        [MenuItem("Tools/Spaghetti Wires/Modele/Zbuduj drewniany słup")]
        public static void Build()
        {
            var wood = GetOrCreateMaterial("Wood", WoodColor, smoothness: 0.1f);
            var ceramic = GetOrCreateMaterial("Insulator", InsulatorColor, smoothness: 0.7f);

            var root = new GameObject("WoodenPole");

            // Słup: cylinder Unity ma wysokość 2, więc skala Y to połowa wysokości.
            CreatePart(PrimitiveType.Cylinder, "Pole", root.transform,
                new Vector3(0f, PoleHeight / 2f, 0f),
                new Vector3(PoleDiameter, PoleHeight / 2f, PoleDiameter), wood);

            CreatePart(PrimitiveType.Cube, "Crossarm", root.transform,
                new Vector3(0f, CrossarmHeight, 0f),
                new Vector3(CrossarmLength, CrossarmThickness, CrossarmThickness), wood);

            // Dwa ukośne zastrzały między słupem a poprzeczką.
            float armBottom = CrossarmHeight - CrossarmThickness / 2f;
            foreach (float side in new[] { -1f, 1f })
            {
                CreateBeam("Brace", root.transform,
                    from: new Vector3(0f, armBottom - BraceReach, 0f),
                    to: new Vector3(side * BraceReach, armBottom, 0f),
                    BraceThickness, wood);
            }

            float armTop = CrossarmHeight + CrossarmThickness / 2f;
            float spacing = (CrossarmLength - 2f * InsulatorMargin) / (InsulatorCount - 1);
            for (int i = 0; i < InsulatorCount; i++)
            {
                float x = -CrossarmLength / 2f + InsulatorMargin + i * spacing;
                CreatePart(PrimitiveType.Cylinder, $"Insulator_{i}", root.transform,
                    new Vector3(x, armTop + InsulatorHeight / 2f, 0f),
                    new Vector3(InsulatorDiameter, InsulatorHeight / 2f, InsulatorDiameter), ceramic);

                var anchor = new GameObject($"CableAnchor_{i}");
                anchor.transform.SetParent(root.transform, false);
                anchor.transform.localPosition = new Vector3(x, armTop + InsulatorHeight, 0f);
            }

            // Jeden prosty collider na cały słup zamiast colliderów każdego prymitywu.
            var collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, PoleHeight / 2f, 0f);
            collider.height = PoleHeight;
            collider.radius = 0.4f;

            // Słup jest widokiem węzła sieci: kable zaczepia na punktach CableAnchor_N.
            root.AddComponent<NodeView>();

            EnsureFolder(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[WoodenPoleBuilder] Zapisano {PrefabPath}");
        }

        /// <summary>
        /// Do uruchamiania z wiersza poleceń: buduje słup i renderuje podgląd do PNG.
        /// Argument „-previewPath ścieżka” wskazuje prefiks plików wyjściowych.
        /// </summary>
        public static void BuildAndRenderPreview()
        {
            Build();
            string prefix = GetCommandLineArg("-previewPath") ?? "Temp/WoodenPolePreview";
            RenderPreviews(prefix);
        }

        private static GameObject CreatePart(PrimitiveType type, string name, Transform parent,
            Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part;
        }

        // Prostopadłościan rozpięty między dwoma punktami.
        private static void CreateBeam(string name, Transform parent, Vector3 from, Vector3 to,
            float thickness, Material material)
        {
            var direction = to - from;
            var beam = CreatePart(PrimitiveType.Cube, name, parent, (from + to) / 2f,
                new Vector3(thickness, direction.magnitude, thickness), material);
            beam.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
        }

        private static Material GetOrCreateMaterial(string name, Color color, float smoothness)
        {
            EnsureFolder(MaterialsFolder);
            string path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder))
                return;

            EnsureFolder(Path.GetDirectoryName(folder));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));
        }

        private static void RenderPreviews(string prefix)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            PrefabUtility.InstantiatePrefab(prefab);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.GetComponent<MeshRenderer>().sharedMaterial =
                new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.45f, 0.5f, 0.4f) };

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.52f, 0.58f);

            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.75f, 0.88f);
            camera.fieldOfView = 30f;

            // Cały słup oraz zbliżenie na poprzeczkę.
            Render(camera, $"{prefix}_full.png", focus: new Vector3(0f, 3.6f, 0f), distance: 17f, yaw: 30f, pitch: 12f);
            Render(camera, $"{prefix}_top.png", focus: new Vector3(0f, CrossarmHeight, 0f), distance: 4f, yaw: 30f, pitch: 18f);
        }

        private static void Render(Camera camera, string path, Vector3 focus, float distance, float yaw, float pitch)
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            camera.transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);

            var target = new RenderTexture(768, 1024, 24);
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
            Debug.Log($"[WoodenPoleBuilder] Podgląd: {path}");
        }

        private static string GetCommandLineArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
