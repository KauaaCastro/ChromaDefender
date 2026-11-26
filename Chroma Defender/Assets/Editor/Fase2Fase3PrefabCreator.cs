#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cria os prefabs dos inimigos e chefes das Fases 2 (Castelo) e 3 (Área Mágica)
/// e adiciona menus para inseri-los na cena com 1 clique.
///
///   Inimigos (Assets/Prefabs/Enemies):
///     Skeleton_Sword_Enemy   – Esqueleto Espadachim (Fogo)
///     Skeleton_Shield_Enemy  – Esqueleto Escudeiro (Gelo) – bloqueia pela frente
///     Skeleton_Spear_Enemy   – Esqueleto Lanceiro (Fogo) – alcance longo
///     Archer_Enemy           – Arqueiro (Gelo) – flechas, mantém distância
///     ShadowSpikes_Enemy     – Sombra Espinhosa (Gelo) – emboscada com espinhos
///     DarkMage_Enemy         – Mago Sombrio (Fogo) – orbes de fogo absorvíveis
///
///   Chefes (Assets/Prefabs/Bosses):
///     Boss_MestreDeArmas     – Fase 2, solta a Chave de Prata do Poço
///     Boss_Arquimago         – Fase 3, chefe final da demo (tela de vitória)
///
/// Todos os valores podem ser ajustados depois no Inspector do prefab.
/// </summary>
public static class Fase2Fase3PrefabCreator
{
    public const string EnemyDir = "Assets/Prefabs/Enemies";
    public const string BossDir = "Assets/Prefabs/Bosses";

    public const string SkeletonSword = "Skeleton_Sword_Enemy";
    public const string SkeletonShield = "Skeleton_Shield_Enemy";
    public const string SkeletonSpear = "Skeleton_Spear_Enemy";
    public const string Archer = "Archer_Enemy";
    public const string ShadowSpikes = "ShadowSpikes_Enemy";
    public const string DarkMage = "DarkMage_Enemy";
    public const string BossFase2 = "Boss_MestreDeArmas";
    public const string BossFase3 = "Boss_Arquimago";

    private const string HeroOpp = "Assets/Hero and Opponents/Sprites/";
    private const string MartialHero = "Assets/Martial Hero/Sprites/";
    private const string Wizard3 = "Assets/Evil Wizard 3/Sprites/";
    private const string Wizard1 = "Assets/EVil Wizard/Sprites/";
    private const string CainosProps = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png";
    private const string KeySilverSprite = "Assets/karsiori/Pixel Art Key Pack - V2/Key 3 - Silver.png";

    // =====================================================================
    // MENUS
    // =====================================================================

    [MenuItem("Chroma Defender/Inimigos/3. Criar ou Atualizar Prefabs do Castelo e da Área Mágica", priority = 20)]
    public static void CreateAllEnemiesMenu()
    {
        CreateAllEnemies();
        Debug.Log("<color=lime>[Chroma Defender]</color> Prefabs dos inimigos das Fases 2 e 3 criados em " + EnemyDir);
    }

    [MenuItem("Chroma Defender/Chefes/1. Criar ou Atualizar Prefabs dos Chefes (Fase 2 e 3)", priority = 10)]
    public static void CreateAllBossesMenu()
    {
        CreateAllEnemies(); // o Arquimago usa um inimigo como lacaio
        CreateAllBosses();
        Debug.Log("<color=lime>[Chroma Defender]</color> Prefabs dos chefes criados em " + BossDir);
    }

    [MenuItem("Chroma Defender/Inimigos/Castelo: Esqueleto Espadachim", priority = 40)]
    public static void InsertSword() => InsertEnemy(SkeletonSword);
    [MenuItem("Chroma Defender/Inimigos/Castelo: Esqueleto Escudeiro", priority = 41)]
    public static void InsertShield() => InsertEnemy(SkeletonShield);
    [MenuItem("Chroma Defender/Inimigos/Castelo: Esqueleto Lanceiro", priority = 42)]
    public static void InsertSpear() => InsertEnemy(SkeletonSpear);
    [MenuItem("Chroma Defender/Inimigos/Castelo: Arqueiro", priority = 43)]
    public static void InsertArcher() => InsertEnemy(Archer);
    [MenuItem("Chroma Defender/Inimigos/Área Mágica: Sombra Espinhosa", priority = 44)]
    public static void InsertShadow() => InsertEnemy(ShadowSpikes);
    [MenuItem("Chroma Defender/Inimigos/Área Mágica: Mago Sombrio", priority = 45)]
    public static void InsertMage() => InsertEnemy(DarkMage);

