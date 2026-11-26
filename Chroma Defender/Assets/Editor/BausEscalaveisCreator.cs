#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Baús escaláveis (UpgradeChest) — um tipo de baú para cada habilidade, sempre dando o PRÓXIMO nível:
///   Bau_Madeira_ComboLeve   → Combo Leve      (2 → 3 → 4 golpes)
///   Bau_Prata_AtaquePesado  → Ataque Pesado   (bloqueado → 1 → 2 → 3)
///   Bau_Ouro_Especial       → Ataque Especial (bloqueado → liberado)
/// Arte e animação: baús do pacote Cainos. Os prefabs ficam em Assets/Prefabs/Baus.
/// Cada baú colocado na cena ganha um id automático (cena + posição), então pode repetir o prefab à vontade.
/// </summary>
public static class BausEscalaveisCreator
{
    public const string Dir = "Assets/Prefabs/Baus";
    public const string Madeira = "Bau_Madeira_ComboLeve";
    public const string Prata = "Bau_Prata_AtaquePesado";
    public const string Ouro = "Bau_Ouro_Especial";

    private const string CainosDir = "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - Chest ";

    [MenuItem("Chroma Defender/Baús/Criar ou atualizar prefabs de baú", priority = 400)]
    public static void CreateAll()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) { Directory.CreateDirectory(Dir); AssetDatabase.Refresh(); }
        Save(Build(Madeira, "Wooden", UpgradeChest.UpgradeKind.LightCombo));
        Save(Build(Prata, "Silver", UpgradeChest.UpgradeKind.HeavyAttack));
        Save(Build(Ouro, "Golden", UpgradeChest.UpgradeKind.WeaponArt));
        AssetDatabase.SaveAssets();
        Debug.Log("<color=lime>[Baús]</color> Prefabs criados em " + Dir + " (Madeira = Combo Leve, Prata = Ataque Pesado, Ouro = Especial).");
    }

    [MenuItem("Chroma Defender/Baús/Inserir Baú de Madeira (Combo Leve)", priority = 410)] public static void I1() => Insert(Madeira);
    [MenuItem("Chroma Defender/Baús/Inserir Baú de Prata (Ataque Pesado)", priority = 411)] public static void I2() => Insert(Prata);
    [MenuItem("Chroma Defender/Baús/Inserir Baú de Ouro (Especial)", priority = 412)] public static void I3() => Insert(Ouro);

    public static GameObject Load(string name)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{name}.prefab");
        if (p == null) { CreateAll(); p = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{name}.prefab"); }
        return p;
    }

    private static void Insert(string name)
    {
        var prefab = Load(name);
        if (prefab == null) return;
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(inst, "Inserir " + name);
        var view = SceneView.lastActiveSceneView;
        if (view != null) { var p = view.pivot; inst.transform.position = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), 0f); }
        Selection.activeGameObject = inst;
    }

    private static GameObject Build(string name, string cainosColor, UpgradeChest.UpgradeKind kind)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(CainosDir + cainosColor + ".prefab");
        var srcAnim = src != null ? src.GetComponent<Animator>() : null;
        var srcSr = src != null ? src.GetComponentInChildren<SpriteRenderer>() : null;
        if (src == null || srcSr == null) Debug.LogWarning("[Baús] Baú Cainos não encontrado: " + cainosColor);

        var go = new GameObject(name);
        var anim = go.AddComponent<Animator>();
        if (srcAnim != null) anim.runtimeAnimatorController = srcAnim.runtimeAnimatorController;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(0f, 0.6f);
        col.size = new Vector2(1.8f, 1.4f);

        // O nome do filho precisa ser o mesmo do Cainos ("Chest"): as animações apontam para ele.
        var child = new GameObject(srcSr != null ? srcSr.gameObject.name : "Chest");
        child.transform.SetParent(go.transform, false);
        if (srcSr != null) child.transform.localPosition = srcSr.transform.localPosition;
        var sr = child.AddComponent<SpriteRenderer>();
        if (srcSr != null) { sr.sprite = srcSr.sprite; sr.sharedMaterial = srcSr.sharedMaterial; }
        sr.sortingOrder = 5;

        var chest = go.AddComponent<UpgradeChest>();
        var so = new SerializedObject(chest);
        LabirintoCasteloPresets.Set(so, "chestId", "");
        LabirintoCasteloPresets.Set(so, "upgradeKind", (int)kind);
        LabirintoCasteloPresets.Set(so, "lightComboHits", 3);
        LabirintoCasteloPresets.Set(so, "heavyComboHits", 1);
        LabirintoCasteloPresets.Set(so, "rewardTitle", kind == UpgradeChest.UpgradeKind.LightCombo ? "Combo Leve — 3º golpe" : kind == UpgradeChest.UpgradeKind.HeavyAttack ? "Ataque Pesado" : "Ataque Especial");
        LabirintoCasteloPresets.Set(so, "rewardDescription", kind == UpgradeChest.UpgradeKind.LightCombo ? "Seu ataque rápido agora encadeia 3 golpes!" : kind == UpgradeChest.UpgradeKind.HeavyAttack ? "Botão direito do mouse: golpe pesado liberado!" : "Tecla F ou E: tempestade de cortes liberada!");
        LabirintoCasteloPresets.Set(so, "animator", anim);
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    private static void Save(GameObject go)
    {
        PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/{go.name}.prefab");
        Object.DestroyImmediate(go);
    }
}
#endif
