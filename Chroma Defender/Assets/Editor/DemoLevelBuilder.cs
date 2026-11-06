#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Construtor automatizado de fases e zonas para Chroma Defender.
/// Permite construir a Zona 1 (Floresta Externa / Vilarejo) isolada com assets da Cainos,
/// ou o mapa completo com todas as 4 zonas.
/// </summary>
public static class DemoLevelBuilder
{
    private const string RootName = "--- DEMO LEVEL ---";

    // Layer de chão configurada no projeto (Ground = 6)
    private static int GroundLayer => LayerMask.NameToLayer("Ground") >= 0 ? LayerMask.NameToLayer("Ground") : 6;

    // Cache de sprites do Tileset de Chão da Cainos
    private static Dictionary<string, Sprite> groundSpritesCache;

    [MenuItem("Chroma Defender/1. Construir Zona 1 (Floresta Tranquila - Cainos Assets)")]
    public static void BuildZone1StandaloneMenu()
    {
        if (!EditorSceneManager.GetActiveScene().isLoaded || EditorSceneManager.GetActiveScene().name != "SampleScene")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }
        ClearExistingLevel();
        BuildZone1Only();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Chroma Defender/2. Construir Mapa Completo da Demo (Todas as Zonas)")]
    public static void BuildFullDemoLevelMenu()
    {
        if (!EditorSceneManager.GetActiveScene().isLoaded || EditorSceneManager.GetActiveScene().name != "SampleScene")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }
        ClearExistingLevel();
        BuildFullLevel();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Chroma Defender/3. Limpar Mapa da Demo")]
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

    // ------------------------------------------------------------------
    // CONSTRUÇÃO: Apenas Zona 1 (Foco Total em Acabamento e Tranquilidade)
    // ------------------------------------------------------------------
    public static void BuildZone1Only()
    {
        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Construir Zona 1 (Cainos)");

        // 1. Câmera e Jogador (Limites focados na Zona 1)
        SetupCameraAndPlayer(new Vector2(-5f, -4f), new Vector2(30f, 8f), new Vector3(-2.5f, 0.5f, 0f));

        // 2. Fundo Parallax Claro e Acolhedor
        Transform bgGroup = new GameObject("01_Background_Parallax").transform;
        bgGroup.SetParent(root.transform);
        BuildZone1Parallax(bgGroup);

        // 3. Terreno Real com Tileset Cainos (TX Tileset Ground)
        Transform terrainGroup = new GameObject("02_Terrain_And_Platforms").transform;
        terrainGroup.SetParent(root.transform);
        BuildZone1Terrain(terrainGroup);

        // 4. Objetos do Vilarejo, Decorações, Baú e Boneco de Treino
        Transform propsGroup = new GameObject("03_Village_Props_And_Decor").transform;
        propsGroup.SetParent(root.transform);
        BuildZone1Props(propsGroup, isStandalone: true);

        // 5. Inimigos da Zona 1 (Animados e balanceados)
        Transform enemiesGroup = new GameObject("04_Enemies_Zone1").transform;
        enemiesGroup.SetParent(root.transform);
        BuildZone1Enemies(enemiesGroup);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=green>[Chroma Defender]</color> ★ Zona 1 (Floresta Tranquila - Cainos) construída com sucesso! Pressione PLAY.");
    }

    // ------------------------------------------------------------------
    // CONSTRUÇÃO: Mapa Completo (Zona 1 Cainos + Zonas 2, 3 e 4)
    // ------------------------------------------------------------------
    public static void BuildFullLevel()
    {
        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Construir Mapa Completo");

        // 1. Câmera e Jogador (Limites globais)
        SetupCameraAndPlayer(new Vector2(-5f, -9f), new Vector2(105f, 15f), new Vector3(-2.5f, 0.5f, 0f));

        // 2. Fundo Parallax
        Transform bgGroup = new GameObject("01_Background_Parallax").transform;
        bgGroup.SetParent(root.transform);
        BuildZone1Parallax(bgGroup);
        // Fundo de pedra para o interior do castelo
        CreateFlatPlatform(bgGroup, "Castle_Backdrop_Stone", new Vector2(65f, 2f), new Vector2(65f, 14f), new Color(0.12f, 0.13f, 0.17f, 1f), -15, false);

        // 3. Terreno e Plataformas
        Transform terrainGroup = new GameObject("02_Terrain_And_Platforms").transform;
        terrainGroup.SetParent(root.transform);
        BuildZone1Terrain(terrainGroup);
        BuildCastleTerrain(terrainGroup);

        // 4. Objetos Interativos
        Transform propsGroup = new GameObject("03_Interactive_And_Props").transform;
        propsGroup.SetParent(root.transform);
        BuildZone1Props(propsGroup, isStandalone: false);
        BuildCastleInteractiveObjects(propsGroup);

        // 5. Inimigos
        Transform enemiesGroup = new GameObject("04_Enemies").transform;
        enemiesGroup.SetParent(root.transform);
        BuildZone1Enemies(enemiesGroup);
        BuildCastleEnemies(enemiesGroup);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=green>[Chroma Defender]</color> ★ Mapa Completo construído com sucesso! Pressione PLAY.");
    }

