#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cria o prefab "Nevoa_Labirinto": névoa da frente (sprite 10_Mist) com 3 tiles
/// rolando em loop, sorting 51 (na frente do jogador, que está em 50).
///
/// Menu: Chroma Defender → Fundos → Criar Prefab Névoa do Labirinto
/// Saída: Assets/Prefabs/LabirintoCastelo/Nevoa_Labirinto.prefab
/// </summary>
public static class NevoaLabirintoCreator
{
    private const string SpritePath = "Assets/FREE Parallax Forest Background HQ/Parallax Forest Background/10_Mist.png";
    private const string Dir = "Assets/Prefabs/LabirintoCastelo";
    private const string PrefabPath = Dir + "/Nevoa_Labirinto.prefab";
    private const int SortingOrder = 51;

    [MenuItem("Chroma Defender/Fundos/Criar Prefab Névoa do Labirinto", priority = 40)]
    public static void Create()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sprite == null)
        {
            Debug.LogError("[Névoa] Sprite não encontrado: " + SpritePath);
            return;
        }

        if (!AssetDatabase.IsValidFolder(Dir))
        {
            Directory.CreateDirectory(Path.GetFullPath(Dir));
            AssetDatabase.Refresh();
        }

        var root = new GameObject("Nevoa_Labirinto");
        var nevoa = root.AddComponent<NevoaFrente>();
        float width = sprite.bounds.size.x;

        for (int i = 0; i < 3; i++)
        {
            var tileGo = new GameObject("Tile_" + i);
            tileGo.transform.SetParent(root.transform, false);
            tileGo.transform.localPosition = new Vector3((i - 1) * width, 0f, 0f);
            var sr = tileGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = SortingOrder;
            sr.color = Color.white;
        }

        // Serializa o tamanho real do sprite no componente (caso o PPU não seja 100).
        var so = new SerializedObject(nevoa);
        so.FindProperty("tileWidth").floatValue = width;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        var saved = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Selection.activeObject = saved;
        EditorGUIUtility.PingObject(saved);
        Debug.Log("<color=lime>[Chroma Defender]</color> Prefab da névoa criado em " + PrefabPath);
    }
}
#endif
