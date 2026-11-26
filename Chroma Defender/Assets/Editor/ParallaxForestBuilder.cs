#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Prepara o pacote "FREE Parallax Forest Background HQ" (10 PNGs estáticos) como fundo animado:
///   - Cria o prefab Assets/Prefabs/Backgrounds/ParallaxForest.prefab com o ParallaxBackground configurado.
///   - Adiciona esse prefab na cena aberta (substitui o anterior, se existir).
/// Os valores de cada camada ficam na tabela abaixo e podem ser ajustados depois no Inspector.
/// </summary>
public static class ParallaxForestBuilder
{
    private const string ForestDir = "Assets/FREE Parallax Forest Background HQ/Parallax Forest Background/";
    private const string PrefabDir = "Assets/Prefabs/Backgrounds";
    private const string PrefabPath = PrefabDir + "/ParallaxForest.prefab";
    private const string RootName = "--- FUNDO: PARALLAX FLORESTA ---";

    // arquivo, parallax, autoScroll, bobAmp, bobSpeed, alphaPulse, alphaSpeed
    private static readonly (string file, float parallax, float scroll, float bob, float bobSpeed, float pulse, float pulseSpeed)[] Table =
    {
        ("01_Sky",       0.00f,  0.00f, 0f,    0f,    0f,    0f),
        ("02_Forest",    0.04f,  0.00f, 0f,    0f,    0f,    0f),
        ("03_Forest",    0.08f,  0.00f, 0f,    0f,    0f,    0f),
        ("04_Forest",    0.14f,  0.00f, 0f,    0f,    0f,    0f),
        ("05_Forest",    0.22f,  0.00f, 0f,    0f,    0f,    0f),
        ("06_Particles", 0.30f,  0.25f, 0.12f, 0.12f, 0.30f, 0.20f),
        ("07_Forest",    0.40f,  0.00f, 0f,    0f,    0f,    0f),
        ("08_Particles", 0.50f, -0.35f, 0.18f, 0.09f, 0.35f, 0.15f),
        ("09_Bushes",    0.62f,  0.00f, 0f,    0f,    0f,    0f),
        ("10_Mist",      0.55f,  0.45f, 0.06f, 0.05f, 0.40f, 0.10f),
    };

    [MenuItem("Chroma Defender/Fundo/1. Criar prefab Parallax Floresta", priority = 300)]
    public static GameObject CreatePrefab()
    {
        if (!AssetDatabase.IsValidFolder(PrefabDir))
        {
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();
        }

        var go = new GameObject(RootName);
        var bg = go.AddComponent<ParallaxBackground>();
        bg.Layers.Clear();
        int missing = 0;
        foreach (var row in Table)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ForestDir + row.file + ".png");
            if (sprite == null) { missing++; Debug.LogWarning("[Chroma Defender] Sprite não encontrado: " + row.file); continue; }
            bg.Layers.Add(new ParallaxBackground.Layer
            {
                name = row.file,
                sprite = sprite,
                parallax = row.parallax,
                autoScrollSpeed = row.scroll,
                bobAmplitude = row.bob,
                bobSpeed = row.bobSpeed,
                alphaPulse = row.pulse,
                alphaPulseSpeed = row.pulseSpeed,
            });
        }
        bg.Build(); // pré-visualização na Scene view (no Play ele reconstrói)

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        Debug.Log($"[Chroma Defender] Prefab criado em {PrefabPath} ({Table.Length - missing} camadas).");
        Selection.activeObject = prefab;
        return prefab;
    }

    [MenuItem("Chroma Defender/Fundo/2. Adicionar Parallax Floresta na cena aberta", priority = 301)]
    public static void AddToOpenScene()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) prefab = CreatePrefab();

        RemoveFromOpenScene();
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Adicionar Parallax Floresta");
        if (Camera.main != null)
        {
            var p = Camera.main.transform.position;
            instance.transform.position = new Vector3(p.x, p.y, 0f);
        }

        EditorSceneManager.MarkSceneDirty(instance.scene);
        Selection.activeGameObject = instance;
        Debug.Log("[Chroma Defender] Parallax Floresta adicionado em " + instance.scene.name + ". Salve a cena (Ctrl+S).");
    }

    [MenuItem("Chroma Defender/Fundo/3. Remover Parallax Floresta da cena aberta", priority = 302)]
    public static void RemoveFromOpenScene()
    {
        foreach (var bg in Object.FindObjectsByType<ParallaxBackground>(FindObjectsInactive.Include))
        {
            if (bg.gameObject.name != RootName) continue;
            var scene = bg.gameObject.scene;
            Undo.DestroyObjectImmediate(bg.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
