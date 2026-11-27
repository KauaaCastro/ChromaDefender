#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cria o prefab "Caixote_Empurravel": caixote com a arte do pacote Cainos (Crate Large),
/// física dinâmica que o jogador empurra, sem tombar (rotação travada) e com massa leve.
///
/// Menu: Chroma Defender → Fundos → Criar Caixote Empurrável
/// Saída: Assets/Prefabs/LabirintoCastelo/Caixote_Empurravel.prefab
/// </summary>
public static class CaixoteEmpurravelCreator
{
    private const string SourcePrefab = "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - Crate Large.prefab";
    private const string Dir = "Assets/Prefabs/LabirintoCastelo";
    private const string PrefabPath = Dir + "/Caixote_Empurravel.prefab";

    [MenuItem("Chroma Defender/Fundos/Criar Caixote Empurrável", priority = 41)]
    public static void Create()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);
        var srcSr = source != null ? source.GetComponent<SpriteRenderer>() : null;
        if (srcSr == null || srcSr.sprite == null)
        {
            Debug.LogError("[Caixote] Não encontrei o sprite em: " + SourcePrefab);
            return;
        }

        var go = new GameObject("Caixote_Empurravel");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = srcSr.sprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 3;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.3f, 1.3f);
        box.offset = new Vector2(0f, 0f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 12f;                 // leve o bastante para empurrar
        rb.gravityScale = 1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // nunca tomba
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (!AssetDatabase.IsValidFolder(Dir))
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetFullPath(Dir));
            AssetDatabase.Refresh();
        }

        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.Refresh();
        var saved = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Selection.activeObject = saved;
        EditorGUIUtility.PingObject(saved);
        Debug.Log("<color=lime>[Chroma Defender]</color> Caixote empurrável criado em " + PrefabPath);
    }
}
#endif
