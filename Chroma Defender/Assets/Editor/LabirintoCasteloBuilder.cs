#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// LABIRINTO DO CASTELO — monta a Fase 2 como um labirinto de salas ligadas por portas.
///
///  Primeira Sala ─(🔥Dir)→ Corredor das Armaduras ─(🔥Esq)→ Galeria dos Retratos ─(🔥Grade)→ Antessala ─(🔥Chave)→ Sala do Trono
///       │Esq                     │Dir                            │Esq
///  Cripta Esquecida ─Selada→ Arsenal Esquecido           Salão dos Espelhos
///   (Traiçoeira)              (baú + ALAVANCA)            (CHAVE DE FERRO, Traiçoeiras)
///
/// Pista do labirinto: "Onde a chama arde, o caminho segue" — tochas acesas ao lado da porta certa.
/// Mas a Grade da Galeria só abre com a alavanca do Arsenal e a porta do Trono pede a chave do
/// Salão dos Espelhos: o jogador PRECISA explorar e usar a Porta do Início para voltar.
///
/// Cada sala é uma caixa fechada; elas ficam espalhadas numa grade (4 colunas x 2 linhas).
/// Tudo pode ser reconstruído: o menu apaga só o que ele mesmo criou.
/// </summary>
public static class LabirintoCasteloBuilder
{
    public const string RootName = "--- LABIRINTO DO CASTELO ---";
    private const string GridName = "Grid_Labirinto";
    private const string SolidName = "Labirinto_Paredes";
    private const string BackName = "Labirinto_Fundo";
    private const string OldPlaceholderRoot = "--- FASE2_CASTELO (PROVISORIO) ---";

    private const string MedievalTiles = "Assets/Medieval_pixel_art_asset_FREE/Textures/Medieval_tiles_free2.png";
    private const string TileDir = LabirintoCasteloPresets.Dir + "/Tiles";
    private const string StonePrefab = "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - Stone of Recall.prefab";

    private const int RoomH = 10;      // altura interna das salas
    private const int ColStep = 44;    // distância entre salas (X)
    private const int RowStep = 24;    // distância entre linhas de salas (Y)

    private class Room
    {
        public string key;
        public string name;
        public int x0;       // coluna da parede esquerda
        public int width;    // largura interna
        public int surf;     // y onde os pés pisam
        public Transform group;
        public float L => x0 + 2;           // borda interna esquerda
        public float X(float local) => L + local;
    }

    // =====================================================================
    // MENU
    // =====================================================================