    // ------------------------------------------------------------------
    // 1. Câmera e Jogador
    // ------------------------------------------------------------------
    private static void SetupCameraAndPlayer(Vector2 minBounds, Vector2 maxBounds, Vector3 playerPos)
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow2D>();

            var so = new SerializedObject(follow);
            so.FindProperty("minBounds").vector2Value = minBounds;
            so.FindProperty("maxBounds").vector2Value = maxBounds;
            so.FindProperty("offset").vector3Value = new Vector3(0f, 1.2f, -10f);
            so.FindProperty("smoothTime").floatValue = 0.2f;
            so.ApplyModifiedProperties();
        }

        PlayerBehavior player = Object.FindFirstObjectByType<PlayerBehavior>();
        if (player != null)
        {
            player.transform.position = playerPos;
            player.gameObject.layer = LayerMask.NameToLayer("Default");
        }
    }

    // ------------------------------------------------------------------
    // 2. Fundo Parallax Claro e Tranquilo da Zona 1
    // ------------------------------------------------------------------
    private static void BuildZone1Parallax(Transform parent)
    {
        Sprite skySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FREE Parallax Forest Background HQ/Parallax Forest Background/01_Sky.png");
        Sprite forestFar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FREE Parallax Forest Background HQ/Parallax Forest Background/04_Forest.png");
        Sprite forestMid = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FREE Parallax Forest Background HQ/Parallax Forest Background/03_Forest.png");
        Sprite forestNear = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FREE Parallax Forest Background HQ/Parallax Forest Background/02_Forest.png");

        // Painéis de céu límpido (lado a lado para cobrir toda a zona)
        if (skySprite != null)
        {
            CreateBackdropQuad(parent, "Sky_01", skySprite, new Vector3(0f, 4.5f, 0f), new Vector3(1.4f, 1.3f, 1f), -50);
            CreateBackdropQuad(parent, "Sky_02", skySprite, new Vector3(22f, 4.5f, 0f), new Vector3(1.4f, 1.3f, 1f), -50);
        }

        // Camada distante suave
        if (forestFar != null)
        {
            CreateBackdropQuad(parent, "Forest_Far_01", forestFar, new Vector3(0f, 2.5f, 0f), new Vector3(1.3f, 1.2f, 1f), -40);
            CreateBackdropQuad(parent, "Forest_Far_02", forestFar, new Vector3(20f, 2.5f, 0f), new Vector3(1.3f, 1.2f, 1f), -40);
        }

        // Camada intermediária
        if (forestMid != null)
        {
            CreateBackdropQuad(parent, "Forest_Mid_01", forestMid, new Vector3(2f, 1.8f, 0f), new Vector3(1.25f, 1.15f, 1f), -30);
            CreateBackdropQuad(parent, "Forest_Mid_02", forestMid, new Vector3(22f, 1.8f, 0f), new Vector3(1.25f, 1.15f, 1f), -30);
        }

        // Camada próxima
        if (forestNear != null)
        {
            CreateBackdropQuad(parent, "Forest_Near_01", forestNear, new Vector3(4f, 1.0f, 0f), new Vector3(1.2f, 1.1f, 1f), -20);
            CreateBackdropQuad(parent, "Forest_Near_02", forestNear, new Vector3(24f, 1.0f, 0f), new Vector3(1.2f, 1.1f, 1f), -20);
        }
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
    // 3. Terreno Real Cainos (TX Tileset Ground) da Zona 1
    // ------------------------------------------------------------------
    private static void BuildZone1Terrain(Transform parent)
    {
        Transform groundParent = new GameObject("Zone1_Lush_Ground").transform;
        groundParent.SetParent(parent);
        groundParent.gameObject.layer = GroundLayer;

        int startX = -6;
        int endX = 28;

        // Monta visualmente os blocos 32x32 do tileset Cainos
        for (int x = startX; x <= endX; x++)
        {
            // Linha Y = -1 (Superfície de grama verde com flores/folhas)
            string topSprite = (x == startX) ? "TX Tileset Ground_0" :
                               (x == endX) ? "TX Tileset Ground_2" :
                               "TX Tileset Ground_1";
            CreateGroundTile(groundParent, topSprite, new Vector3(x, -1f, 0f), 2);

            // Linha Y = -2 (Subsolo de terra fértil)
            string midSprite = (x == startX) ? "TX Tileset Ground_3" :
                               (x == endX) ? "TX Tileset Ground_5" :
                               "TX Tileset Ground_4";
            CreateGroundTile(groundParent, midSprite, new Vector3(x, -2f, 0f), 2);

            // Linha Y = -3 (Fundo rochoso de terra)
            string botSprite = (x == startX) ? "TX Tileset Ground_6" :
                               (x == endX) ? "TX Tileset Ground_8" :
                               "TX Tileset Ground_7";
            CreateGroundTile(groundParent, botSprite, new Vector3(x, -3f, 0f), 2);

            // Linha Y = -4 (Fundação profunda para evitar vácuo)
            CreateGroundTile(groundParent, "TX Tileset Ground_4", new Vector3(x, -4f, 0f), 2);
        }

        // Colisor contínuo e perfeito para o chão (sem quinas travando o cavaleiro)
        GameObject groundCollider = new GameObject("Ground_Surface_Collider");
        groundCollider.transform.SetParent(groundParent);
        groundCollider.layer = GroundLayer;

        float width = (endX - startX + 1);
        float centerX = (startX + endX) * 0.5f;

        var box = groundCollider.AddComponent<BoxCollider2D>();
        // Topo da caixa fica em Y = -0.5f (exatamente onde a grama do sprite termina)
        box.offset = new Vector2(centerX, -2.5f);
        box.size = new Vector2(width, 4.0f);

        // Parede de limite esquerdo do mapa
        GameObject leftWall = new GameObject("Left_Boundary_Wall");
        leftWall.transform.SetParent(parent);
        leftWall.layer = GroundLayer;
        var leftBox = leftWall.AddComponent<BoxCollider2D>();
        leftBox.offset = new Vector2(-6.6f, 3.0f);
        leftBox.size = new Vector2(1.2f, 10f);

        // --- Plataformas Elevadas da Cainos (Saltos do Tutorial) ---
        // Plataforma 1 (X = 6.0, Y = 1.3): Plataforma modular de madeira
        InstantiateCainosPrefab(parent, "PF Village Props - Platform 02 X3.prefab", new Vector3(6.0f, 1.3f, 0f), GroundLayer);
        // Coletável decorativo em cima da plataforma
        InstantiateCainosPrefab(parent, "PF Village Props - Apple 01.prefab", new Vector3(6.5f, 1.8f, 0f));

        // Plataforma 2 (X = 12.0, Y = 2.2): Ponte suspensa rústica
        InstantiateCainosPrefab(parent, "PF Village Props - Wooden Bridge X6.prefab", new Vector3(12.0f, 2.2f, 0f), GroundLayer);
        InstantiateCainosPrefab(parent, "PF Village Props - Flower 01.prefab", new Vector3(13.2f, 2.6f, 0f));
    }

    private static Sprite GetGroundSprite(string spriteName)
    {
        if (groundSpritesCache == null)
        {
            groundSpritesCache = new Dictionary<string, Sprite>();
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Tileset Ground.png");
            foreach (var obj in assets)
            {
                if (obj is Sprite sp) groundSpritesCache[sp.name] = sp;
            }
        }
        groundSpritesCache.TryGetValue(spriteName, out Sprite result);
        return result;
    }

    private static GameObject CreateGroundTile(Transform parent, string spriteName, Vector3 pos, int sortingOrder)
    {
        Sprite sp = GetGroundSprite(spriteName);
        if (sp == null) return null;

        GameObject tile = new GameObject(spriteName);
        tile.transform.SetParent(parent);
        tile.transform.position = pos;

        var sr = tile.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = sortingOrder;
        return tile;
    }

    // ------------------------------------------------------------------
    // 4. Decorações, Cenário de Vilarejo, Baú e Boneco de Treino
    // ------------------------------------------------------------------
    private static void BuildZone1Props(Transform parent, bool isStandalone)
    {
        const float groundY = -0.5f;

        // 1. Árvores exuberantes da Cainos
        InstantiateCainosPrefab(parent, "PF Village Props - Tree 01.prefab", new Vector3(-4.0f, groundY, 0f), scale: 1.15f, sortingOrder: -5);
        InstantiateCainosPrefab(parent, "PF Village Props - Tree 02.prefab", new Vector3(1.0f, groundY, 0f), scale: 0.95f, sortingOrder: -5);
        InstantiateCainosPrefab(parent, "PF Village Props - Tree 01.prefab", new Vector3(4.8f, groundY, 0f), scale: 1.05f, sortingOrder: -5);
        InstantiateCainosPrefab(parent, "PF Village Props - Tree 02.prefab", new Vector3(16.0f, groundY, 0f), scale: 1.1f, sortingOrder: -5);
        InstantiateCainosPrefab(parent, "PF Village Props - Tree 01.prefab", new Vector3(20.8f, groundY, 0f), scale: 1.0f, sortingOrder: -5);
        InstantiateCainosPrefab(parent, "PF Village Props - Tree 02.prefab", new Vector3(26.5f, groundY, 0f), scale: 1.05f, sortingOrder: -5);

        // 2. Cercas rústicas de madeira
        InstantiateCainosPrefab(parent, "PF Village Props - Fence A.prefab", new Vector3(-3.5f, groundY, 0f), sortingOrder: -2);
        InstantiateCainosPrefab(parent, "PF Village Props - Fence B.prefab", new Vector3(-1.8f, groundY, 0f), sortingOrder: -2);
        InstantiateCainosPrefab(parent, "PF Village Props - Fence A.prefab", new Vector3(18.2f, groundY, 0f), sortingOrder: -2);
        InstantiateCainosPrefab(parent, "PF Village Props - Fence B.prefab", new Vector3(19.9f, groundY, 0f), sortingOrder: -2);

        // 3. Girassóis e Flores (Dão a atmosfera clara, vibrante e tranquila)
        InstantiateCainosPrefab(parent, "PF Village Props - Sunflower 01.prefab", new Vector3(2.5f, groundY, 0f), sortingOrder: 4);
        InstantiateCainosPrefab(parent, "PF Village Props - Sunflower 02.prefab", new Vector3(7.8f, groundY, 0f), sortingOrder: 4);
        InstantiateCainosPrefab(parent, "PF Village Props - Sunflower 03.prefab", new Vector3(15.2f, groundY, 0f), sortingOrder: 4);
        InstantiateCainosPrefab(parent, "PF Village Props - Sunflower 01.prefab", new Vector3(21.5f, groundY, 0f), sortingOrder: 4);
        InstantiateCainosPrefab(parent, "PF Village Props - Sunflower 02.prefab", new Vector3(25.2f, groundY, 0f), sortingOrder: 4);

        InstantiateCainosPrefab(parent, "PF Village Props - Flower 01.prefab", new Vector3(-2.0f, groundY, 0f), sortingOrder: 4);
        InstantiateCainosPrefab(parent, "PF Village Props - Flower 02.prefab", new Vector3(3.2f, groundY, 0f), sortingOrder: 4);
        InstantiateCainosPrefab(parent, "PF Village Props - Flower 01.prefab", new Vector3(10.2f, groundY, 0f), sortingOrder: 4);
        InstantiateCainosPrefab(parent, "PF Village Props - Flower 02.prefab", new Vector3(17.5f, groundY, 0f), sortingOrder: 4);
        InstantiateCainosPrefab(parent, "PF Village Props - Flower 01.prefab", new Vector3(22.8f, groundY, 0f), sortingOrder: 4);

        // 4. Arbustos e Tufos de Grama
        InstantiateCainosPrefab(parent, "PF Village Props - Bush 01.prefab", new Vector3(-3.8f, groundY, 0f), sortingOrder: -1);
        InstantiateCainosPrefab(parent, "PF Village Props - Bush 02.prefab", new Vector3(4.2f, groundY, 0f), sortingOrder: -1);
        InstantiateCainosPrefab(parent, "PF Village Props - Bush 03.prefab", new Vector3(13.0f, groundY, 0f), sortingOrder: -1);
        InstantiateCainosPrefab(parent, "PF Village Props - Bush 01.prefab", new Vector3(20.2f, groundY, 0f), sortingOrder: -1);

        float[] grassPositions = { -3f, -1.2f, 0.8f, 2.2f, 5.5f, 7.0f, 9.0f, 12.0f, 14.5f, 18.0f, 23.5f, 27.0f };
        for (int i = 0; i < grassPositions.Length; i++)
        {
            string grassName = $"PF Village Props - Grass 0{(i % 6) + 1}.prefab";
            InstantiateCainosPrefab(parent, grassName, new Vector3(grassPositions[i], groundY, 0f), sortingOrder: 5);
        }

        // 5. Elementos de Vilarejo Rústico
        // Placa inicial apontando para a direita (➔ Aventura)
        InstantiateCainosPrefab(parent, "PF Village Props - Road Sign 01.prefab", new Vector3(-2.8f, groundY, 0f), sortingOrder: 3);

        // Pilha de lenha e toco
        InstantiateCainosPrefab(parent, "PF Village Props - Wood Logs.prefab", new Vector3(-0.2f, groundY, 0f), sortingOrder: 3);
        InstantiateCainosPrefab(parent, "PF Village Props - Stump.prefab", new Vector3(-0.9f, groundY, 0f), sortingOrder: 3);

        // Poço de pedra, barris e caixotes
        InstantiateCainosPrefab(parent, "PF Village Props - Well.prefab", new Vector3(13.5f, groundY, 0f), sortingOrder: 0);
        InstantiateCainosPrefab(parent, "PF Village Props - Barrel.prefab", new Vector3(14.8f, groundY, 0f), sortingOrder: 3);
        InstantiateCainosPrefab(parent, "PF Village Props - Crate Small.prefab", new Vector3(15.4f, groundY, 0f), sortingOrder: 3);
        InstantiateCainosPrefab(parent, "PF Village Props - Crate Large.prefab", new Vector3(24.2f, groundY, 0f), sortingOrder: 3);

        // Postes de iluminação de estrada
        InstantiateCainosPrefab(parent, "PF Village Props - Road Lamp.prefab", new Vector3(8.2f, groundY, 0f), sortingOrder: 3);
        InstantiateCainosPrefab(parent, "PF Village Props - Road Lamp.prefab", new Vector3(25.5f, groundY, 0f), sortingOrder: 3);

        // Rochedos de contenção à esquerda
        InstantiateCainosPrefab(parent, "PF Village Props - Rock 01.prefab", new Vector3(-5.8f, groundY, 0f), sortingOrder: 3);
        InstantiateCainosPrefab(parent, "PF Village Props - Rock 02.prefab", new Vector3(-5.2f, groundY, 0f), sortingOrder: 3);

        // Placa no final da Zona 1
        InstantiateCainosPrefab(parent, "PF Village Props - Road Sign 02.prefab", new Vector3(27.5f, groundY, 0f), sortingOrder: 3);

        // 6. Boneco de Treino Cainos (Target Dummy com TrainingDummy.cs)
        GameObject dummy = InstantiateCainosPrefab(parent, "PF Village Props - Training Dummy.prefab", new Vector3(-0.8f, groundY, 0f), sortingOrder: 3);
        if (dummy != null)
        {
            var boxCol = dummy.GetComponent<BoxCollider2D>();
            if (boxCol == null) boxCol = dummy.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(0.8f, 1.4f);
            boxCol.offset = new Vector2(0f, 0.7f);

            var rb = dummy.GetComponent<Rigidbody2D>();
            if (rb == null) rb = dummy.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.linearDamping = 14f;

            if (dummy.GetComponent<TrainingDummy>() == null)
            {
                dummy.AddComponent<TrainingDummy>();
            }
        }

        // 7. Baú Inicial de Madeira (Desbloqueio do Ataque Pesado)
        GameObject chest = InstantiateCainosPrefab(parent, "PF Village Props - Chest Wooden.prefab", new Vector3(16.5f, groundY, 0f), sortingOrder: 3);
        if (chest != null)
        {
            var boxCol = chest.GetComponent<BoxCollider2D>();
            if (boxCol == null) boxCol = chest.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(1.2f, 1.0f);
            boxCol.offset = new Vector2(0f, 0.5f);
            boxCol.isTrigger = false;

            var interactable = chest.GetComponent<ChestInteractable>();
            if (interactable == null) interactable = chest.AddComponent<ChestInteractable>();

            var so = new SerializedObject(interactable);
            so.FindProperty("rewardType").enumValueIndex = (int)ChestRewardType.UnlockHeavyAttack;
            so.ApplyModifiedProperties();
        }

        // 8. Se for teste isolado da Zona 1, coloca uma barricada no final (X = 28.5)
        if (isStandalone)
        {
            GameObject barricade = InstantiateCainosPrefab(parent, "PF Village Props - Barricade.prefab", new Vector3(28.5f, groundY, 0f), sortingOrder: 3);
            if (barricade != null)
            {
                var barCol = barricade.GetComponent<BoxCollider2D>();
                if (barCol == null) barCol = barricade.AddComponent<BoxCollider2D>();
                barCol.size = new Vector2(1.5f, 2.0f);
                barCol.offset = new Vector2(0f, 1.0f);
            }
        }
    }

    // ------------------------------------------------------------------
    // 5. Inimigos Vivos e Animados da Zona 1
    // ------------------------------------------------------------------
    private static void BuildZone1Enemies(Transform parent)
    {
        // 1. Slime Neutro (Slime saltitante e amigável para aprender o combo leve)
        CreateAnimatedEnemy(parent, "Enemy_Slime_Neutral", new Vector3(8.8f, -0.4f, 0f),
            EnemyPolarityType.Neutral, 50, 2.0f, false);

        // 2. Rato Neutro (Rápido e rasteiro para aprender o rolamento / esquiva)
        CreateAnimatedEnemy(parent, "Enemy_Rat_Neutral", new Vector3(23.0f, -0.4f, 0f),
            EnemyPolarityType.Neutral, 40, 2.5f, true);
    }

    private static void CreateAnimatedEnemy(Transform parent, string name, Vector3 pos, EnemyPolarityType polarity, int health, float patrolDist, bool isRat)
    {
        GameObject enemy = new GameObject(name);
        enemy.transform.SetParent(parent);
        enemy.transform.position = pos;
        enemy.transform.localScale = new Vector3(2f, 2f, 1f);

        var sr = enemy.AddComponent<SpriteRenderer>();
        string spritePath = isRat ?
            "Assets/Monsters Creatures Fantasy 2/Sprites/Rat/idle.png" :
            "Assets/Monsters Creatures Fantasy 2/Sprites/Slime/idle.png";

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath) as Sprite[];
        if (sprites != null && sprites.Length > 0)
        {
            sr.sprite = sprites[0];
        }
        else
        {
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        sr.sortingOrder = 4;

        // Animação viva de Idle/Salto
        var anim = enemy.AddComponent<Animator>();
        string controllerPath = isRat ?
            "Assets/Monsters Creatures Fantasy 2/Animations/idle_0.controller" :
            "Assets/Monsters Creatures Fantasy 2/Animations/idle_0 1.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (ctrl != null) anim.runtimeAnimatorController = ctrl;

        var rb = enemy.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.7f);
        col.offset = new Vector2(0f, 0.35f);

        var comp = enemy.AddComponent<ElementalEnemy>();
        var so = new SerializedObject(comp);
        so.FindProperty("polarity").enumValueIndex = (int)polarity;
        so.FindProperty("maxHealth").intValue = health;
        so.FindProperty("patrolDistance").floatValue = patrolDist;
        so.FindProperty("moveSpeed").floatValue = isRat ? 1.8f : 1.2f;
        so.ApplyModifiedProperties();
    }

    // ------------------------------------------------------------------
    // Prefab Loader Auxiliar da Cainos
    // ------------------------------------------------------------------
    private static GameObject InstantiateCainosPrefab(Transform parent, string prefabName, Vector3 pos, int layer = -1, float scale = 1f, int sortingOrder = int.MinValue)
    {
        string fullPath = $"Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/{prefabName}";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
        if (prefab == null) return null;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instance == null) return null;

        instance.transform.SetParent(parent);
        instance.transform.position = pos;
        if (scale != 1f) instance.transform.localScale = Vector3.one * scale;

        if (layer >= 0)
        {
            SetLayerRecursively(instance, layer);
        }

        if (sortingOrder != int.MinValue)
        {
            foreach (var sr in instance.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.sortingOrder = sortingOrder;
            }
        }

        return instance;
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // ------------------------------------------------------------------
    // MÉTODOS DE APOIO PARA O MAPA COMPLETO (ZONAS 2, 3 E 4)
    // ------------------------------------------------------------------
    private static void BuildCastleTerrain(Transform parent)
    {
        Color stoneColor = new Color(0.35f, 0.37f, 0.42f);
        Color dungeonColor = new Color(0.22f, 0.24f, 0.28f);
        Color woodColor = new Color(0.55f, 0.42f, 0.26f);

        // Zona 2: Muralha e Portão
        CreateFlatPlatform(parent, "Castle_Rampart_Wall", new Vector2(28f, 5f), new Vector2(2.5f, 10f), stoneColor);
        CreateFlatPlatform(parent, "Castle_Gate_Floor", new Vector2(33f, -1f), new Vector2(10f, 2f), stoneColor);
        CreateFlatPlatform(parent, "Castle_Gate_Arch", new Vector2(33f, 4.5f), new Vector2(10f, 2f), stoneColor);

        // Zona 3: Salão Principal
        CreateFlatPlatform(parent, "Hall_Ground", new Vector2(44f, -1f), new Vector2(14f, 2f), stoneColor);
        CreateFlatPlatform(parent, "Castle_Ceiling", new Vector2(65f, 7.5f), new Vector2(65f, 2f), stoneColor);

        CreateFlatPlatform(parent, "Stair_Step_1", new Vector2(43f, 1.3f), new Vector2(2.8f, 0.5f), woodColor);
        CreateFlatPlatform(parent, "Stair_Step_2", new Vector2(40f, 3.0f), new Vector2(3.0f, 0.5f), woodColor);

        // Caminho Falso
        CreateFlatPlatform(parent, "FakePath_Floor", new Vector2(37f, 4.8f), new Vector2(11f, 0.8f), stoneColor);
        CreateFlatPlatform(parent, "FakePath_DeadEnd_Wall", new Vector2(31f, 6.5f), new Vector2(1.5f, 4f), stoneColor);

        // Caminho Verdadeiro
        CreateFlatPlatform(parent, "Bridge_Access", new Vector2(56f, 1.4f), new Vector2(3.5f, 0.5f), woodColor);
        CreateFlatPlatform(parent, "Bridge_Main", new Vector2(62f, 2.2f), new Vector2(9f, 0.7f), stoneColor);

        CreateFlatPlatform(parent, "Key_Pedestal_Ledge", new Vector2(74f, 2.2f), new Vector2(5f, 0.7f), stoneColor);
        CreateFlatPlatform(parent, "Key_Stone_Pedestal", new Vector2(74f, 2.8f), new Vector2(1f, 0.6f), new Color(0.7f, 0.65f, 0.55f));

        // Fosso
        CreateFlatPlatform(parent, "Pit_Floor", new Vector2(69f, -4.5f), new Vector2(19f, 1.8f), dungeonColor);
        CreateFlatPlatform(parent, "Pit_Wall_Left", new Vector2(59f, -1.2f), new Vector2(1.8f, 6f), dungeonColor);
        CreateFlatPlatform(parent, "Pit_Wall_Right", new Vector2(79f, -1.2f), new Vector2(1.8f, 6f), dungeonColor);

        CreateFlatPlatform(parent, "Climb_Step_1", new Vector2(62f, -2.8f), new Vector2(2.5f, 0.5f), woodColor);
        CreateFlatPlatform(parent, "Climb_Step_2", new Vector2(66.5f, -1.3f), new Vector2(2.5f, 0.5f), woodColor);
        CreateFlatPlatform(parent, "Climb_Step_3", new Vector2(62.5f, 0.3f), new Vector2(2.5f, 0.5f), woodColor);
        CreateFlatPlatform(parent, "Climb_Step_4", new Vector2(67f, 1.6f), new Vector2(2.5f, 0.5f), woodColor);

        // Zona 4: Câmara Secreta
        CreateFlatPlatform(parent, "Secret_Chamber_Floor", new Vector2(71f, -1f), new Vector2(40f, 2f), new Color(0.25f, 0.28f, 0.35f));
        CreateFlatPlatform(parent, "Secret_Chamber_EndWall", new Vector2(92f, 3.2f), new Vector2(2f, 10f), stoneColor);
        CreateFlatPlatform(parent, "Door_Header_Wall", new Vector2(50f, 5.0f), new Vector2(1.6f, 4.5f), stoneColor);
    }

    private static void BuildCastleInteractiveObjects(Transform parent)
    {
        // Portão Elemental (Gelo exige FOGO)
        GameObject barrier = new GameObject("Ice_Crystal_Barrier");
        barrier.transform.SetParent(parent);
        barrier.transform.position = new Vector3(36f, 0.6f, 0f);
        var srB = barrier.AddComponent<SpriteRenderer>();
        srB.sprite = GetDefaultSquareSprite();
        srB.size = new Vector2(1.2f, 2.8f);
        srB.sortingOrder = 5;
        srB.color = new Color(0.3f, 0.8f, 1f, 0.85f);
        var boxB = barrier.AddComponent<BoxCollider2D>();
        boxB.size = new Vector2(1.2f, 2.8f);
        var trig = barrier.AddComponent<PolarityTrigger>();
        var soTrig = new SerializedObject(trig);
        soTrig.FindProperty("requiredPolarity").enumValueIndex = (int)ElementPolarity.Fire;
        soTrig.ApplyModifiedProperties();

        // Grande Porta Trancada
        GameObject door = new GameObject("Castle_Grand_Locked_Door");
        door.transform.SetParent(parent);
        door.transform.position = new Vector3(50f, 0.7f, 0f);
        var srD = door.AddComponent<SpriteRenderer>();
        srD.sprite = GetDefaultSquareSprite();
        srD.size = new Vector2(1.4f, 3.2f);
        srD.color = new Color(0.45f, 0.35f, 0.25f);
        srD.sortingOrder = 4;
        var boxD = door.AddComponent<BoxCollider2D>();
        boxD.size = new Vector2(1.4f, 3.2f);
        door.AddComponent<LockedDoor>();

        // Baú do Caminho Falso (Cura Total)
        GameObject fakeChest = InstantiateCainosPrefab(parent, "PF Village Props - Chest Wooden.prefab", new Vector3(35f, 5.4f, 0f));
        if (fakeChest != null)
        {
            var ci = fakeChest.GetComponent<ChestInteractable>() ?? fakeChest.AddComponent<ChestInteractable>();
            var so = new SerializedObject(ci);
            so.FindProperty("rewardType").enumValueIndex = (int)ChestRewardType.FullHeal;
            so.ApplyModifiedProperties();
        }

        // Chão Falso diante da chave
        GameObject trap = new GameObject("Key_Trap_CrumblingFloor");
        trap.transform.SetParent(parent);
        trap.transform.position = new Vector3(69f, 2.2f, 0f);
        trap.layer = GroundLayer;
        var srT = trap.AddComponent<SpriteRenderer>();
        srT.sprite = GetDefaultSquareSprite();
        srT.drawMode = SpriteDrawMode.Sliced;
        srT.size = new Vector2(4.5f, 0.7f);
        srT.color = new Color(0.5f, 0.46f, 0.42f);
        srT.sortingOrder = 3;
        var boxT = trap.AddComponent<BoxCollider2D>();
        boxT.size = new Vector2(4.5f, 0.7f);
        trap.AddComponent<CrumblingPlatform>();

        // Chave Dourada
        GameObject key = new GameObject("Castle_Golden_Key");
        key.transform.SetParent(parent);
        key.transform.position = new Vector3(74f, 3.5f, 0f);
        key.transform.localScale = Vector3.one * 1.5f;
        var srK = key.AddComponent<SpriteRenderer>();
        srK.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/karsiori/Pixel Art Key Pack - V2/Key 1 - Gold.png") ?? GetDefaultSquareSprite();
        srK.sortingOrder = 8;
        srK.color = new Color(1f, 0.88f, 0.25f, 1f);
        var colK = key.AddComponent<CircleCollider2D>();
        colK.radius = 0.5f;
        colK.isTrigger = true;
        key.AddComponent<KeyPickup>();

        // Grande Baú Dourado (Câmara Secreta)
        GameObject goldenChest = InstantiateCainosPrefab(parent, "PF Village Props - Chest Golden.prefab", new Vector3(84f, 0.2f, 0f));
        if (goldenChest != null)
        {
            var ci = goldenChest.GetComponent<ChestInteractable>() ?? goldenChest.AddComponent<ChestInteractable>();
            var so = new SerializedObject(ci);
            so.FindProperty("rewardType").enumValueIndex = (int)ChestRewardType.SecretDemoTreasure;
            so.ApplyModifiedProperties();
        }
    }

    private static void BuildCastleEnemies(Transform parent)
    {
        CreateAnimatedEnemy(parent, "Enemy_Ice_Slime_Gate", new Vector3(32.5f, 0.2f, 0f), EnemyPolarityType.Ice, 65, 2.5f, false);
        CreateAnimatedEnemy(parent, "FakePath_Fire_Rat", new Vector3(35f, 5.6f, 0f), EnemyPolarityType.Fire, 55, 2.5f, true);
        CreateAnimatedEnemy(parent, "FakePath_Ice_Slime", new Vector3(40f, 5.6f, 0f), EnemyPolarityType.Ice, 65, 2.5f, false);
        CreateAnimatedEnemy(parent, "Pit_Ice_Rat", new Vector3(64f, -3.3f, 0f), EnemyPolarityType.Ice, 55, 3f, true);
        CreateAnimatedEnemy(parent, "Pit_Fire_Slime", new Vector3(74f, -3.3f, 0f), EnemyPolarityType.Fire, 65, 3f, false);
        CreateAnimatedEnemy(parent, "SecretChamber_Fire_Guardian", new Vector3(64f, 0.2f, 0f), EnemyPolarityType.Fire, 80, 3f, true);
        CreateAnimatedEnemy(parent, "SecretChamber_Ice_Guardian", new Vector3(76f, 0.2f, 0f), EnemyPolarityType.Ice, 80, 3f, false);
    }

    private static GameObject CreateFlatPlatform(Transform parent, string name, Vector2 pos, Vector2 size, Color color, int sortingOrder = 1, bool addCollider = true)
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
