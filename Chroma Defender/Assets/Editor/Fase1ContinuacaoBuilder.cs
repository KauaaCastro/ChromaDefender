#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Constrói a continuação da Fase 1 (SampleScene) a partir do fim do chão atual (x ≈ 110):
///
///   Trilha das Ruínas → Pântano de plataformas → Vilarejo abandonado → BIFURCAÇÃO
///      ├─ ↗ Colina: arena do Ceifador Sombrio (boss) → Castelo (Fase 2, exige Chave Dourada do boss)
///      └─ ↓ Fosso: Caminho subterrâneo → Poço Antigo (Fase 3, exige Chave de Prata da Fase 2)
///
/// Tudo fica dentro de "--- FASE 1: CONTINUACAO ---" e do Tilemap "FlorestGround_Continuacao",
/// então dá para reconstruir quantas vezes quiser (o anterior é apagado antes).
/// As coordenadas estão nas constantes/listas abaixo para facilitar ajustes.
/// </summary>
public static class Fase1ContinuacaoBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string RootName = "--- FASE 1: CONTINUACAO ---";
    private const string TilemapName = "FlorestGround_Continuacao";

    private const string CainosPrefabDir = "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/";
    private const string CainosTileDir = "Assets/Cainos/Pixel Art Platformer - Village Props/Tileset Palette/TP Ground/";
    private const string MedievalTiles = "Assets/Medieval_pixel_art_asset_FREE/Textures/Medieval_tiles_free2.png";
    private const string MedievalProps = "Assets/Medieval_pixel_art_asset_FREE/Textures/Medieval_props_free.png";
    private const string BossController = "Assets/Bringer Of Death/Animation/Bringer Of Death.controller";
    private const string BossSheet = "Assets/Bringer Of Death/Sprite Sheet/Bringer-of-Death-SpritSheet.png";
    private const string KeyGoldSprite = "Assets/karsiori/Pixel Art Key Pack - V2/Key 1 - Gold.png";
    private const string KeySilverSprite = "Assets/karsiori/Pixel Art Key Pack - V2/Key 3 - Silver.png";
    private const string PadlockCastleDir = "Assets/karsiori/Pixel Art Padlock Pack - Animated/Sprites/Old Padlock/GOLD/Sprites";
    private const string PadlockWellDir = "Assets/karsiori/Pixel Art Padlock Pack - Animated/Sprites/Hefty Padlock/BLACK/Sprites";

    // --- Alturas de superfície (onde os pés pisam) ---
    private const int Bottom = -10;         // linha mais funda do terreno
    private const float SurfTrail = -1f;     // trilha/vilarejo/bifurcação (mesmo nível do mapa atual)
    private const float SurfTunnel = -6f;    // caminho do poço (subterrâneo)
    private const float SurfPlateau = 4f;    // topo da colina (boss + castelo)

    // --- Pontos-chave (X) ---
    private const float BossX = 206f;
    private const float ArenaMinX = 189f;
    private const float ArenaMaxX = 213f;
    private const float CastleGateX = 222.5f;
    private const float WellX = 222f;
    private const float WorldEndX = 233f;

    private static int GroundLayer => LayerMask.NameToLayer("Ground") >= 0 ? LayerMask.NameToLayer("Ground") : 6;

    // =====================================================================
    // MENUS
    // =====================================================================

    [MenuItem("Chroma Defender/Fase 1/1. Construir continuação (Castelo + Poço)", priority = 100)]
    public static void BuildMenu()
    {
        if (!OpenScene(ScenePath)) return;

        ClearExisting();
        RemoveOldVictoryTriggers();

        Grid grid = FindOrCreateGrid();
        Tilemap tm = CreateTilemap(grid.transform);
        PaintTerrain(tm);

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Fase 1 Continuação");

        Transform decor = Group(root, "01_Decoracao");
        Transform gameplay = Group(root, "02_Checkpoints_e_Dicas");
        Transform enemies = Group(root, "03_RespawnDeMobs");
        Transform boss = Group(root, "04_Boss_Arena");
        Transform castle = Group(root, "05_Castelo_Fase2");
        Transform well = Group(root, "06_Poco_Fase3");

        BuildDecor(decor);
        BuildGameplay(gameplay);
        BuildEnemies(enemies);
        BuildBoss(boss);
        BuildCastle(castle);
        BuildWell(well);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log("<color=lime>[Chroma Defender]</color> Continuação da Fase 1 construída (x 112 → 233). Bifurcação em x≈161.");
    }

    [MenuItem("Chroma Defender/Fase 1/2. Remover continuação", priority = 101)]
    public static void ClearMenu()
    {
        ClearExisting();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    [MenuItem("Chroma Defender/Fase 1/3. Criar cenas da Fase 2 e 3 (provisórias)", priority = 102)]
    public static void CreatePlaceholderScenesMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        CreatePlaceholderScene(GameProgress.SceneFase2, BuildFase2Content);
        CreatePlaceholderScene(GameProgress.SceneFase3, BuildFase3Content);

        EditorSceneManager.OpenScene(ScenePath);
    }

    // =====================================================================
    // TERRENO
    // =====================================================================

    private static void PaintTerrain(Tilemap tm)
    {
        var cells = new HashSet<Vector2Int>();
        void Fill(int x0, int x1, int top, int bottom)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = bottom; y <= top; y++)
                    cells.Add(new Vector2Int(x, y));
        }

        // 1) Trilha das Ruínas (começa após um pequeno vão no fim do chão atual, x≈109.7)
        Fill(112, 126, -2, Bottom);

        // 2) Pântano: ilhas flutuantes sobre o abismo
        Fill(129, 131, -1, -2);   // superfície 0
        Fill(134, 136, 0, -1);    // superfície 1
        Fill(139, 140, -1, -2);   // superfície 0

        // 3) Vilarejo abandonado + praça da bifurcação
        Fill(142, 163, -2, Bottom);

        // 4) Fosso (x 164..167 aberto) e piso do caminho subterrâneo do poço
        Fill(164, 232, -7, Bottom);

        // 5) Colina em degraus até o platô do boss/castelo (base em y=-1 = teto do túnel)
        Fill(168, 170, -1, -1);   // superfície 0
        Fill(171, 173, 0, -1);    // superfície 1
        Fill(174, 176, 1, -1);    // superfície 2
        Fill(177, 179, 2, -1);    // superfície 3
        Fill(180, 232, 3, -1);    // platô, superfície 4

        // 6) Parede que fecha o fim do túnel
        Fill(229, 232, -6, -2);

        TileBase TL = LoadTile(13), T1 = LoadTile(14), T2 = LoadTile(15), TR = LoadTile(16);
        TileBase L = LoadTile(23), M1 = LoadTile(24), M2 = LoadTile(25), R = LoadTile(26);

        foreach (var c in cells)
        {
            bool up = cells.Contains(c + Vector2Int.up);
            bool left = cells.Contains(c + Vector2Int.left);
            bool right = cells.Contains(c + Vector2Int.right);
            bool even = (c.x & 1) == 0;

            TileBase t;
            if (!up)
            {
                if (!left && right) t = TL;
                else if (left && !right) t = TR;
                else t = even ? T1 : T2;
            }
            else
            {
                if (!left && right) t = L;
                else if (left && !right) t = R;
                else t = even ? M1 : M2;
            }
            tm.SetTile(new Vector3Int(c.x, c.y, 0), t);
        }
        tm.CompressBounds();
    }

    // =====================================================================
    // DECORAÇÃO (assets Cainos usados como enfeite, sem física)
    // =====================================================================

    private static void BuildDecor(Transform p)
    {
        float s = SurfTrail;

        // --- Trilha das Ruínas ---
        Deco(p, "Road Sign 02", 112.8f, s);
        Deco(p, "Road Lamp", 114.5f, s);
        Deco(p, "Tree 01", 117.5f, s, sortingOrder: -5);
        Deco(p, "Crate Large", 119.2f, s);
        Deco(p, "Crate Small", 120.4f, s);
        Deco(p, "Barrel", 121.3f, s);
        Deco(p, "Bush 01", 123.0f, s);
        Deco(p, "Tree 02", 124.6f, s, sortingOrder: -5);
        Deco(p, "Grass 02", 113.6f, s);
        Deco(p, "Grass 05", 118.3f, s);
        Deco(p, "Flower 01", 122.2f, s);
        Deco(p, "Grass 03", 125.8f, s);

        // --- Pântano (ilhas) ---
        Deco(p, "Grass 01", 130.0f, 0f);
        Deco(p, "Sunflower 02", 135.2f, 1f);
        Deco(p, "Grass 04", 139.5f, 0f);

        // --- Vilarejo abandonado ---
        Deco(p, "Fence A", 142.9f, s);
        Deco(p, "Stall", 146.5f, s, sortingOrder: -3);
        Deco(p, "Wheelbarrow", 149.3f, s);
        Deco(p, "Hay Bale", 151.2f, s);
        Deco(p, "Hay Pile", 152.4f, s);
        Deco(p, "Scarecrow 01", 153.8f, s, sortingOrder: -3);
        Deco(p, "Log Bench", 155.2f, s);
        Deco(p, "Campfire 01", 156.6f, s);
        Deco(p, "Tree 01", 158.2f, s, sortingOrder: -5);
        Deco(p, "Pumpkin A", 157.6f, s);
        Deco(p, "Basket", 159.4f, s);
        Deco(p, "Grass 06", 145.0f, s);
        Deco(p, "Grass 07", 150.3f, s);
        Deco(p, "Flower 02", 154.5f, s);

        // --- Praça da bifurcação ---
        Deco(p, "Road Sign 01", 161.3f, s);
        Deco(p, "Fire Bowl", 163.2f, s);

        // --- Degraus da colina ---
        Deco(p, "Grass 01", 169.0f, 0f);
        Deco(p, "Grass 08", 172.2f, 1f);
        Deco(p, "Bush 02", 175.0f, 2f);
        Deco(p, "Grass 09", 178.3f, 3f);

        // --- Platô: caminho até a arena ---
        float top = SurfPlateau;
        Deco(p, "Road Sign 02", 184.6f, top);
        Deco(p, "Tree 02", 186.0f, top, sortingOrder: -5);
        Deco(p, "Weapon Rack", 187.6f, top);
        Deco(p, "Fire Bowl", ArenaMinX, top);
        Deco(p, "Gravestone 01", 193.0f, top, sortingOrder: -3);
        Deco(p, "Gravestone 02", 197.5f, top, sortingOrder: -3);
        Deco(p, "Gravestone 04", 201.8f, top, sortingOrder: -3);
        Deco(p, "Spear", 199.4f, top, sortingOrder: -2);
        Deco(p, "Fire Bowl", ArenaMaxX, top);
        Deco(p, "Statue", 214.6f, top, sortingOrder: -3);
        Deco(p, "Banner", 230.0f, top, sortingOrder: -3);

        // --- Caminho subterrâneo do poço ---
        float t = SurfTunnel;
        Deco(p, "Torch", 170.0f, t);
        Deco(p, "Rock 01", 175.4f, t);
        Deco(p, "Torch", 182.0f, t);
        Deco(p, "Pot 01", 185.8f, t);
        Deco(p, "Rock 02", 188.6f, t);
        Deco(p, "Torch", 194.0f, t);
        Deco(p, "Gravestone 03", 198.0f, t, sortingOrder: -3);
        Deco(p, "Wine Bottle", 199.6f, t);
        Deco(p, "Torch", 206.0f, t);
        Deco(p, "Cauldron", 210.5f, t);
        Deco(p, "Rock 03", 214.0f, t);
        Deco(p, "Torch", 217.5f, t);
        Deco(p, "Torch", 226.5f, t);
    }

    // =====================================================================
    // CHECKPOINTS, DICAS, PLATAFORMA DE IMPULSO, PAREDES
    // =====================================================================

    private static void BuildGameplay(Transform p)
    {
        // Checkpoints são raros: só este, no começo da continuação.
        CreateCheckpoint(p, "Checkpoint_Trilha", 116.0f, SurfTrail);

        CreateHint(p, "Dica_Trilha", 112.5f, SurfTrail, 2f,
            "Trilha das Ruínas — cuidado com os buracos!\nSe morrer, os monstros voltam e o Guardião se recupera.");
        CreateHint(p, "Dica_Bifurcacao", 160.5f, SurfTrail, 3f,
            "BIFURCAÇÃO\n↗ Suba a colina: Guardião e Castelo (Fase 2)     ↓ Pule no fosso: Poço Antigo (Fase 3)", 6f);
        CreateHint(p, "Dica_Tunel", 169.5f, SurfTunnel, 2f,
            "Caminho do Poço Antigo.\nPara voltar à bifurcação, fique parado na plataforma mágica do fosso.", 5f);

        // Plataforma de impulso no fundo do fosso (volta para a praça)
        var padRoot = new GameObject("PlataformaImpulso_Fosso");
        padRoot.transform.SetParent(p);
        padRoot.transform.position = new Vector3(165.9f, SurfTunnel, 0f);
        var padCol = padRoot.AddComponent<BoxCollider2D>();
        padCol.isTrigger = true;
        padCol.size = new Vector2(1.8f, 0.9f);
        padCol.offset = new Vector2(0f, 0.45f);
        var padVisual = Deco(padRoot.transform, "Jump Platform 01", 165.9f, SurfTunnel);
        if (padVisual != null)
        {
            foreach (var mb in padVisual.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
        }
        padRoot.AddComponent<BouncePad>().Configure(padVisual != null ? padVisual.transform : null);

        // Paredes invisíveis no fim do mundo
        CreateWall(p, "ParedeInvisivel_Fim", WorldEndX + 0.5f, 0f, 1f, 40f);
    }

    // =====================================================================
    // INIMIGOS
    // =====================================================================

    /// <summary>
    /// Os mobs da fase são posicionados manualmente na cena.
    /// Aqui só garantimos o gerenciador que faz TODOS eles voltarem quando o cavaleiro morre.
    /// </summary>
    private static void BuildEnemies(Transform p)
    {
        if (Object.FindFirstObjectByType<EnemyRespawnManager>() == null)
        {
            var mgr = new GameObject("EnemyRespawnManager");
            mgr.transform.SetParent(p);
            mgr.AddComponent<EnemyRespawnManager>();
        }
    }

    // =====================================================================
    // BOSS
    // =====================================================================

    private static void BuildBoss(Transform p)
    {
        var go = new GameObject("Boss_CeifadorSombrio");
        go.transform.SetParent(p);
        go.transform.position = new Vector3(BossX, SurfPlateau + 0.05f, 0f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.mass = 50f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.5f, 3.3f);
        box.offset = new Vector2(0f, 1.65f);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAllAssetsAtPath(BossSheet).OfType<Sprite>().OrderBy(x => x.name.Length).ThenBy(x => x.name).FirstOrDefault();
        sr.sortingOrder = 45;
        var anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BossController);

        var boss = go.AddComponent<BossBringer>();
        boss.Configure(visual.transform, AssetDatabase.LoadAssetAtPath<Sprite>(KeyGoldSprite), ArenaMinX, ArenaMaxX);
        visual.transform.localScale = new Vector3(4.2f, 4.2f, 1f);
        visual.transform.localPosition = new Vector3(0f, 1.9f, 0f);
        EditorUtility.SetDirty(boss);
    }

    // =====================================================================
    // CASTELO (entrada da Fase 2)
    // =====================================================================

    private static void BuildCastle(Transform p)
    {
        var tiles = LoadMedievalTiles();
        float baseY = SurfPlateau;

        // Torres: recorte de 3x7 blocos (colunas 13-15, linhas 0-6 da textura)
        BuildTower(p, tiles, "Torre_Esquerda", 216f, baseY, mirror: false);
        BuildTower(p, tiles, "Torre_Direita", 226f, baseY, mirror: true);

        // Muralha entre as torres: bloco 3x3 (colunas 0-2, linhas 8-10) esticado para 7x5
        var wall = Group(p.gameObject, "Muralha");
        const int wallW = 7, wallH = 5;
        for (int i = 0; i < wallW; i++)
        {
            int srcCol = i == 0 ? 0 : (i == wallW - 1 ? 2 : 1);
            for (int j = 0; j < wallH; j++)
            {
                int srcRow = j == 0 ? 8 : (j == wallH - 1 ? 10 : 9);
                PlaceTile(wall, tiles, srcCol, srcRow, 219f + i + 0.5f, baseY + (wallH - 1 - j) + 0.5f, 4, false);
            }
        }

        // Portão (fechado = grade levadiça; aberto = arco escuro)
        Sprite[] props = LoadSpritesSorted(MedievalProps);
        Sprite closed = props.FirstOrDefault(x => x.name == "Medieval_props_free_3");
        Sprite open = props.FirstOrDefault(x => x.name == "Medieval_props_free_4");
        Sprite torch = props.FirstOrDefault(x => x.name == "Medieval_props_free_21");
        Sprite shield = props.FirstOrDefault(x => x.name == "Medieval_props_free_20");

        var gateClosed = SpriteObj(p, "Portao_Fechado", closed, new Vector3(CastleGateX, baseY + 1.5f, 0f), 2.6f, 6);
        var gateOpen = SpriteObj(p, "Portao_Aberto", open, new Vector3(CastleGateX, baseY + 1.5f, 0f), 2.6f, 6);
        gateOpen.SetActive(false);

        SpriteObj(p, "Tocha_E", torch, new Vector3(CastleGateX - 2.2f, baseY + 2.4f, 0f), 1.4f, 7);
        SpriteObj(p, "Tocha_D", torch, new Vector3(CastleGateX + 2.2f, baseY + 2.4f, 0f), 1.4f, 7);
        SpriteObj(p, "Escudo_E", shield, new Vector3(217.5f, baseY + 4.2f, 0f), 1.2f, 7);
        SpriteObj(p, "Escudo_D", shield, new Vector3(227.5f, baseY + 4.2f, 0f), 1.2f, 7);

        var padlockFrames = LoadFolderSprites(PadlockCastleDir);
        var padlock = SpriteObj(p, "Cadeado_Dourado", padlockFrames.FirstOrDefault(), new Vector3(CastleGateX, baseY + 1.6f, 0f), 0.8f, 8);

        var gate = new GameObject("Entrada_Castelo_Fase2");
        gate.transform.SetParent(p);
        gate.transform.position = new Vector3(CastleGateX, baseY + 1.5f, 0f);
        var gc = gate.AddComponent<BoxCollider2D>();
        gc.isTrigger = true;
        gc.size = new Vector2(2.2f, 3f);
        var lg = gate.AddComponent<LevelGate>();
        lg.Configure(GameProgress.SceneFase2, GameProgress.SpawnFase2Entrada, GameProgress.KeyCastelo,
            "O portão do Castelo está trancado por um cadeado dourado.\nDerrote o Ceifador Sombrio para conseguir a chave!",
            "A Chave Dourada abriu o portão do Castelo!",
            "Entrando no Castelo...",
            "[W] Entrar no Castelo");
        lg.ConfigureVisuals(padlock.GetComponent<SpriteRenderer>(), padlockFrames, new[] { gateClosed }, new[] { gateOpen });
        EditorUtility.SetDirty(lg);

        // Onde o cavaleiro aparece ao voltar da Fase 2
        var spawn = new GameObject("Spawn_VoltaDoCastelo");
        spawn.transform.SetParent(p);
        spawn.transform.position = new Vector3(CastleGateX - 7.5f, baseY + 1.2f, 0f);
        spawn.AddComponent<SpawnPoint>().Configure(GameProgress.SpawnFase1Castelo);
    }

    private static void BuildTower(Transform parent, Dictionary<Vector2Int, Sprite> tiles, string name, float leftX, float baseY, bool mirror)
    {
        var tower = Group(parent.gameObject, name);
        for (int c = 13; c <= 15; c++)
        {
            for (int r = 0; r <= 6; r++)
            {
                int localCol = mirror ? (15 - c) : (c - 13);
                PlaceTile(tower, tiles, c, r, leftX + localCol + 0.5f, baseY + (6 - r) + 0.5f, 5, mirror);
            }
        }
    }

    // =====================================================================
    // POÇO ANTIGO (entrada da Fase 3)
    // =====================================================================

    private static void BuildWell(Transform p)
    {
        float y = SurfTunnel;
        var wellVisual = Deco(p, "Well", WellX, y);

        Sprite[] props = LoadSpritesSorted(MedievalProps);
        Sprite crystal = props.FirstOrDefault(x => x.name == "Medieval_props_free_0");
        SpriteObj(p, "Cristal_E", crystal, new Vector3(WellX - 2.4f, y + 0.6f, 0f), 1.1f, 3);
        SpriteObj(p, "Cristal_D", crystal, new Vector3(WellX + 2.4f, y + 0.6f, 0f), 1.1f, 3);

        var padlockFrames = LoadFolderSprites(PadlockWellDir);
        var padlock = SpriteObj(p, "Cadeado_Magico", padlockFrames.FirstOrDefault(), new Vector3(WellX, y + 1.3f, 0f), 0.8f, 8);

        var gate = new GameObject("Entrada_Poco_Fase3");
        gate.transform.SetParent(p);
        gate.transform.position = new Vector3(WellX, y + 1.2f, 0f);
        var gc = gate.AddComponent<BoxCollider2D>();
        gc.isTrigger = true;
        gc.size = new Vector2(2.2f, 2.4f);
        var lg = gate.AddComponent<LevelGate>();
        lg.Configure(GameProgress.SceneFase3, GameProgress.SpawnFase3Entrada, GameProgress.KeyPoco,
            "Um cadeado mágico sela o Poço Antigo.\nA chave está guardada no Castelo (Fase 2) — volte aqui quando encontrá-la.",
            "A Chave de Prata abriu o Poço! Uma luz mágica brilha lá no fundo...",
            "Descendo pelo poço até a Área Mágica...",
            "[W] Descer pelo Poço");
        lg.ConfigureVisuals(padlock.GetComponent<SpriteRenderer>(), padlockFrames, null, null);
        EditorUtility.SetDirty(lg);
    }

    // =====================================================================
    // CENAS PROVISÓRIAS DA FASE 2 E 3
    // =====================================================================

    private static readonly string[] KeepRoots =
        { "Main Camera", "EventSystem", "Global Light 2D", "GameManager", "GameOver", "HUD", "TelaVitoria" };

    private static void CreatePlaceholderScene(string sceneName, System.Action<Tilemap, Transform> buildContent)
    {
        string path = $"Assets/Scenes/{sceneName}.unity";
        if (File.Exists(path))
        {
            Debug.Log($"<color=yellow>[Chroma Defender]</color> {path} já existe — não foi sobrescrita.");
            AddToBuildSettings(path);
            return;
        }

        if (!AssetDatabase.CopyAsset(ScenePath, path))
        {
            Debug.LogError($"[Chroma Defender] Não consegui copiar a SampleScene para {path}.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(path);
        foreach (var go in scene.GetRootGameObjects())
        {
            if (!KeepRoots.Contains(go.name)) Object.DestroyImmediate(go);
        }

        var gridGO = new GameObject("Grid", typeof(Grid));
        var tm = CreateTilemap(gridGO.transform);
        var root = new GameObject("--- " + sceneName.ToUpper() + " (PROVISORIO) ---");
        buildContent(tm, root.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddToBuildSettings(path);
        Debug.Log($"<color=lime>[Chroma Defender]</color> Cena provisória criada: {path}");
    }

    /// <summary>
    /// Fase 2 — Castelo: o LABIRINTO DO CASTELO (8 salas ligadas por portas, Sala do Trono com o Mestre de Armas).
    /// Montado pelo LabirintoCasteloBuilder (também disponível no menu Chroma Defender → Labirinto do Castelo).
    /// </summary>
    private static void BuildFase2Content(Tilemap tm, Transform root)
    {
        LabirintoCasteloBuilder.Build(tm, root, includeSceneEntrance: true);
    }

    /// <summary>
    /// Fase 3 — Área Mágica (base para expandir):
    ///   Mago Sombrio → Sombras Espinhosas → Mago no degrau → Checkpoint → Arena: Arquimago do Caos (vitória).
    /// </summary>
    private static void BuildFase3Content(Tilemap tm, Transform root)
    {
        const float floor = -1f;
        PaintSimpleRoom(tm, -14, 74, new[] { (30, 34, 0) });

        Sprite[] props = LoadSpritesSorted(MedievalProps);
        Sprite crystal = props.FirstOrDefault(x => x.name == "Medieval_props_free_0");
        for (int i = 0; i < 14; i++) SpriteObj(root, "Cristal_" + i, crystal, new Vector3(-8 + i * 6, -0.4f, 0f), 1.1f, 3);

        var spawn = new GameObject("Spawn_Entrada");
        spawn.transform.SetParent(root);
        spawn.transform.position = new Vector3(-5f, 0.5f, 0f);
        spawn.AddComponent<SpawnPoint>().Configure(GameProgress.SpawnFase3Entrada, isDefault: true);

        CreateHint(root, "Dica_Fase3", -6f, floor, 4f,
            "FASE 3 — ÁREA MÁGICA\nOrbes da MESMA cor do seu elemento são absorvidos sem dano!", 6f);

        var enemies = Group(root, "Inimigos");
        PlaceEnemy(enemies, Fase2Fase3PrefabCreator.DarkMage, 10f, floor);
        PlaceEnemy(enemies, Fase2Fase3PrefabCreator.ShadowSpikes, 18f, floor);
        PlaceEnemy(enemies, Fase2Fase3PrefabCreator.ShadowSpikes, 24f, floor);
        PlaceEnemy(enemies, Fase2Fase3PrefabCreator.DarkMage, 32f, 1f);
        CreateCheckpoint(root, "Checkpoint_Arena", 40f, floor);

        var mgr = new GameObject("EnemyRespawnManager");
        mgr.transform.SetParent(root);
        mgr.AddComponent<EnemyRespawnManager>();

        PlaceBoss(root, Fase2Fase3PrefabCreator.BossFase3, 64f, floor, 45f, 71f);
    }

    private static void PlaceEnemy(Transform parent, string prefabName, float x, float surfaceY)
    {
        var prefab = Fase2Fase3PrefabCreator.LoadEnemy(prefabName);
        if (prefab == null) { Debug.LogWarning($"[Chroma Defender] Prefab {prefabName} não encontrado."); return; }
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.transform.position = new Vector3(x, surfaceY + 0.05f, 0f);
    }

    private static void PlaceBoss(Transform parent, string prefabName, float x, float surfaceY, float minX, float maxX)
    {
        var prefab = Fase2Fase3PrefabCreator.LoadBoss(prefabName);
        if (prefab == null) { Debug.LogWarning($"[Chroma Defender] Prefab {prefabName} não encontrado."); return; }
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.transform.position = new Vector3(x, surfaceY + 0.05f, 0f);
        var boss = inst.GetComponent<BossBase>();
        if (boss != null)
        {
            boss.ConfigureArena(minX, maxX);
            PrefabUtility.RecordPrefabInstancePropertyModifications(boss);
        }
    }

    /// <summary>Sala simples: chão em y=-1, paredes laterais e degraus opcionais (x0, x1, linhaTopo).</summary>
    private static void PaintSimpleRoom(Tilemap tm, int x0, int x1, (int a, int b, int top)[] steps)
    {
        var cells = new HashSet<Vector2Int>();
        for (int x = x0; x <= x1; x++) for (int y = -6; y <= -2; y++) cells.Add(new Vector2Int(x, y));
        for (int y = -1; y <= 10; y++)
        {
            cells.Add(new Vector2Int(x0, y)); cells.Add(new Vector2Int(x0 + 1, y));
            cells.Add(new Vector2Int(x1, y)); cells.Add(new Vector2Int(x1 - 1, y));
        }
        foreach (var s in steps)
            for (int x = s.a; x <= s.b; x++) for (int y = -1; y <= s.top; y++) cells.Add(new Vector2Int(x, y));

        TileBase TL = LoadTile(13), T1 = LoadTile(14), TR = LoadTile(16), L = LoadTile(23), M1 = LoadTile(24), R = LoadTile(26);
        foreach (var c in cells)
        {
            bool up = cells.Contains(c + Vector2Int.up), left = cells.Contains(c + Vector2Int.left), right = cells.Contains(c + Vector2Int.right);
            TileBase t = !up ? (!left && right ? TL : left && !right ? TR : T1) : (!left && right ? L : left && !right ? R : M1);
            tm.SetTile(new Vector3Int(c.x, c.y, 0), t);
        }
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private static bool OpenScene(string path)
    {
        var active = SceneManager.GetActiveScene();
        if (active.path == path) return true;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.OpenScene(path);
        return true;
    }

    private static void ClearExisting()
    {
        var root = GameObject.Find(RootName);
        if (root != null) Undo.DestroyObjectImmediate(root);
        foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tm != null && tm.gameObject.name == TilemapName) Undo.DestroyObjectImmediate(tm.gameObject);
        }
    }

    /// <summary>O fim da Fase 1 agora é a bifurcação — os gatilhos de vitória antigos (duplicados) saem.</summary>
    private static void RemoveOldVictoryTriggers()
    {
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name == "GatilhoVitoria") Undo.DestroyObjectImmediate(go);
        }
    }

    private static Grid FindOrCreateGrid()
    {
        var florest = GameObject.Find("Florest");
        if (florest != null && florest.GetComponent<Grid>() != null) return florest.GetComponent<Grid>();
        var go = new GameObject("Grid_Continuacao", typeof(Grid));
        Undo.RegisterCreatedObjectUndo(go, "Grid");
        return go.GetComponent<Grid>();
    }

    private static Tilemap CreateTilemap(Transform gridParent)
    {
        var go = new GameObject(TilemapName, typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        Undo.RegisterCreatedObjectUndo(go, "Tilemap");
        go.transform.SetParent(gridParent, false);
        go.transform.localPosition = Vector3.zero;
        go.layer = GroundLayer;

        int order = 50;
        foreach (var r in Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None))
        {
            if (r.gameObject.name == "FlorestGround") { order = r.sortingOrder; break; }
        }
        go.GetComponent<TilemapRenderer>().sortingOrder = order;
        return go.GetComponent<Tilemap>();
    }

    private static TileBase LoadTile(int index)
    {
        var t = AssetDatabase.LoadAssetAtPath<TileBase>($"{CainosTileDir}TX Tileset Ground_{index}.asset");
        if (t == null) Debug.LogWarning($"[Chroma Defender] Tile TX Tileset Ground_{index} não encontrado.");
        return t;
    }

    private static Transform Group(GameObject parent, string name)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent.transform, false);
        return g.transform;
    }

    private static Transform Group(Transform parent, string name) => Group(parent.gameObject, name);

    /// <summary>Instancia um prefab da Cainos só como enfeite (colisores e física desligados).</summary>
    private static GameObject Deco(Transform parent, string shortName, float x, float surfaceY, float scale = 1f, int sortingOrder = int.MinValue)
    {
        string path = $"{CainosPrefabDir}PF Village Props - {shortName}.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[Chroma Defender] Prefab não encontrado: {path}");
            return null;
        }
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.transform.SetParent(parent);
        inst.transform.position = new Vector3(x, surfaceY, 0f);
        if (scale != 1f) inst.transform.localScale = Vector3.one * scale;

        foreach (var c in inst.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
        foreach (var rb in inst.GetComponentsInChildren<Rigidbody2D>(true)) rb.simulated = false;
        if (sortingOrder != int.MinValue)
            foreach (var sr in inst.GetComponentsInChildren<SpriteRenderer>(true)) sr.sortingOrder = sortingOrder;
        return inst;
    }

    private static void CreateCheckpoint(Transform parent, string name, float x, float surfaceY)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, surfaceY, 0f);
        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(1.5f, 3f);
        bc.offset = new Vector2(0f, 1.5f);
        go.AddComponent<Checkpoint>();
        Deco(go.transform, "Stone of Recall", x, surfaceY);
    }

    private static void CreateHint(Transform parent, string name, float x, float surfaceY, float width, string msg, float duration = 4f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, surfaceY, 0f);
        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(width, 4f);
        bc.offset = new Vector2(0f, 2f);
        go.AddComponent<HintTrigger>().Configure(msg, duration);
    }

    private static void CreateWall(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, y, 0f);
        go.AddComponent<BoxCollider2D>().size = new Vector2(w, h);
    }

    private static GameObject SpriteObj(Transform parent, string name, Sprite sprite, Vector3 pos, float scale, int order, bool flipX = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        sr.flipX = flipX;
        if (sprite == null) Debug.LogWarning($"[Chroma Defender] Sprite ausente para {name}.");
        return go;
    }

    private static Dictionary<Vector2Int, Sprite> LoadMedievalTiles()
    {
        // Textura 1024x824, grade de 48px com margem de 16px. Linha 0 = topo da imagem.
        var dict = new Dictionary<Vector2Int, Sprite>();
        foreach (var s in AssetDatabase.LoadAllAssetsAtPath(MedievalTiles).OfType<Sprite>())
        {
            int col = Mathf.RoundToInt((s.rect.x - 16f) / 48f);
            int row = Mathf.RoundToInt((760f - s.rect.y) / 48f);
            dict[new Vector2Int(col, row)] = s;
        }
        return dict;
    }

    private static void PlaceTile(Transform parent, Dictionary<Vector2Int, Sprite> tiles, int col, int row, float x, float y, int order, bool flipX)
    {
        if (!tiles.TryGetValue(new Vector2Int(col, row), out var sprite)) return; // célula vazia na textura
        SpriteObj(parent, $"T_{col}_{row}", sprite, new Vector3(x, y, 0f), 1f, order, flipX);
    }

    private static Sprite[] LoadSpritesSorted(string texturePath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
    }

    private static Sprite[] LoadFolderSprites(string folder)
    {
        return AssetDatabase.FindAssets("t:Sprite", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct()
            .OrderBy(p => p)
            .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p))
            .Where(s => s != null)
            .ToArray();
    }

    private static void AddToBuildSettings(string path)
    {
        var list = EditorBuildSettings.scenes.ToList();
        if (list.Any(s => s.path == path)) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