    [MenuItem("Chroma Defender/Labirinto do Castelo/2. Construir Labirinto de Exemplo na cena aberta", priority = 2)]
    public static void BuildMenu()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path == "Assets/Scenes/SampleScene.unity" &&
            !EditorUtility.DisplayDialog("Labirinto do Castelo", "A cena aberta é a Fase 1 (SampleScene).\nO labirinto foi pensado para a Fase 2 (Fase2_Castelo). Construir mesmo assim?", "Construir", "Cancelar"))
            return;

        var old = GameObject.Find(OldPlaceholderRoot);
        if (old != null && EditorUtility.DisplayDialog("Labirinto do Castelo",
                "Esta cena tem o conteúdo provisório antigo da Fase 2.\nRemover para o labirinto ocupar o lugar dele? (Ctrl+Z desfaz)", "Remover", "Manter"))
        {
            Undo.DestroyObjectImmediate(old);
            var oldGrid = GameObject.Find("Grid");
            if (oldGrid != null && oldGrid.GetComponentInChildren<Tilemap>() != null) Undo.DestroyObjectImmediate(oldGrid);
        }

        Clear();

        var grid = new GameObject(GridName, typeof(Grid));
        Undo.RegisterCreatedObjectUndo(grid, "Labirinto");
        var solid = CreateTilemap(grid.transform, SolidName, true, 50);
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Labirinto");

        Build(solid, root.transform, includeSceneEntrance: true);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        Debug.Log("<color=lime>[Labirinto do Castelo]</color> Labirinto construído: 8 salas. Salve a cena (Ctrl+S).");
    }

    [MenuItem("Chroma Defender/Labirinto do Castelo/3. Remover Labirinto de Exemplo", priority = 3)]
    public static void ClearMenu()
    {
        Clear();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void Clear()
    {
        var root = GameObject.Find(RootName);
        if (root != null) Undo.DestroyObjectImmediate(root);
        var grid = GameObject.Find(GridName);
        if (grid != null) Undo.DestroyObjectImmediate(grid);
    }

    // =====================================================================
    // CONSTRUÇÃO
    // =====================================================================

    /// <summary>
    /// Monta o labirinto. "solid" é o Tilemap com colisão (o fundo é criado ao lado dele, no mesmo Grid).
    /// includeSceneEntrance: cria o SpawnPoint de chegada da floresta e a porta de volta (LevelGate).
    /// </summary>
    public static void Build(Tilemap solid, Transform root, bool includeSceneEntrance)
    {
        LabirintoCasteloPresets.CreateAll();
        var tiles = EnsureTiles();
        var back = CreateTilemap(solid.transform.parent, BackName, false, -20);

        var solidCells = new HashSet<Vector2Int>();
        var rooms = new Dictionary<string, Room>();
        Room R(string key, string name, int col, int row, int width = 26)
        {
            var r = new Room { key = key, name = name, x0 = col * ColStep, width = width, surf = -1 - row * RowStep };
            r.group = new GameObject("Sala_" + key).transform;
            r.group.SetParent(root, false);
            PaintRoom(solidCells, back, tiles, r);
            var area = new GameObject("Area_" + key);
            area.transform.SetParent(r.group, false);
            area.transform.position = new Vector3(r.L + width * 0.5f, r.surf + RoomH * 0.5f, 0f);
            area.AddComponent<CastleMazeRoom>();
            rooms[key] = r;
            return r;
        }

        // ---------------------------------------------------------------- salas
        var r0 = R("0_PrimeiraSala", "Primeira Sala", 0, 0);
        var r1 = R("1_Corredor", "Corredor das Armaduras", 1, 0);
        var r2 = R("2_Cripta", "Cripta Esquecida", 2, 0);
        var r3 = R("3_Galeria", "Galeria dos Retratos", 3, 0);
        var r4 = R("4_Arsenal", "Arsenal Esquecido", 0, 1);
        var r5 = R("5_Antessala", "Antessala do Trono", 1, 1);
        var r6 = R("6_Espelhos", "Salão dos Espelhos", 2, 1);
        var r7 = R("7_Trono", "Sala do Trono", 3, 1, 34);

        ConfigureRoom(r0, true, "<b>LABIRINTO DO CASTELO</b>\nCada sala tem portas que levam a caminhos diferentes. Leia a placa!");
        ConfigureRoom(r1, false, "");
        ConfigureRoom(r2, false, "Cripta Esquecida — a grade só se abre quando os mortos voltarem a descansar.");
        ConfigureRoom(r3, false, "");
        ConfigureRoom(r4, false, "Um arsenal abandonado... há um baú e uma alavanca enferrujada.");
        ConfigureRoom(r5, false, "A porta do trono está trancada a ferro. A chave deve estar em algum lugar do labirinto.");
        ConfigureRoom(r6, false, "Salão dos Espelhos — nem toda porta é o que parece.");
        ConfigureRoom(r7, false, "");

        // Degrau na Galeria (posição de tiro do arqueiro)
        for (int x = (int)r3.X(7); x <= (int)r3.X(10); x++) solidCells.Add(new Vector2Int(x, r3.surf));

        PaintSolid(solid, tiles, solidCells);

        // ---------------------------------------------------------------- portas
        // Primeira Sala
        var d0Esq = Door(r0, LabirintoCasteloPresets.PortaComum, 13f, "Porta da Esquerda", "lab_r0_esq");
        var d0Dir = Door(r0, LabirintoCasteloPresets.PortaComum, 22f, "Porta da Direita", "lab_r0_dir", torches: true);

        // Corredor das Armaduras
        var d1Ent = Door(r1, LabirintoCasteloPresets.PortaComum, 2.5f, "Volta", "lab_r1_ent");
        var d1Esq = Door(r1, LabirintoCasteloPresets.PortaComum, 13f, "Porta da Esquerda", "lab_r1_esq", torches: true);
        var d1Dir = Door(r1, LabirintoCasteloPresets.PortaComum, 22f, "Porta da Direita", "lab_r1_dir");

        // Cripta Esquecida
        var d2Ent = Door(r2, LabirintoCasteloPresets.PortaComum, 2.5f, "Volta", "lab_r2_ent");
        var d2Sel = Door(r2, LabirintoCasteloPresets.PortaSelada, 13f, "Porta Selada", "lab_r2_selada");
        var d2Tra = Door(r2, LabirintoCasteloPresets.PortaTraicoeira, 22f, "Porta Misteriosa", "lab_r2_traicoeira");

        // Galeria dos Retratos
        var d3Ent = Door(r3, LabirintoCasteloPresets.PortaComum, 2.5f, "Volta", "lab_r3_ent");
        var d3Esq = Door(r3, LabirintoCasteloPresets.PortaComum, 14f, "Porta da Esquerda", "lab_r3_esq");
        var d3Gra = Door(r3, LabirintoCasteloPresets.PortaAlavanca, 22f, "Grade de Ferro", "lab_r3_grade", torches: true);

        // Arsenal Esquecido
        var d4Ent = Door(r4, LabirintoCasteloPresets.PortaComum, 2.5f, "Volta", "lab_r4_ent");
        var d4Ini = Door(r4, LabirintoCasteloPresets.PortaInicio, 22.5f, "Porta do Início", "lab_r4_inicio");

        // Antessala do Trono
        var d5Ent = Door(r5, LabirintoCasteloPresets.PortaComum, 2.5f, "Volta", "lab_r5_ent");
        var d5Tro = Door(r5, LabirintoCasteloPresets.PortaTrancada, 15f, "Porta do Trono", "lab_r5_trono", torches: true);
        var d5Ini = Door(r5, LabirintoCasteloPresets.PortaInicio, 22.5f, "Porta do Início", "lab_r5_inicio");

        // Salão dos Espelhos
        var d6Ent = Door(r6, LabirintoCasteloPresets.PortaComum, 2.5f, "Volta", "lab_r6_ent");
        var d6TraA = Door(r6, LabirintoCasteloPresets.PortaTraicoeira, 12f, "Porta Misteriosa", "lab_r6_traicoeira_a");
        var d6TraB = Door(r6, LabirintoCasteloPresets.PortaTraicoeira, 17.5f, "Porta Misteriosa", "lab_r6_traicoeira_b");
        var d6Ini = Door(r6, LabirintoCasteloPresets.PortaInicio, 23f, "Porta do Início", "lab_r6_inicio");

        // Sala do Trono
        var d7Ent = Door(r7, LabirintoCasteloPresets.PortaComum, 2.5f, "Volta", "lab_r7_ent");
        Door(r7, LabirintoCasteloPresets.PortaInicio, 6.5f, "Porta do Início", "lab_r7_inicio");

        // ---------------------------------------------------------------- ligações
        TwoWay(d0Esq, d2Ent);
        TwoWay(d0Dir, d1Ent);
        TwoWay(d1Esq, d3Ent);
        TwoWay(d1Dir, d4Ent);
        LabirintoCasteloPresets.Link(d2Sel, d4Ent);          // atalho só de ida para o Arsenal
        TwoWay(d3Esq, d6Ent);
        TwoWay(d3Gra, d5Ent);
        TwoWay(d5Tro, d7Ent);
        RandomTargets(d2Tra, d1Ent, d3Ent, d4Ent, d6Ent);
        RandomTargets(d6TraA, d1Ent, d2Ent, d4Ent, d3Ent);
        RandomTargets(d6TraB, d2Ent, d4Ent, d1Ent, d5Ent); // às vezes dá sorte e pula para a Antessala!

        SetDoor(d3Gra, "requiredFlag", "alavanca_arsenal");
        SetDoor(d3Gra, "lockedMessage", "A Grade de Ferro não se move.\nDizem que a alavanca fica no velho Arsenal...");
        SetDoor(d5Ent, "arrivalIsCheckpoint", true);
        SetDoor(d7Ent, "arrivalIsCheckpoint", true);

        // ---------------------------------------------------------------- placas
        HintSign(r0, 7.5f,
            "<b>LABIRINTO DO CASTELO</b>\n“Onde a chama arde, o caminho segue.”\n" +
            "<size=12>As portas lembram para onde levam. Perdido? A Porta do Início (brilho azul) sempre te traz de volta.</size>");
        foreach (var r in new[] { r1, r2, r3, r4, r5, r6 }) ArrowSign(r, 5.5f);
        HintSign(r2, 17.5f, "A Porta Misteriosa leva a um lugar diferente a cada vez...");

        // ---------------------------------------------------------------- itens, alavanca, baú, chave
        var lever = Place(LabirintoCasteloPresets.Alavanca, r4.group, r4.X(16f), r4.surf);
        var lso = new SerializedObject(lever.GetComponent<CastleLever>());
        LabirintoCasteloPresets.Set(lso, "flagId", "alavanca_arsenal");
        LabirintoCasteloPresets.Set(lso, "pulledMessage", "Você puxou a alavanca... ao longe, a Grade de Ferro da Galeria se ergue!");
        lso.ApplyModifiedProperties();

        // Baús escaláveis: cada um dá o próximo nível da sua habilidade
        PlaceChest(r4, BausEscalaveisCreator.Madeira, 10f);   // Arsenal: Combo Leve
        PlaceChest(r2, BausEscalaveisCreator.Prata, 24.6f);   // Cripta (depois da luta): Ataque Pesado
        PlaceChest(r5, BausEscalaveisCreator.Ouro, 20.4f);    // Antessala, antes do chefe: Ataque Especial
        Place(LabirintoCasteloPresets.Chave, r6.group, r6.X(7.5f), r6.surf + 1.2f);
        CreateCheckpoint(r5, 7f);

        // ---------------------------------------------------------------- inimigos
        Enemy(r1, Fase2Fase3PrefabCreator.SkeletonSword, 8f);
        Enemy(r1, Fase2Fase3PrefabCreator.SkeletonShield, 18f);
        Enemy(r2, Fase2Fase3PrefabCreator.SkeletonSword, 8f);
        Enemy(r2, Fase2Fase3PrefabCreator.SkeletonSword, 17f);
        Enemy(r2, Fase2Fase3PrefabCreator.SkeletonSpear, 19f);
        Enemy(r3, Fase2Fase3PrefabCreator.Archer, 8.5f, 1f);
        Enemy(r3, Fase2Fase3PrefabCreator.SkeletonSpear, 18f);
        Enemy(r4, Fase2Fase3PrefabCreator.SkeletonSpear, 6.5f);
        Enemy(r4, Fase2Fase3PrefabCreator.Archer, 19.5f);
        Enemy(r5, Fase2Fase3PrefabCreator.SkeletonShield, 10.5f);
        Enemy(r5, Fase2Fase3PrefabCreator.Archer, 19f);
        Enemy(r6, Fase2Fase3PrefabCreator.SkeletonSword, 15f);

        // ---------------------------------------------------------------- chefe
        var bossPrefab = Fase2Fase3PrefabCreator.LoadBoss(Fase2Fase3PrefabCreator.BossFase2);
        if (bossPrefab != null)
        {
            var boss = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab, r7.group);
            boss.transform.position = new Vector3(r7.X(27f), r7.surf + 0.05f, 0f);
            var b = boss.GetComponent<BossBase>();
            b.ConfigureArena(r7.X(10f), r7.X(r7.width - 1f));
            PrefabUtility.RecordPrefabInstancePropertyModifications(b);
        }

        // ---------------------------------------------------------------- cenários (um tema por sala)
        foreach (var r in rooms.Values) PlaceScenery(r);

        // ---------------------------------------------------------------- início / gerenciadores
        var start = Place(LabirintoCasteloPresets.Inicio, r0.group, r0.X(6f), r0.surf);
        start.name = "Inicio_Labirinto";
        Place(LabirintoCasteloPresets.Gerenciador, root, r0.X(0f), r0.surf);
        if (Object.FindAnyObjectByType<EnemyRespawnManager>() == null)
        {
            var mgr = new GameObject("EnemyRespawnManager");
            mgr.transform.SetParent(root);
            mgr.AddComponent<EnemyRespawnManager>();
        }

        if (includeSceneEntrance) BuildSceneEntrance(r0);
    }

    /// <summary>Chegada vinda da floresta + porta (LevelGate) para voltar à Fase 1.</summary>
    private static void BuildSceneEntrance(Room r0)
    {
        var props = LabirintoCasteloPresets.Props();
        var arch = LabirintoCasteloPresets.P(props, 4);

        var gate = new GameObject("Saida_VoltarFloresta");
        gate.transform.SetParent(r0.group);
        gate.transform.position = new Vector3(r0.X(2.5f), r0.surf + 1.5f, 0f);
        var vis = LabirintoCasteloPresets.Child(gate.transform, "Visual", arch, Vector3.zero, 2.6f, 4);
        vis.GetComponent<SpriteRenderer>().color = new Color(0.75f, 1f, 0.75f);
        var bc = gate.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(1.6f, 3f);
        gate.AddComponent<LevelGate>().Configure(GameProgress.SceneFase1, GameProgress.SpawnFase1Castelo, "",
            "", "", "Voltando à Floresta...", "[W] Voltar à Floresta");

        var spawn = new GameObject("Spawn_Entrada");
        spawn.transform.SetParent(r0.group);
        spawn.transform.position = new Vector3(r0.X(6f), r0.surf + 1.2f, 0f);
        spawn.AddComponent<SpawnPoint>().Configure(GameProgress.SpawnFase2Entrada, isDefault: true);
    }

    // =====================================================================
    // HELPERS DE CONTEÚDO
    // =====================================================================

    private static void ConfigureRoom(Room r, bool start, string msg)
    {
        var room = r.group.GetComponentInChildren<CastleMazeRoom>();
        room.Configure(r.name, new Vector2(r.width, RoomH), start, msg);
        EditorUtility.SetDirty(room);
    }

    private static GameObject Place(string preset, Transform parent, float x, float y)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(LabirintoCasteloPresets.Load(preset), parent);
        inst.transform.position = new Vector3(x, y, 0f);
        return inst;
    }

    private static CastleDoor Door(Room r, string preset, float localX, string title, string id, bool torches = false)
    {
        var go = Place(preset, r.group, r.X(localX), r.surf);
        go.name = $"{preset}_{id}";
        var door = go.GetComponent<CastleDoor>();
        var so = new SerializedObject(door);
        LabirintoCasteloPresets.Set(so, "doorTitle", title);
        LabirintoCasteloPresets.Set(so, "doorId", id);
        so.ApplyModifiedProperties();

        if (torches)
        {
            // A pista do labirinto: tochas acesas dos dois lados da porta certa.
            var torch = LabirintoCasteloPresets.P(LabirintoCasteloPresets.Props(), 21);
            foreach (float side in new[] { -1.9f, 1.9f })
            {
                var t = LabirintoCasteloPresets.Child(go.transform, "Tocha", torch, new Vector3(side, 2.3f, 0f), 1.4f, 5);
                t.name = side < 0 ? "Tocha_E" : "Tocha_D";
            }
        }
        return door;
    }

    private static void SetDoor(CastleDoor d, string field, object value)
    {
        var so = new SerializedObject(d);
        LabirintoCasteloPresets.Set(so, field, value);
        so.ApplyModifiedProperties();
    }

    private static void TwoWay(CastleDoor a, CastleDoor b)
    {
        LabirintoCasteloPresets.Link(a, b);
        LabirintoCasteloPresets.Link(b, a);
    }

    private static void RandomTargets(CastleDoor d, params CastleDoor[] targets)
    {
        var so = new SerializedObject(d);
        LabirintoCasteloPresets.SetArray(so, "randomDestinations", targets);
        so.ApplyModifiedProperties();
    }

    private static void ArrowSign(Room r, float localX)
    {
        var go = Place(LabirintoCasteloPresets.PlacaSeta, r.group, r.X(localX), r.surf);
        go.name = "Placa_Seta_PrimeiraSala";
    }

    private static void HintSign(Room r, float localX, string text)
    {
        var go = Place(LabirintoCasteloPresets.PlacaDica, r.group, r.X(localX), r.surf);
        var so = new SerializedObject(go.GetComponent<CastleSign>());
        LabirintoCasteloPresets.Set(so, "manualText", text);
        so.ApplyModifiedProperties();
    }

    private static void Enemy(Room r, string prefab, float localX, float yAbove = 0f)
    {
        var p = Fase2Fase3PrefabCreator.LoadEnemy(prefab);
        if (p == null) return;
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(p, r.group);
        inst.transform.position = new Vector3(r.X(localX), r.surf + yAbove + 0.05f, 0f);
    }

    private static void PlaceChest(Room r, string chestPrefab, float localX)
    {
        var prefab = BausEscalaveisCreator.Load(chestPrefab);
        if (prefab == null) { Debug.LogWarning("[Labirinto do Castelo] Baú não encontrado: " + chestPrefab); return; }
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, r.group);
        inst.transform.position = new Vector3(r.X(localX), r.surf, 0f);
        var chest = inst.GetComponentInChildren<UpgradeChest>();
        if (chest == null) return;
        // Id fixo por sala: o baú continua "aberto" mesmo se o labirinto for reconstruído em outra posição
        var so = new SerializedObject(chest);
        LabirintoCasteloPresets.Set(so, "chestId", $"labirinto_{r.key}_{chestPrefab}");
        // Nível deste baú na progressão do labirinto (sobrepõe o padrão do prefab)
        if (chestPrefab == BausEscalaveisCreator.Madeira) LabirintoCasteloPresets.Set(so, "lightComboHits", 4);
        if (chestPrefab == BausEscalaveisCreator.Prata) LabirintoCasteloPresets.Set(so, "heavyComboHits", 2);
        so.ApplyModifiedProperties();
    }

    /// <summary>Cenário temático da sala (LabirintoCenariosCreator). Sem tema → decoração padrão.</summary>
    private static void PlaceScenery(Room r)
    {
        var prefab = LabirintoCenariosCreator.Load(r.key);
        if (prefab == null) { DecorateRoom(r); return; }
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, r.group);
        inst.transform.position = new Vector3(r.L, r.surf, 0f);
    }

    private static void CreateCheckpoint(Room r, float localX)
    {
        var go = new GameObject("Checkpoint_" + r.name.Replace(" ", ""));
        go.transform.SetParent(r.group);
        go.transform.position = new Vector3(r.X(localX), r.surf, 0f);
        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(1.5f, 3f);
        bc.offset = new Vector2(0f, 1.5f);
        go.AddComponent<Checkpoint>();

        var stone = AssetDatabase.LoadAssetAtPath<GameObject>(StonePrefab);
        if (stone != null)
        {
            var s = (GameObject)PrefabUtility.InstantiatePrefab(stone, go.transform);
            s.transform.position = go.transform.position;
            foreach (var c in s.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
            foreach (var rb in s.GetComponentsInChildren<Rigidbody2D>(true)) rb.simulated = false;
        }
    }

    /// <summary>Decoração padrão (salas sem tema): janelas de cristal e brasões (sem tochas — tocha é a pista da porta certa).</summary>
    private static void DecorateRoom(Room r)
    {
        var props = LabirintoCasteloPresets.Props();
        var window = LabirintoCasteloPresets.P(props, 0);
        var shield = LabirintoCasteloPresets.P(props, 20);
        var deco = new GameObject("Decoracao").transform;
        deco.SetParent(r.group, false);
        for (float x = 8f; x < r.width - 2f; x += 9f)
        {
            var w = LabirintoCasteloPresets.Child(deco, "Janela", window, Vector3.zero, 1.6f, -10);
            w.transform.position = new Vector3(r.X(x), r.surf + 6.2f, 0f);
        }
        for (float x = 4f; x < r.width - 2f; x += 9f)
        {
            var s = LabirintoCasteloPresets.Child(deco, "Brasao", shield, Vector3.zero, 1.1f, -10);
            s.transform.position = new Vector3(r.X(x), r.surf + 5.2f, 0f);
        }
    }

    // =====================================================================
    // TERRENO
    // =====================================================================

    private class Tiles
    {
        public TileBase TL, T, TR, L, M, R;
        public TileBase[] Back;
    }

    /// <summary>Caixa fechada: chão (3), paredes (2 de cada lado) e teto (2). O fundo é pintado no tilemap sem colisão.</summary>
    private static void PaintRoom(HashSet<Vector2Int> cells, Tilemap back, Tiles tiles, Room r)
    {
        int x0 = r.x0, x1 = r.x0 + r.width + 3, s = r.surf;
        for (int x = x0; x <= x1; x++)
        {
            for (int y = s - 3; y <= s - 1; y++) cells.Add(new Vector2Int(x, y));               // chão
            for (int y = s + RoomH; y <= s + RoomH + 1; y++) cells.Add(new Vector2Int(x, y));   // teto
        }
        for (int y = s; y < s + RoomH; y++)
        {
            cells.Add(new Vector2Int(x0, y)); cells.Add(new Vector2Int(x0 + 1, y));
            cells.Add(new Vector2Int(x1, y)); cells.Add(new Vector2Int(x1 - 1, y));
        }

        for (int x = x0 + 2; x <= x1 - 2; x++)
            for (int y = s; y < s + RoomH; y++)
                back.SetTile(new Vector3Int(x, y, 0), tiles.Back[Mathf.Abs(x * 7 + y * 13) % tiles.Back.Length]);
    }

    private static void PaintSolid(Tilemap tm, Tiles t, HashSet<Vector2Int> cells)
    {
        foreach (var c in cells)
        {
            bool up = cells.Contains(c + Vector2Int.up);
            bool left = cells.Contains(c + Vector2Int.left);
            bool right = cells.Contains(c + Vector2Int.right);
            TileBase tile;
            if (!up) tile = !left && right ? t.TL : left && !right ? t.TR : t.T;
            else tile = !left && right ? t.L : left && !right ? t.R : t.M;
            tm.SetTile(new Vector3Int(c.x, c.y, 0), tile);
        }
        tm.CompressBounds();
    }

    /// <summary>Cria (uma vez) os Tile assets do castelo a partir do tileset medieval.</summary>
    private static Tiles EnsureTiles()
    {
        if (!AssetDatabase.IsValidFolder(TileDir))
        {
            if (!AssetDatabase.IsValidFolder(LabirintoCasteloPresets.Dir)) AssetDatabase.CreateFolder("Assets/Prefabs", "LabirintoCastelo");
            AssetDatabase.CreateFolder(LabirintoCasteloPresets.Dir, "Tiles");
        }

        // Textura 1024x824, grade de 48px com margem de 16px. Linha 0 = topo da imagem.
        var grid = new Dictionary<Vector2Int, Sprite>();
        foreach (var s in AssetDatabase.LoadAllAssetsAtPath(MedievalTiles).OfType<Sprite>())
        {
            int col = Mathf.RoundToInt((s.rect.x - 16f) / 48f);
            int row = Mathf.RoundToInt((760f - s.rect.y) / 48f);
            grid[new Vector2Int(col, row)] = s;
        }

        TileBase Make(string name, int col, int row, bool collide, Color color)
        {
            string path = $"{TileDir}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            bool isNew = tile == null;
            if (isNew) tile = ScriptableObject.CreateInstance<Tile>();
            grid.TryGetValue(new Vector2Int(col, row), out var sprite);
            if (sprite == null) Debug.LogWarning($"[Labirinto do Castelo] Tile medieval ({col},{row}) não encontrado.");
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = collide ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            if (isNew) AssetDatabase.CreateAsset(tile, path);
            else EditorUtility.SetDirty(tile);
            return tile;
        }

        var dark = new Color(0.5f, 0.5f, 0.62f);
        var t = new Tiles
        {
            TL = Make("Castelo_TopoEsq", 0, 8, true, Color.white),
            T = Make("Castelo_Topo", 1, 8, true, Color.white),
            TR = Make("Castelo_TopoDir", 2, 8, true, Color.white),
            L = Make("Castelo_Esq", 0, 9, true, Color.white),
            M = Make("Castelo_Meio", 1, 9, true, Color.white),
            R = Make("Castelo_Dir", 2, 9, true, Color.white),
            Back = new[]
            {
                Make("Castelo_Fundo_0", 0, 0, false, dark),
                Make("Castelo_Fundo_1", 1, 0, false, dark),
                Make("Castelo_Fundo_2", 2, 0, false, dark),
                Make("Castelo_Fundo_3", 3, 0, false, dark),
            }
        };
        AssetDatabase.SaveAssets();
        return t;
    }

    private static Tilemap CreateTilemap(Transform gridParent, string name, bool collide, int order)
    {
        var existing = gridParent.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var go = collide
            ? new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D))
            : new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(gridParent, false);
        if (collide)
        {
            int ground = LayerMask.NameToLayer("Ground");
            go.layer = ground >= 0 ? ground : 6;
        }
        go.GetComponent<TilemapRenderer>().sortingOrder = order;
        return go.GetComponent<Tilemap>();
    }
}
#endif