    [MenuItem("Chroma Defender/Chefes/Inserir Mestre de Armas (Fase 2)", priority = 30)]
    public static void InsertBossFase2() => InsertBoss(BossFase2);
    [MenuItem("Chroma Defender/Chefes/Inserir Arquimago do Caos (Fase 3)", priority = 31)]
    public static void InsertBossFase3() => InsertBoss(BossFase3);

    // =====================================================================
    // API usada pelos builders de cena
    // =====================================================================

    public static GameObject LoadEnemy(string name)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyDir}/{name}.prefab");
        if (p == null) { CreateAllEnemies(); p = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyDir}/{name}.prefab"); }
        return p;
    }

    public static GameObject LoadBoss(string name)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{BossDir}/{name}.prefab");
        if (p == null) { CreateAllEnemies(); CreateAllBosses(); p = AssetDatabase.LoadAssetAtPath<GameObject>($"{BossDir}/{name}.prefab"); }
        return p;
    }

    public static void CreateAllEnemies()
    {
        EnsureDir(EnemyDir);

        // ---------------- Esqueleto Espadachim ----------------
        Enemy(SkeletonSword, EnemyType.SkeletonSword, EnemyPolarityType.Fire, "Enemy1", "walk", "attack-A",
            bodyOffsetPx: -23f, collider: new Vector2(0.7f, 1.6f), stats: new Dictionary<string, object>
            {
                { "maxHealth", 70 }, { "contactDamage", 12 }, { "activeAttackDamage", 20 }, { "attackKnockback", 8f },
                { "detectionRadius", 6f }, { "attackRange", 1.7f }, { "attackCooldown", 1.5f },
                { "patrolSpeed", 1.2f }, { "chaseSpeed", 2.3f }, { "patrolDistance", 3f },
                { "attackWindup", 0.45f }, { "attackLungeMultiplier", 0.6f }, { "healOnDeath", 12 },
            });

        // ---------------- Esqueleto Escudeiro ----------------
        Enemy(SkeletonShield, EnemyType.SkeletonShield, EnemyPolarityType.Ice, "Enemy2", "walk", "attack-A",
            bodyOffsetPx: -16f, collider: new Vector2(0.75f, 1.6f), stats: new Dictionary<string, object>
            {
                { "maxHealth", 90 }, { "contactDamage", 12 }, { "activeAttackDamage", 18 }, { "attackKnockback", 7f },
                { "detectionRadius", 5.5f }, { "attackRange", 1.5f }, { "attackCooldown", 1.8f },
                { "patrolSpeed", 1.0f }, { "chaseSpeed", 1.8f }, { "patrolDistance", 2.5f },
                { "attackWindup", 0.5f }, { "attackLungeMultiplier", 0.5f }, { "healOnDeath", 15 },
                { "blocksFrontalHits", true }, { "guardBreakDamage", 50 },
                // Contra-ataque: aviso de 0,45s + golpe + 0,7s de recuperação (vulnerável), e demora 1s para virar.
                { "attackTelegraph", 0.45f }, { "attackRecovery", 0.7f }, { "turnAroundDelay", 1.0f },
            });

        // ---------------- Esqueleto Lanceiro ----------------
        Enemy(SkeletonSpear, EnemyType.SkeletonSpear, EnemyPolarityType.Fire, "Enemy4", "walk", "attack-A",
            bodyOffsetPx: -19f, collider: new Vector2(0.7f, 1.65f), stats: new Dictionary<string, object>
            {
                { "maxHealth", 75 }, { "contactDamage", 12 }, { "activeAttackDamage", 22 }, { "attackKnockback", 9f },
                { "detectionRadius", 6.5f }, { "attackRange", 2.3f }, { "attackCooldown", 1.9f },
                { "patrolSpeed", 1.1f }, { "chaseSpeed", 2.0f }, { "patrolDistance", 3f },
                { "attackWindup", 0.5f }, { "attackLungeMultiplier", 0.4f }, { "healOnDeath", 12 },
            });

        // ---------------- Arqueiro ----------------
        Sprite arrow = AssetDatabase.LoadAllAssetsAtPath(CainosProps).OfType<Sprite>().FirstOrDefault(s => s.name == "TX Village Props - Arrow");
        Enemy(Archer, EnemyType.Archer, EnemyPolarityType.Ice, "Enemy5", "run", "attack-A",
            bodyOffsetPx: -10f, collider: new Vector2(0.65f, 1.5f), stats: new Dictionary<string, object>
            {
                { "maxHealth", 55 }, { "contactDamage", 8 }, { "activeAttackDamage", 14 }, { "attackKnockback", 6f },
                { "detectionRadius", 8.5f }, { "attackRange", 7f }, { "attackCooldown", 2.2f },
                { "patrolSpeed", 1.2f }, { "chaseSpeed", 2.2f }, { "patrolDistance", 2.5f },
                { "attackWindup", 0.5f }, { "attackLungeMultiplier", 0f }, { "healOnDeath", 10 },
                { "rangedAttack", true }, { "projectileSpeed", 9f }, { "projectileScale", 0.9f },
                { "projectileSpawnOffset", new Vector2(0.6f, 1.05f) }, { "keepDistance", 3.5f },
                { "projectileUsesPolarity", false },
            },
            projectile: arrow != null ? new[] { arrow } : null);

        // ---------------- Sombra Espinhosa ----------------
        Enemy(ShadowSpikes, EnemyType.ShadowSpikes, EnemyPolarityType.Ice, "Enemy3", "walk", "attack-C",
            bodyOffsetPx: -3f, collider: new Vector2(0.9f, 0.5f), stats: new Dictionary<string, object>
            {
                { "maxHealth", 60 }, { "contactDamage", 10 }, { "activeAttackDamage", 24 }, { "attackKnockback", 7f },
                { "attackKnockbackY", 6f }, { "detectionRadius", 3.5f }, { "attackRange", 1.7f }, { "attackCooldown", 2.0f },
                { "patrolSpeed", 0.5f }, { "chaseSpeed", 0.9f }, { "patrolDistance", 2f },
                { "attackWindup", 0.55f }, { "attackLungeMultiplier", 0f }, { "animFps", 12f }, { "healOnDeath", 12 },
            });

        // ---------------- Mago Sombrio ----------------
        CreateDarkMage();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void CreateAllBosses()
    {
        EnsureDir(BossDir);
        CreateDuelist();
        CreateArchmage();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // =====================================================================
    // INIMIGOS
    // =====================================================================

    /// <summary>Inimigos do pacote "Hero and Opponents": quadros 128x96, pés na base do quadro.</summary>
    private static void Enemy(string name, EnemyType type, EnemyPolarityType polarity, string folder, string movePrefix, string attackPrefix,
        float bodyOffsetPx, Vector2 collider, Dictionary<string, object> stats, Sprite[] projectile = null)
    {
        const float scale = 3f;
        const float ppu = 100f;
        const float feetPx = 48f; // 96/2: os pés ficam na base do quadro
        string dir = HeroOpp + folder;

        var go = BuildEnemyRoot(name, collider, scale, feetPx / ppu * scale, -bodyOffsetPx / ppu * scale,
            EnemyBehavior.LoadFramesFromFolder(dir, "idle-"));

        var eb = go.GetComponent<EnemyBehavior>();
        var so = new SerializedObject(eb);
        Set(so, "enemyType", (int)type);
        Set(so, "polarity", (int)polarity);
        Set(so, "polarityRule", (int)EnemyBehavior.PolarityRuleMode.OppositeElement);
        Set(so, "canActivelyAttack", true);
        Set(so, "autoLoadSprites", false);
        Set(so, "animFps", 10f);
        Set(so, "spriteBodyOffsetX", bodyOffsetPx / ppu * scale);
        SetSprites(so, "idleSprites", EnemyBehavior.LoadFramesFromFolder(dir, "idle-"));
        SetSprites(so, "moveSprites", EnemyBehavior.LoadFramesFromFolder(dir, movePrefix + "-"));
        SetSprites(so, "attackSprites", EnemyBehavior.LoadFramesFromFolder(dir, attackPrefix));
        SetSprites(so, "hurtSprites", EnemyBehavior.LoadFramesFromFolder(dir, "hit-"));
        SetSprites(so, "deathFrames", EnemyBehavior.LoadFramesFromFolder(dir, "dead-"));
        if (projectile != null) SetSprites(so, "projectileSprites", projectile);
        foreach (var kv in stats) Set(so, kv.Key, kv.Value);
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(go, $"{EnemyDir}/{name}.prefab");
    }

    private static void CreateDarkMage()
    {
        const float scale = 2.6f;
        const float ppu = 100f;
        Sprite[] idle = Sheet(Wizard1 + "Idle.png");

        // Quadro 150x150: pés 27 px abaixo do centro, corpo 3 px à esquerda do centro.
        var go = BuildEnemyRoot(DarkMage, new Vector2(0.7f, 1.45f), scale, 27f / ppu * scale, 3f / ppu * scale, idle);

        var so = new SerializedObject(go.GetComponent<EnemyBehavior>());
        Set(so, "enemyType", (int)EnemyType.DarkMage);
        Set(so, "polarity", (int)EnemyPolarityType.Fire);
        Set(so, "polarityRule", (int)EnemyBehavior.PolarityRuleMode.OppositeElement);
        Set(so, "canActivelyAttack", true);
        Set(so, "autoLoadSprites", false);
        Set(so, "animFps", 12f);
        Set(so, "spriteBodyOffsetX", -3f / ppu * scale);
        SetSprites(so, "idleSprites", idle);
        SetSprites(so, "moveSprites", Sheet(Wizard1 + "Move.png"));
        SetSprites(so, "attackSprites", Sheet(Wizard1 + "Attack.png"));
        SetSprites(so, "hurtSprites", Sheet(Wizard1 + "Take Hit.png"));
        SetSprites(so, "deathFrames", Sheet(Wizard1 + "Death.png"));
        SetSprites(so, "projectileSprites", Sheet(Wizard3 + "Projectile/Moving.png"));

        Set(so, "maxHealth", 65);
        Set(so, "contactDamage", 10);
        Set(so, "activeAttackDamage", 16);
        Set(so, "attackKnockback", 6f);
        Set(so, "detectionRadius", 9f);
        Set(so, "attackRange", 7.5f);
        Set(so, "attackCooldown", 2.6f);
        Set(so, "patrolSpeed", 0.9f);
        Set(so, "chaseSpeed", 1.6f);
        Set(so, "patrolDistance", 2f);
        Set(so, "attackWindup", 0.5f);
        Set(so, "attackLungeMultiplier", 0f);
        Set(so, "healOnDeath", 14);
        Set(so, "rangedAttack", true);
        Set(so, "projectileUsesPolarity", true);
        Set(so, "projectileSpeed", 5.5f);
        Set(so, "projectileHoming", 0.8f);
        Set(so, "projectileScale", 1.6f);
        Set(so, "projectileSpawnOffset", new Vector2(0.9f, 0.9f));
        Set(so, "keepDistance", 4f);
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(go, $"{EnemyDir}/{DarkMage}.prefab");
    }

    /// <summary>Raiz com física + filho "Visual" com o sprite (assim a arte pode ser deslocada sem mexer no colisor).</summary>
    private static GameObject BuildEnemyRoot(string name, Vector2 colliderSize, float visualScale, float visualY, float visualX, Sprite[] idle)
    {
        var go = new GameObject(name);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = new Vector3(visualScale, visualScale, 1f);
        visual.transform.localPosition = new Vector3(visualX, visualY, 0f);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = idle != null && idle.Length > 0 ? idle[0] : null;
        sr.sortingOrder = 15;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 2.5f;
        rb.linearDamping = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = colliderSize;
        col.offset = new Vector2(0f, colliderSize.y * 0.5f);
        col.edgeRadius = 0.02f;

        go.AddComponent<EnemyBehavior>();
        return go;
    }

    // =====================================================================
    // CHEFES
    // =====================================================================

    private static void CreateDuelist()
    {
        var go = BuildBossRoot(BossFase2, Sheet(MartialHero + "Idle.png"));
        var boss = go.AddComponent<BossDuelist>();
        var so = new SerializedObject(boss);

        Set(so, "visual", go.transform.GetChild(0));
        Set(so, "bossName", "Mestre de Armas");
        Set(so, "defeatedFlag", GameProgress.FlagBossFase2);
        Set(so, "dropKeyId", GameProgress.KeyPoco);
        Set(so, "dropKeySprite", AssetDatabase.LoadAssetAtPath<Sprite>(KeySilverSprite));
        Set(so, "defeatMessage", "O Mestre de Armas caiu! Pegue a <color=#D8E6FF>Chave de Prata</color> e volte à floresta: o Poço Antigo espera.");
        Set(so, "maxHealth", 650);
        Set(so, "startingShield", (int)ElementPolarity.Ice);
        Set(so, "wrongElementHealMultiplier", 0.5f);
        Set(so, "contactDamage", 10);
        Set(so, "visualScale", 5.5f);
        Set(so, "spriteFacesRight", true);
        Set(so, "feetOffsetPixels", 22f);
        Set(so, "bodyOffsetPixels", -1f);
        Set(so, "bodyWidth", 1.0f);
        Set(so, "bodyHeight", 2.7f);

        SetSprites(so, "idleFrames", Sheet(MartialHero + "Idle.png"));
        SetSprites(so, "runFrames", Sheet(MartialHero + "Run.png"));
        SetSprites(so, "attack1Frames", Sheet(MartialHero + "Attack1.png"));
        SetSprites(so, "attack2Frames", Sheet(MartialHero + "Attack2.png"));
        SetSprites(so, "jumpFrames", Sheet(MartialHero + "Jump.png"));
        SetSprites(so, "fallFrames", Sheet(MartialHero + "Fall.png"));
        SetSprites(so, "hurtFrames", Sheet(MartialHero + "Take Hit.png"));
        SetSprites(so, "deathFrames", Sheet(MartialHero + "Death.png"));
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(go, $"{BossDir}/{BossFase2}.prefab");
    }

    private static void CreateArchmage()
    {
        var go = BuildBossRoot(BossFase3, Sheet(Wizard3 + "Idle.png"));
        var boss = go.AddComponent<BossArchmage>();
        var so = new SerializedObject(boss);

        Set(so, "visual", go.transform.GetChild(0));
        Set(so, "bossName", "Arquimago do Caos");
        Set(so, "defeatedFlag", GameProgress.FlagBossFase3);
        Set(so, "dropKeyId", "");
        Set(so, "triggerVictoryOnDeath", true);
        Set(so, "victoryDelay", 3f);
        Set(so, "defeatMessage", "O Arquimago do Caos foi derrotado! O cristal guardião está a salvo.");
        Set(so, "maxHealth", 700);
        Set(so, "startingShield", (int)ElementPolarity.Fire);
        Set(so, "wrongElementHealMultiplier", 0.4f);
        Set(so, "contactDamage", 8);
        Set(so, "visualScale", 5.5f);
        Set(so, "spriteFacesRight", true);
        Set(so, "feetOffsetPixels", 27f);
        Set(so, "bodyOffsetPixels", -2f);
        Set(so, "bodyWidth", 1.0f);
        Set(so, "bodyHeight", 2.8f);

        SetSprites(so, "idleFrames", Sheet(Wizard3 + "Idle.png"));
        SetSprites(so, "walkFrames", Sheet(Wizard3 + "Walk.png"));
        SetSprites(so, "attackFrames", Sheet(Wizard3 + "Attack.png"));
        SetSprites(so, "hurtFrames", Sheet(Wizard3 + "Get hit.png"));
        SetSprites(so, "deathFrames", Sheet(Wizard3 + "Death.png"));
        SetSprites(so, "projectileFrames", Sheet(Wizard3 + "Projectile/Moving.png"));
        Set(so, "minionPrefab", LoadEnemy(SkeletonSword));
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(go, $"{BossDir}/{BossFase3}.prefab");
    }

    private static GameObject BuildBossRoot(string name, Sprite[] idle)
    {
        var go = new GameObject(name);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.mass = 50f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, 2.7f);
        box.offset = new Vector2(0f, 1.35f);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = idle != null && idle.Length > 0 ? idle[0] : null;
        sr.sortingOrder = 45;
        visual.transform.localScale = new Vector3(5.5f, 5.5f, 1f);
        visual.transform.localPosition = new Vector3(0f, 1.3f, 0f);
        return go;
    }

    // =====================================================================
    // INSERIR NA CENA
    // =====================================================================

    private static void InsertEnemy(string name)
    {
        var prefab = LoadEnemy(name);
        if (prefab == null) return;
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.transform.position = ScenePivot() + Vector3.right * 4f;
        Undo.RegisterCreatedObjectUndo(inst, "Inserir " + name);
        Selection.activeGameObject = inst;
        EnsureRespawnManager();
    }

    private static void InsertBoss(string name)
    {
        var prefab = LoadBoss(name);
        if (prefab == null) return;
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Vector3 pos = ScenePivot() + Vector3.right * 8f;
        inst.transform.position = pos;
        // Arena padrão: 12 unidades para cada lado. Ajuste "Arena Min X / Max X" no Inspector.
        var boss = inst.GetComponent<BossBase>();
        if (boss != null)
        {
            var so = new SerializedObject(boss);
            Set(so, "arenaMinX", pos.x - 12f);
            Set(so, "arenaMaxX", pos.x + 12f);
            so.ApplyModifiedProperties();
        }
        Undo.RegisterCreatedObjectUndo(inst, "Inserir " + name);
        Selection.activeGameObject = inst;
        Debug.Log($"<color=lime>[Chroma Defender]</color> {name} inserido. Ajuste 'Arena Min X' e 'Arena Max X' no Inspector (linha amarela no Scene).");
    }

    private static Vector3 ScenePivot()
    {
        var view = SceneView.lastActiveSceneView;
        if (view != null) return new Vector3(view.pivot.x, view.pivot.y, 0f);
        var player = Object.FindAnyObjectByType<PlayerBehavior>();
        return player != null ? player.transform.position : Vector3.zero;
    }

    private static void EnsureRespawnManager()
    {
        if (Object.FindAnyObjectByType<EnemyRespawnManager>() != null) return;
        var mgr = new GameObject("EnemyRespawnManager");
        mgr.AddComponent<EnemyRespawnManager>();
        Undo.RegisterCreatedObjectUndo(mgr, "EnemyRespawnManager");
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private static void EnsureDir(string dir)
    {
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }
    }

    private static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    /// <summary>Sprites fatiados de uma folha (Idle_0, Idle_1 ... Idle_10) em ordem numérica.</summary>
    public static Sprite[] Sheet(string path)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();
        if (sprites.Count == 0) Debug.LogWarning($"[Chroma Defender] Nenhum sprite em {path}");
        return sprites.OrderBy(s => TrailingNumber(s.name)).ThenBy(s => s.name).ToArray();
    }

    private static int TrailingNumber(string s)
    {
        int i = s.Length;
        while (i > 0 && char.IsDigit(s[i - 1])) i--;
        return i < s.Length && int.TryParse(s.Substring(i), out int n) ? n : -1;
    }

    private static void Set(SerializedObject so, string field, object value)
    {
        var p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogWarning($"[Chroma Defender] Campo '{field}' não encontrado em {so.targetObject.GetType().Name}.");
            return;
        }
        switch (value)
        {
            case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
            case int i when p.propertyType == SerializedPropertyType.Float: p.floatValue = i; break;
            case int i: p.intValue = i; break;
            case float f: p.floatValue = f; break;
            case bool b: p.boolValue = b; break;
            case string s: p.stringValue = s; break;
            case Vector2 v: p.vector2Value = v; break;
            case Object o: p.objectReferenceValue = o; break;
            case null: p.objectReferenceValue = null; break;
            default: Debug.LogWarning($"[Chroma Defender] Tipo não suportado para '{field}'."); break;
        }
    }

    private static void SetSprites(SerializedObject so, string field, Sprite[] sprites)
    {
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[Chroma Defender] Campo '{field}' não encontrado."); return; }
        sprites = sprites ?? new Sprite[0];
        p.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
#endif
