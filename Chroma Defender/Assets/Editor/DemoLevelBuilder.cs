#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Construtor automatizado do mapa da fase de demonstração de Chroma Defender.
/// Gera via código: cenários de paralaxe, plataformas, armadilha de chão falso,
/// chave dourada, porta trancada, inimigos com polaridade e câmara secreta.
/// </summary>
public static class DemoLevelBuilder
{
    private const string RootName = "--- DEMO LEVEL ---";

    // Layer de chão configurada no projeto
    private static int GroundLayer => LayerMask.NameToLayer("Ground") >= 0 ? LayerMask.NameToLayer("Ground") : 6;

    [MenuItem("Chroma Defender/1. Construir Mapa da Demo (Completo)")]
    public static void BuildDemoLevelMenu()
    {
        ClearExistingLevel();
        BuildLevel();
    }

    [MenuItem("Chroma Defender/2. Limpar Mapa da Demo")]
    public static void ClearExistingLevelMenu()
    {
        ClearExistingLevel();
    }

    private static void ClearExistingLevel()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            Debug.Log("<color=yellow>[Chroma Defender]</color> Mapa anterior removido com sucesso.");
        }
    }

    public static void BuildLevel()
    {
        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Construir Mapa da Demo");

        // 1. Câmera e Jogador
        SetupCameraAndPlayer(root.transform);

        // 2. Fundo Parallax e Atmosfera
        Transform bgGroup = new GameObject("01_Background_Parallax").transform;
        bgGroup.SetParent(root.transform);
        BuildParallaxBackground(bgGroup);

        // 3. Geometria do Cenário e Plataformas
        Transform terrainGroup = new GameObject("02_Terrain_And_Platforms").transform;
        terrainGroup.SetParent(root.transform);
        BuildTerrain(terrainGroup);

        // 4. Objetos Interativos, Puzzles e Baús
        Transform interactiveGroup = new GameObject("03_Interactive_And_Puzzles").transform;
        interactiveGroup.SetParent(root.transform);
        BuildInteractiveObjects(interactiveGroup);

        // 5. Inimigos da Fase
        Transform enemiesGroup = new GameObject("04_Enemies").transform;
        enemiesGroup.SetParent(root.transform);
        BuildEnemies(enemiesGroup);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=green>[Chroma Defender]</color> ★ Mapa da Demo construído com sucesso! Pressione PLAY para testar.");
    }

    // ------------------------------------------------------------------
    // 1. Câmera e Posicionamento do Jogador
    // ------------------------------------------------------------------
    private static void SetupCameraAndPlayer(Transform root)
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow2D>();

            // Configura limites do mapa da demo
            var minProp = new SerializedObject(follow).FindProperty("minBounds");
            var maxProp = new SerializedObject(follow).FindProperty("maxBounds");
            if (minProp != null && maxProp != null)
            {
                var so = new SerializedObject(follow);
                so.FindProperty("minBounds").vector2Value = new Vector2(-4f, -9f);
                so.FindProperty("maxBounds").vector2Value = new Vector2(105f, 15f);
                so.FindProperty("offset").vector3Value = new Vector3(0f, 1.2f, -10f);
                so.FindProperty("smoothTime").floatValue = 0.2f;
                so.ApplyModifiedProperties();
            }
        }

        PlayerBehavior player = Object.FindFirstObjectByType<PlayerBehavior>();
        if (player != null)
        {
            player.transform.position = new Vector3(-2f, 0.5f, 0f);
            player.gameObject.layer = LayerMask.NameToLayer("Default");
        }
    }

    // ------------------------------------------------------------------
    // 2. Fundo Parallax
    // ------------------------------------------------------------------
    private static void BuildParallaxBackground(Transform parent)
    {
        Sprite skySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FREE Parallax Forest Background HQ/Parallax Forest Background/01_Sky.png");
        Sprite forestNear = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FREE Parallax Forest Background HQ/Parallax Forest Background/02_Forest.png");
        Sprite forestMid = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FREE Parallax Forest Background HQ/Parallax Forest Background/03_Forest.png");
        Sprite forestFar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FREE Parallax Forest Background HQ/Parallax Forest Background/04_Forest.png");

        if (skySprite != null)
        {
            CreateBackdropQuad(parent, "Sky_01", skySprite, new Vector3(15f, 5f, 0f), new Vector3(3.5f, 3f, 1f), -50);
            CreateBackdropQuad(parent, "Sky_02", skySprite, new Vector3(65f, 5f, 0f), new Vector3(3.5f, 3f, 1f), -50);
        }

        if (forestFar != null)
        {
            CreateBackdropQuad(parent, "ForestFar", forestFar, new Vector3(12f, 3f, 0f), new Vector3(3.2f, 3.2f, 1f), -40);
        }
        if (forestMid != null)
        {
            CreateBackdropQuad(parent, "ForestMid", forestMid, new Vector3(14f, 2f, 0f), new Vector3(3.2f, 3.2f, 1f), -30);
        }
        if (forestNear != null)
        {
            CreateBackdropQuad(parent, "ForestNear", forestNear, new Vector3(15f, 1f, 0f), new Vector3(3.2f, 3.2f, 1f), -20);
        }

        // Fundo de pedra escura para o interior do castelo
        CreatePlatform(parent, "Castle_Backdrop_Stone", new Vector2(65f, 2f), new Vector2(65f, 14f), new Color(0.12f, 0.13f, 0.17f, 1f), -15, false);
    }

    private static void CreateBackdropQuad(Transform parent, string name, Sprite sprite, Vector3 pos, Vector3 scale, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
    }

    // ------------------------------------------------------------------
    // 3. Geometria do Terreno e Plataformas
    // ------------------------------------------------------------------
    private static void BuildTerrain(Transform parent)
    {
        Color grassColor = new Color(0.38f, 0.62f, 0.32f);
        Color dirtColor = new Color(0.28f, 0.22f, 0.16f);
        Color woodColor = new Color(0.55f, 0.42f, 0.26f);
        Color stoneColor = new Color(0.35f, 0.37f, 0.42f);
        Color dungeonColor = new Color(0.22f, 0.24f, 0.28f);

        // --- ZONA 1: Exterior / Floresta (x: -5 a 28) ---
        // Chão principal
        CreatePlatform(parent, "Forest_Ground", new Vector2(11.5f, -1f), new Vector2(35f, 2f), grassColor);
        // Subterrâneo visual
        CreatePlatform(parent, "Forest_Underground", new Vector2(11.5f, -3f), new Vector2(35f, 2f), dirtColor, -5, false);

        // Plataformas de salto no exterior
        CreatePlatform(parent, "Jump_Platform_1", new Vector2(7f, 1.2f), new Vector2(3.5f, 0.5f), woodColor);
        CreatePlatform(parent, "Jump_Platform_2", new Vector2(12.5f, 2.3f), new Vector2(3.5f, 0.5f), woodColor);

        // Parede de limite esquerdo do mapa
        CreatePlatform(parent, "Left_Boundary_Wall", new Vector2(-6f, 4f), new Vector2(2f, 12f), stoneColor);

        // --- ZONA 2: Muralha e Portão do Castelo (x: 28 a 38) ---
        // Torre/Muralha externa
        CreatePlatform(parent, "Castle_Rampart_Wall", new Vector2(28f, 5f), new Vector2(2.5f, 10f), stoneColor);
        CreatePlatform(parent, "Castle_Gate_Floor", new Vector2(33f, -1f), new Vector2(10f, 2f), stoneColor);
        CreatePlatform(parent, "Castle_Gate_Arch", new Vector2(33f, 4.5f), new Vector2(10f, 2f), stoneColor);

        // --- ZONA 3: Interior do Castelo (x: 38 a 55) ---
        // Chão do Salão Principal
        CreatePlatform(parent, "Hall_Ground", new Vector2(44f, -1f), new Vector2(14f, 2f), stoneColor);
        // Teto do Castelo
        CreatePlatform(parent, "Castle_Ceiling", new Vector2(65f, 7.5f), new Vector2(65f, 2f), stoneColor);

        // Plataformas para subir na bifurcação
        CreatePlatform(parent, "Stair_Step_1", new Vector2(43f, 1.3f), new Vector2(2.8f, 0.5f), woodColor);
        CreatePlatform(parent, "Stair_Step_2", new Vector2(40f, 3.0f), new Vector2(3.0f, 0.5f), woodColor);

        // --- CAMINHO FALSO (Elevado à esquerda, x: 32 a 42, y: 5.0) ---
        CreatePlatform(parent, "FakePath_Floor", new Vector2(37f, 4.8f), new Vector2(11f, 0.8f), stoneColor);
        CreatePlatform(parent, "FakePath_DeadEnd_Wall", new Vector2(31f, 6.5f), new Vector2(1.5f, 4f), stoneColor);

        // --- CAMINHO VERDADEIRO: A Ponte da Chave (x: 53 a 76, y: 2.2) ---
        // Passarela de acesso
        CreatePlatform(parent, "Bridge_Access", new Vector2(56f, 1.4f), new Vector2(3.5f, 0.5f), woodColor);
        CreatePlatform(parent, "Bridge_Main", new Vector2(62f, 2.2f), new Vector2(9f, 0.7f), stoneColor);

        // O Pedestal Ledge (onde a chave fica visível)
        CreatePlatform(parent, "Key_Pedestal_Ledge", new Vector2(74f, 2.2f), new Vector2(5f, 0.7f), stoneColor);
        CreatePlatform(parent, "Key_Stone_Pedestal", new Vector2(74f, 2.8f), new Vector2(1f, 0.6f), new Color(0.7f, 0.65f, 0.55f));

        // --- FOSSO INFERIOR (Subterrâneo onde o jogador cai, x: 60 a 78, y: -4.5) ---
        CreatePlatform(parent, "Pit_Floor", new Vector2(69f, -4.5f), new Vector2(19f, 1.8f), dungeonColor);
        CreatePlatform(parent, "Pit_Wall_Left", new Vector2(59f, -1.2f), new Vector2(1.8f, 6f), dungeonColor);
        CreatePlatform(parent, "Pit_Wall_Right", new Vector2(79f, -1.2f), new Vector2(1.8f, 6f), dungeonColor);

        // Plataformas verticais de escalada para sair do fosso
        CreatePlatform(parent, "Climb_Step_1", new Vector2(62f, -2.8f), new Vector2(2.5f, 0.5f), woodColor);
        CreatePlatform(parent, "Climb_Step_2", new Vector2(66.5f, -1.3f), new Vector2(2.5f, 0.5f), woodColor);
        CreatePlatform(parent, "Climb_Step_3", new Vector2(62.5f, 0.3f), new Vector2(2.5f, 0.5f), woodColor);
        CreatePlatform(parent, "Climb_Step_4", new Vector2(67f, 1.6f), new Vector2(2.5f, 0.5f), woodColor);

        // --- ZONA 4: Salão da Câmara Secreta (atrás da porta trancada, x: 50 a 92) ---
        CreatePlatform(parent, "Secret_Chamber_Floor", new Vector2(71f, -1f), new Vector2(40f, 2f), new Color(0.25f, 0.28f, 0.35f));
        CreatePlatform(parent, "Secret_Chamber_EndWall", new Vector2(92f, 3.2f), new Vector2(2f, 10f), stoneColor);

        // Parede de pedra sólida acima da porta trancada (impede passar por cima)
        CreatePlatform(parent, "Door_Header_Wall", new Vector2(50f, 5.0f), new Vector2(1.6f, 4.5f), stoneColor);
    }

    // ------------------------------------------------------------------
    // 4. Objetos Interativos, Puzzles e Baús
    // ------------------------------------------------------------------
    private static void BuildInteractiveObjects(Transform parent)
    {
        // 1. Baú Inicial de Madeira (Desbloqueia Ataque Pesado)
        CreateChest(parent, "Initial_Chest_Wooden", new Vector3(17f, 0.1f, 0f), ChestRewardType.UnlockHeavyAttack, false);

        // 2. Portão Elemental (Cristal de Gelo que exige Fogo)
        CreateElementalBarrier(parent, "Ice_Crystal_Barrier", new Vector3(36f, 0.6f, 0f), ElementPolarity.Fire);

        // 3. A Grande Porta Trancada do Castelo
        CreateLockedDoor(parent, "Castle_Grand_Locked_Door", new Vector3(50f, 0.7f, 0f));

        // 4. Baú do Caminho Falso (Cura Total de Elixir)
        CreateChest(parent, "FakePath_Bonus_Chest", new Vector3(35f, 5.4f, 0f), ChestRewardType.FullHeal, false);

        // 5. O Chão Falso diante da Chave (Crumbling Platform)
        CreateCrumblingTrap(parent, "Key_Trap_CrumblingFloor", new Vector3(69f, 2.2f, 0f), new Vector2(4.5f, 0.7f));

        // 6. A Chave Dourada Flutuante
        CreateGoldenKey(parent, "Castle_Golden_Key", new Vector3(74f, 3.5f, 0f));

        // 7. O Grande Baú Dourado da Câmara Secreta (Recompensa da Demo)
        CreateChest(parent, "Secret_Demo_Golden_Chest", new Vector3(84f, 0.2f, 0f), ChestRewardType.SecretDemoTreasure, true);

        // 8. Tochas e Decorações
        CreateTorch(parent, new Vector3(31f, 1.8f, 0f));
        CreateTorch(parent, new Vector3(38f, 1.8f, 0f));
        CreateTorch(parent, new Vector3(48f, 1.8f, 0f));
        CreateTorch(parent, new Vector3(52f, 1.8f, 0f));
        CreateTorch(parent, new Vector3(78f, 1.8f, 0f));
        CreateTorch(parent, new Vector3(86f, 1.8f, 0f));
    }

    private static void CreateChest(Transform parent, string name, Vector3 pos, ChestRewardType reward, bool golden)
    {
        string prefabPath = golden ?
            "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - Chest Golden.prefab" :
            "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - Chest Wooden.prefab";

        GameObject chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject chestObj = null;

        if (chestPrefab != null)
        {
            chestObj = (GameObject)PrefabUtility.InstantiatePrefab(chestPrefab);
            chestObj.name = name;
        }
        else
        {
            chestObj = new GameObject(name);
            var sr = chestObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultSquareSprite();
            sr.color = golden ? new Color(1f, 0.85f, 0.2f) : new Color(0.6f, 0.4f, 0.2f);
        }

        chestObj.transform.SetParent(parent);
        chestObj.transform.position = pos;

        var box = chestObj.GetComponent<BoxCollider2D>();
        if (box == null) box = chestObj.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.2f, 1.0f);
        box.isTrigger = false;

        var interactable = chestObj.GetComponent<ChestInteractable>();
        if (interactable == null) interactable = chestObj.AddComponent<ChestInteractable>();

        var so = new SerializedObject(interactable);
        so.FindProperty("rewardType").enumValueIndex = (int)reward;
        so.ApplyModifiedProperties();
    }

    private static void CreateElementalBarrier(Transform parent, string name, Vector3 pos, ElementPolarity required)
    {
        GameObject barrier = new GameObject(name);
        barrier.transform.SetParent(parent);
        barrier.transform.position = pos;

        var sr = barrier.AddComponent<SpriteRenderer>();
        sr.sprite = GetDefaultSquareSprite();
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(1.2f, 2.8f);
        sr.sortingOrder = 5;

        var box = barrier.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.2f, 2.8f);

        var trigger = barrier.AddComponent<PolarityTrigger>();
        var so = new SerializedObject(trigger);
        so.FindProperty("requiredPolarity").enumValueIndex = (int)required;
        so.ApplyModifiedProperties();
    }

    private static void CreateLockedDoor(Transform parent, string name, Vector3 pos)
    {
        GameObject door = new GameObject(name);
        door.transform.SetParent(parent);
        door.transform.position = pos;

        var sr = door.AddComponent<SpriteRenderer>();
        sr.sprite = GetDefaultSquareSprite();
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(1.4f, 3.2f);
        sr.color = new Color(0.45f, 0.35f, 0.25f);
        sr.sortingOrder = 4;

        var box = door.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.4f, 3.2f);

        door.AddComponent<LockedDoor>();
    }

    private static void CreateCrumblingTrap(Transform parent, string name, Vector3 pos, Vector2 size)
    {
        GameObject trap = new GameObject(name);
        trap.transform.SetParent(parent);
        trap.transform.position = pos;
        trap.layer = GroundLayer;

        var sr = trap.AddComponent<SpriteRenderer>();
        sr.sprite = GetDefaultSquareSprite();
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.color = new Color(0.5f, 0.46f, 0.42f);
        sr.sortingOrder = 3;

        var box = trap.AddComponent<BoxCollider2D>();
        box.size = size;

        trap.AddComponent<CrumblingPlatform>();
    }

    private static void CreateGoldenKey(Transform parent, string name, Vector3 pos)
    {
        GameObject keyObj = new GameObject(name);
        keyObj.transform.SetParent(parent);
        keyObj.transform.position = pos;
        keyObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        var sr = keyObj.AddComponent<SpriteRenderer>();
        Sprite keySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/karsiori/Pixel Art Key Pack - V2/Key 1 - Gold.png");
        sr.sprite = keySprite != null ? keySprite : GetDefaultSquareSprite();
        sr.sortingOrder = 8;
        sr.color = new Color(1f, 0.88f, 0.25f, 1f);

        var circle = keyObj.AddComponent<CircleCollider2D>();
        circle.radius = 0.5f;
        circle.isTrigger = true;

        keyObj.AddComponent<KeyPickup>();
    }

    private static void CreateTorch(Transform parent, Vector3 pos)
    {
        var torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - Torch.prefab");
        if (torchPrefab != null)
        {
            GameObject torch = (GameObject)PrefabUtility.InstantiatePrefab(torchPrefab);
            torch.transform.SetParent(parent);
            torch.transform.position = pos;
        }
    }

    // ------------------------------------------------------------------
    // 5. Inimigos da Fase
    // ------------------------------------------------------------------
    private static void BuildEnemies(Transform parent)
    {
        // 1. Slime Neutro na Floresta
        CreateEnemy(parent, "Enemy_Slime_Neutral", new Vector3(10f, 0.2f, 0f), EnemyPolarityType.Neutral, 50, 3f, false);

        // 2. Rato Neutro na Floresta
        CreateEnemy(parent, "Enemy_Rat_Neutral", new Vector3(23f, 0.2f, 0f), EnemyPolarityType.Neutral, 40, 2.5f, true);

        // 3. Slime de Gelo diante do Castelo (Requer FOGO)
        CreateEnemy(parent, "Enemy_Ice_Slime_Gate", new Vector3(32.5f, 0.2f, 0f), EnemyPolarityType.Ice, 65, 2.5f, false);

        // 4. Inimigos do Caminho Falso (1 Fogo + 1 Gelo)
        CreateEnemy(parent, "FakePath_Fire_Rat", new Vector3(35f, 5.6f, 0f), EnemyPolarityType.Fire, 55, 2.5f, true);
        CreateEnemy(parent, "FakePath_Ice_Slime", new Vector3(40f, 5.6f, 0f), EnemyPolarityType.Ice, 65, 2.5f, false);

        // 5. Inimigos do Fosso Inferior (Onde o jogador cai)
        CreateEnemy(parent, "Pit_Ice_Rat", new Vector3(64f, -3.3f, 0f), EnemyPolarityType.Ice, 55, 3f, true);
        CreateEnemy(parent, "Pit_Fire_Slime", new Vector3(74f, -3.3f, 0f), EnemyPolarityType.Fire, 65, 3f, false);

        // 6. Guardiões da Câmara Secreta
        CreateEnemy(parent, "SecretChamber_Fire_Guardian", new Vector3(64f, 0.2f, 0f), EnemyPolarityType.Fire, 80, 3f, true);
        CreateEnemy(parent, "SecretChamber_Ice_Guardian", new Vector3(76f, 0.2f, 0f), EnemyPolarityType.Ice, 80, 3f, false);
    }

    private static void CreateEnemy(Transform parent, string name, Vector3 pos, EnemyPolarityType polarity, int health, float patrolDist, bool isRat)
    {
        GameObject enemy = new GameObject(name);
        enemy.transform.SetParent(parent);
        enemy.transform.position = pos;
        enemy.transform.localScale = new Vector3(2f, 2f, 1f);

        var sr = enemy.AddComponent<SpriteRenderer>();
        string spritePath = isRat ?
            "Assets/Monsters Creatures Fantasy 2/Sprites/Rat/idle.png" :
            "Assets/Monsters Creatures Fantasy 2/Sprites/Slime/idle.png";

        // Tenta carregar sub-sprite sliced
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath) as Sprite[];
        if (sprites != null && sprites.Length > 0)
        {
            sr.sprite = sprites[0];
        }
        else
        {
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        if (sr.sprite == null) sr.sprite = GetDefaultSquareSprite();
        sr.sortingOrder = 3;

        var rb = enemy.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.8f);

        var comp = enemy.AddComponent<ElementalEnemy>();
        var so = new SerializedObject(comp);
        so.FindProperty("polarity").enumValueIndex = (int)polarity;
        so.FindProperty("maxHealth").intValue = health;
        so.FindProperty("patrolDistance").floatValue = patrolDist;
        so.ApplyModifiedProperties();
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    private static GameObject CreatePlatform(Transform parent, string name, Vector2 pos, Vector2 size, Color color, int sortingOrder = 1, bool addCollider = true)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        if (addCollider) go.layer = GroundLayer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetDefaultSquareSprite();
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        if (addCollider)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
        }

        return go;
    }

    private static Sprite defaultSquareSprite;
    private static Sprite GetDefaultSquareSprite()
    {
        if (defaultSquareSprite == null)
        {
            // Busca o sprite padrão de quadrado do Unity ou cria uma textura sólida
            defaultSquareSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (defaultSquareSprite == null)
            {
                Texture2D tex = new Texture2D(32, 32);
                Color[] colors = new Color[32 * 32];
                for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
                tex.SetPixels(colors);
                tex.Apply();
                defaultSquareSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            }
        }
        return defaultSquareSprite;
    }
}
#endif
