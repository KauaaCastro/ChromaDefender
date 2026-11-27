#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cria automaticamente os Prefabs dos 4 inimigos (Rato, Slime, Morcego e Mímico)
/// e adiciona opções de menu no Unity para inseri-los na cena com 1 clique.
/// </summary>
[InitializeOnLoad]
public static class EnemyPrefabCreator
{
    private const string PrefabDir = "Assets/Prefabs/Enemies";

    static EnemyPrefabCreator()
    {
        // Ao abrir/recompilar só cria os prefabs que AINDA NÃO existem. Antes ele recriava os 4 do zero
        // a cada recompilação e a cada Play, apagando qualquer ajuste feito nos prefabs.
        EditorApplication.delayCall += CreateMissingEnemyPrefabs;
    }

    private static void CreateMissingEnemyPrefabs()
    {
        bool Missing(string n) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{n}.prefab") == null;
        bool any = false;
        if (!Directory.Exists(PrefabDir)) { Directory.CreateDirectory(PrefabDir); AssetDatabase.Refresh(); }
        if (Missing("Rat_Enemy")) { CreateRatPrefab(); any = true; }
        if (Missing("Slime_Enemy")) { CreateSlimePrefab(); any = true; }
        if (Missing("Bat_Enemy")) { CreateBatPrefab(); any = true; }
        if (Missing("Mimic_Enemy")) { CreateMimicPrefab(); any = true; }
        if (any) { AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); }
    }

    /// <summary>
    /// O OnValidate do EnemyBehavior carrega sprites assim que o componente é adicionado — com o tipo padrão (Slime).
    /// Depois de definir o enemyType certo, limpa as listas e recarrega, senão rato/morcego/mímico ficam com cara de slime.
    /// </summary>
    private static void ReloadSprites(EnemyBehavior eb)
    {
        foreach (var f in new[] { "idleSprites", "moveSprites", "attackSprites", "hurtSprites", "deathFrames" })
            SetField(eb, f, null);
        typeof(EnemyBehavior).GetMethod("LoadAllSpritesIfEmpty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(eb, null);
        var sr = eb.GetComponent<SpriteRenderer>();
        var idle = typeof(EnemyBehavior).GetField("idleSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(eb) as Sprite[];
        if (sr != null && idle != null && idle.Length > 0) sr.sprite = idle[0];
    }

    [MenuItem("Chroma Defender/Inimigos/1. Criar ou Atualizar Prefabs dos 4 Inimigos")]
    public static void EnsureAllEnemyPrefabsExist()
    {
        if (!Directory.Exists(PrefabDir))
        {
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();
        }

        CreateRatPrefab();
        CreateSlimePrefab();
        CreateBatPrefab();
        CreateMimicPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Chroma Defender/Inimigos/2. Inserir os 4 Inimigos na Cena Atual")]
    public static void SpawnAllEnemiesInScene()
    {
        EnsureAllEnemyPrefabsExist();

        SpawnOrSelectEnemy("Rat_Enemy", new Vector3(6f, -3.8f, 0f));
        SpawnOrSelectEnemy("Slime_Enemy", new Vector3(14f, -3.8f, 0f));
        SpawnOrSelectEnemy("Bat_Enemy", new Vector3(22f, -1.5f, 0f));
        SpawnOrSelectEnemy("Mimic_Enemy", new Vector3(30f, -3.8f, 0f));

        Debug.Log("<color=green>[Chroma Defender]</color> 4 Inimigos (Rato, Slime, Morcego, Mímico) inseridos na cena! Você pode arrastá-los livremente pelo Editor.");
    }

    [MenuItem("Chroma Defender/Inimigos/Inserir apenas Rato (Patrulha)")]
    public static void SpawnRat() => SpawnOrSelectEnemy("Rat_Enemy", GetPlayerOrCameraPosition() + Vector3.right * 4f);

    [MenuItem("Chroma Defender/Inimigos/Inserir apenas Slime (Ataque Ativo)")]
    public static void SpawnSlime() => SpawnOrSelectEnemy("Slime_Enemy", GetPlayerOrCameraPosition() + Vector3.right * 4f);

    [MenuItem("Chroma Defender/Inimigos/Inserir apenas Morcego (Voador)")]
    public static void SpawnBat() => SpawnOrSelectEnemy("Bat_Enemy", GetPlayerOrCameraPosition() + Vector3.right * 4f + Vector3.up * 1.5f);

    [MenuItem("Chroma Defender/Inimigos/Inserir apenas Mímico (Baú Emboscada)")]
    public static void SpawnMimic() => SpawnOrSelectEnemy("Mimic_Enemy", GetPlayerOrCameraPosition() + Vector3.right * 4f);

    private static Vector3 GetPlayerOrCameraPosition()
    {
        PlayerBehavior player = Object.FindFirstObjectByType<PlayerBehavior>();
        if (player != null) return player.transform.position;
        Camera cam = Camera.main;
        if (cam != null) return new Vector3(cam.transform.position.x, -3.8f, 0f);
        return Vector3.zero;
    }

    private static void SpawnOrSelectEnemy(string prefabName, Vector3 spawnPos)
    {
        string path = $"{PrefabDir}/{prefabName}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            EnsureAllEnemyPrefabsExist();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = spawnPos;
            Undo.RegisterCreatedObjectUndo(instance, $"Spawn {prefabName}");
            Selection.activeGameObject = instance;
        }
    }

    private static void CreateRatPrefab()
    {
        string path = $"{PrefabDir}/Rat_Enemy.prefab";
        Sprite sprite = LoadFirstSprite("Assets/Monsters Creatures Fantasy 2/Sprites/Rat/idle.png");

        GameObject go = new GameObject("Rat_Enemy");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 15;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 2.5f;
        rb.linearDamping = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.85f, 0.32f);
        // A folha (156 px, pivô no centro) tem margem transparente: os pés visíveis ficam em y = -0,09.
        col.offset = new Vector2(0f, -0.09f + 0.16f);
        col.direction = CapsuleDirection2D.Horizontal;

        var eb = go.AddComponent<EnemyBehavior>();
        // Configura valores padrão do Rato (somente anda de um lado para o outro)
        SetField(eb, "enemyType", EnemyType.Rat);
        SetField(eb, "polarity", EnemyPolarityType.Neutral);
        SetField(eb, "canActivelyAttack", false);
        SetField(eb, "maxHealth", 35);
        SetField(eb, "contactDamage", 12);
        SetField(eb, "patrolSpeed", 2.0f);
        SetField(eb, "patrolDistance", 3.5f);
        ReloadSprites(eb);
        eb.ApplyElementalTint();

        SaveAsPrefab(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateSlimePrefab()
    {
        string path = $"{PrefabDir}/Slime_Enemy.prefab";
        Sprite sprite = LoadFirstSprite("Assets/Monsters Creatures Fantasy 2/Sprites/Slime/idle.png");

        GameObject go = new GameObject("Slime_Enemy");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 15;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 2.5f;
        rb.linearDamping = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.28f;
        // Pés visíveis do sprite em y = -0,09 (margem transparente da folha de 156 px).
        col.offset = new Vector2(0f, -0.09f + 0.28f);
        // Slime grande (2,4x): a folha tem muita margem transparente, o corpo visível fica ~ altura do herói.
        go.transform.localScale = new Vector3(2.4f, 2.4f, 1f);

        var eb = go.AddComponent<EnemyBehavior>();
        SetField(eb, "enemyType", EnemyType.Slime);
        SetField(eb, "polarity", EnemyPolarityType.Ice);
        SetField(eb, "canActivelyAttack", true);
        SetField(eb, "maxHealth", 80);
        SetField(eb, "contactDamage", 15);
        SetField(eb, "activeAttackDamage", 20);
        SetField(eb, "attackKnockback", 5f);
        SetField(eb, "detectionRadius", 5.0f);
        SetField(eb, "attackRange", 1.4f);
        SetField(eb, "patrolSpeed", 1.4f);
        SetField(eb, "chaseSpeed", 2.2f);
        SetField(eb, "patrolDistance", 3.0f);
        ReloadSprites(eb);
        eb.ApplyElementalTint();

        SaveAsPrefab(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateBatPrefab()
    {
        string path = $"{PrefabDir}/Bat_Enemy.prefab";
        Sprite sprite = LoadFirstSprite("Assets/Monsters Creatures Fantasy 2/Sprites/Bat/fly.png");

        GameObject go = new GameObject("Bat_Enemy");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 15;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.35f;
        col.offset = Vector2.zero;

        var eb = go.AddComponent<EnemyBehavior>();
        SetField(eb, "enemyType", EnemyType.Bat);
        SetField(eb, "polarity", EnemyPolarityType.Fire);
        SetField(eb, "canActivelyAttack", true);
        SetField(eb, "isFlying", true);
        SetField(eb, "maxHealth", 40);
        SetField(eb, "contactDamage", 14);
        SetField(eb, "activeAttackDamage", 22);
        SetField(eb, "attackKnockback", 4.5f);
        SetField(eb, "detectionRadius", 6.5f);
        SetField(eb, "attackRange", 1.8f);
        SetField(eb, "patrolSpeed", 2.2f);
        SetField(eb, "chaseSpeed", 3.2f);
        SetField(eb, "patrolDistance", 4.0f);
        ReloadSprites(eb);
        eb.ApplyElementalTint();

        SaveAsPrefab(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateMimicPrefab()
    {
        string path = $"{PrefabDir}/Mimic_Enemy.prefab";
        Sprite sprite = LoadFirstSprite("Assets/Monsters Creatures Fantasy 2/Sprites/Mimic/Idle_closed.png");

        GameObject go = new GameObject("Mimic_Enemy");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 15;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 2.5f;
        rb.linearDamping = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.85f, 0.55f);
        // Pés visíveis do sprite em y = -0,10 (folha de 146 px com margem transparente).
        col.offset = new Vector2(0f, -0.10f + 0.275f);

        var eb = go.AddComponent<EnemyBehavior>();
        SetField(eb, "enemyType", EnemyType.Mimic);
        SetField(eb, "polarity", EnemyPolarityType.Neutral);
        SetField(eb, "canActivelyAttack", true);
        SetField(eb, "maxHealth", 85);
        SetField(eb, "contactDamage", 18);
        SetField(eb, "activeAttackDamage", 32);
        SetField(eb, "attackKnockback", 6.5f);
        SetField(eb, "detectionRadius", 4.0f);
        SetField(eb, "attackRange", 1.5f);
        SetField(eb, "patrolSpeed", 1.2f);
        SetField(eb, "chaseSpeed", 2.6f);
        SetField(eb, "patrolDistance", 2.0f);
        ReloadSprites(eb);
        eb.ApplyElementalTint();

        SaveAsPrefab(go, path);
        Object.DestroyImmediate(go);
    }

    private static Sprite LoadFirstSprite(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in assets)
        {
            if (a is Sprite s) return s;
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }

    private static void SaveAsPrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
    }
}
#endif
