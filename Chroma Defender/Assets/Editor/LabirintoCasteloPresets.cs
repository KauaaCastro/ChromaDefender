#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// LABIRINTO DO CASTELO — presets (prefabs) e ferramentas de editor.
///
/// Menu: Chroma Defender → Labirinto do Castelo
///   1. Criar ou Atualizar Presets        → gera os prefabs em Assets/Prefabs/LabirintoCastelo
///   Inserir → (Porta Comum, Porta do Início, Porta Selada, Porta da Alavanca, Porta Trancada,
///              Porta Traiçoeira, Placa Seta → Primeira Sala, Placa de Dica, Alavanca, Sala,
///              Início do Labirinto, Chave de Ferro)
///   Ligar portas selecionadas (ida e volta)
///   Ligar porta → porta ativa (só ida)
///
/// Dica: na Scene, linhas VERDES = portas de ida e volta, LARANJA = só ida, ROXO = Porta Traiçoeira.
/// </summary>
public static class LabirintoCasteloPresets
{
    public const string Dir = "Assets/Prefabs/LabirintoCastelo";

    public const string PortaComum = "Porta_Labirinto";
    public const string PortaInicio = "Porta_Inicio";
    public const string PortaSelada = "Porta_Selada";
    public const string PortaAlavanca = "Porta_Alavanca";
    public const string PortaTrancada = "Porta_Trancada_Chave";
    public const string PortaTraicoeira = "Porta_Traicoeira";
    public const string PlacaSeta = "Placa_Seta_PrimeiraSala";
    public const string PlacaDica = "Placa_Dica";
    public const string Alavanca = "Alavanca";
    public const string Sala = "Sala_Labirinto";
    public const string Inicio = "Inicio_Labirinto";
    public const string Chave = "Chave_Ferro_Labirinto";
    public const string Gerenciador = "Labirinto_Gerenciador";

    private const string MedievalProps = "Assets/Medieval_pixel_art_asset_FREE/Textures/Medieval_props_free.png";
    private const string CainosProps = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png";
    private const string PadlockDir = "Assets/karsiori/Pixel Art Padlock Pack - Animated/Sprites/Old Padlock/GREY/Sprites";
    private const string KeyRust = "Assets/karsiori/Pixel Art Key Pack - V2/Key 2 - Rust.png";

    private const float DoorScale = 2.6f;

    // =====================================================================
    // MENUS
    // =====================================================================

    [MenuItem("Chroma Defender/Labirinto do Castelo/1. Criar ou Atualizar Presets", priority = 1)]
    public static void CreateAllMenu()
    {
        CreateAll();
        Debug.Log($"<color=lime>[Labirinto do Castelo]</color> Presets criados em {Dir}. Arraste-os para a cena ou use o menu Inserir.");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(Dir);
    }

    [MenuItem("Chroma Defender/Labirinto do Castelo/Inserir/Porta Comum", priority = 20)] public static void I1() => Insert(PortaComum);
    [MenuItem("Chroma Defender/Labirinto do Castelo/Inserir/Porta do Início (sempre volta ao início)", priority = 21)] public static void I2() => Insert(PortaInicio);
    [MenuItem("Chroma Defender/Labirinto do Castelo/Inserir/Porta Selada (abre ao limpar a sala)", priority = 22)] public static void I3() => Insert(PortaSelada);
    [MenuItem("Chroma Defender/Labirinto do Castelo/Inserir/Porta da Alavanca", priority = 23)] public static void I4() => Insert(PortaAlavanca);
    [MenuItem("Chroma Defender/Labirinto do Castelo/Inserir/Porta Trancada (Chave de Ferro)", priority = 24)] public static void I5() => Insert(PortaTrancada);
    [MenuItem("Chroma Defender/Labirinto do Castelo/Inserir/Porta Traiçoeira (destino aleatório)", priority = 25)] public static void I6() => Insert(PortaTraicoeira);
    [MenuItem("Chroma Defender/Labirinto do Castelo/Inserir/Placa Seta → Primeira Sala", priority = 40)] public static void I7() => Insert(PlacaSeta);
    [MenuItem("Chroma Defender/Labirinto do Castelo/Inserir/Placa de Dica", priority = 41)] public static void I8() => Insert(PlacaDica);
    [MenuItem("Chroma Defender/Labirinto do Castelo/Inserir/Alavanca", priority = 42)] public static void I9() => Insert(Alavanca);
    [MenuItem("Chroma Defender/Labirinto do Castelo/Inserir/Chave de Ferro", priority = 43)] public static void I10() => Insert(Chave, 1f);
    [MenuItem("Chroma Defender/Labirinto do Castelo/Inserir/Sala (área do labirinto)", priority = 60)] public static void I11() => Insert(Sala, 5f);
    [MenuItem("Chroma Defender/Labirinto do Castelo/Inserir/Início do Labirinto", priority = 61)] public static void I12() => Insert(Inicio);

    [MenuItem("Chroma Defender/Labirinto do Castelo/Ligar portas selecionadas (ida e volta)", priority = 80)]
    public static void LinkTwoWay()
    {
        var doors = Selection.gameObjects.Select(g => g.GetComponentInParent<CastleDoor>()).Where(d => d != null).Distinct().ToArray();
        if (doors.Length != 2)
        {
            EditorUtility.DisplayDialog("Labirinto do Castelo", "Selecione exatamente DUAS portas (CastleDoor) na Hierarchy ou na Scene.", "OK");
            return;
        }
        Link(doors[0], doors[1]);
        Link(doors[1], doors[0]);
        Debug.Log($"<color=lime>[Labirinto do Castelo]</color> {doors[0].name} ⇄ {doors[1].name}");
    }

    [MenuItem("Chroma Defender/Labirinto do Castelo/Ligar porta → porta ativa (só ida)", priority = 81)]
    public static void LinkOneWay()
    {
        var active = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<CastleDoor>() : null;
        var others = Selection.gameObjects.Select(g => g.GetComponentInParent<CastleDoor>()).Where(d => d != null && d != active).Distinct().ToArray();
        if (active == null || others.Length != 1)
        {
            EditorUtility.DisplayDialog("Labirinto do Castelo",
                "Selecione DUAS portas: primeiro a ORIGEM e por último (Ctrl+clique) o DESTINO.\nA origem passa a levar ao destino; o destino não muda.", "OK");
            return;
        }
        Link(others[0], active);
        Debug.Log($"<color=lime>[Labirinto do Castelo]</color> {others[0].name} → {active.name} (só ida)");
    }

    [MenuItem("Chroma Defender/Labirinto do Castelo/Ligar portas selecionadas (ida e volta)", true)]
    [MenuItem("Chroma Defender/Labirinto do Castelo/Ligar porta → porta ativa (só ida)", true)]
    private static bool ValidateLink() => Selection.gameObjects.Length >= 2;

    public static void Link(CastleDoor from, CastleDoor to)
    {
        var so = new SerializedObject(from);
        Set(so, "destination", (int)CastleDoor.Destination.PortaLigada);
        Set(so, "linkedDoor", to);
        so.ApplyModifiedProperties();
    }

    // =====================================================================
    // PRESETS
    // =====================================================================

    public static void CreateAll()
    {
        if (!Directory.Exists(Dir)) { Directory.CreateDirectory(Dir); AssetDatabase.Refresh(); }

        Save(BuildDoor(PortaComum, DoorKind.Comum));
        Save(BuildDoor(PortaInicio, DoorKind.Inicio));
        Save(BuildDoor(PortaSelada, DoorKind.Selada));
        Save(BuildDoor(PortaAlavanca, DoorKind.Alavanca));
        Save(BuildDoor(PortaTrancada, DoorKind.Trancada));
        Save(BuildDoor(PortaTraicoeira, DoorKind.Traicoeira));
        Save(BuildArrowSign());
        Save(BuildHintSign());
        Save(BuildLever());
        Save(BuildRoom());
        Save(BuildStart());
        Save(BuildKey());
        Save(BuildManager());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static GameObject Load(string name)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{name}.prefab");
        if (p == null) { CreateAll(); p = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{name}.prefab"); }
        return p;
    }

    public enum DoorKind { Comum, Inicio, Selada, Alavanca, Trancada, Traicoeira }

    private static GameObject BuildDoor(string name, DoorKind kind)
    {
        Sprite[] props = Props();
        Sprite closed = P(props, 1), ajar = P(props, 2), bars = P(props, 3), arch = P(props, 4);
        float h = arch != null ? arch.bounds.size.y * DoorScale : 3f;

        var go = new GameObject(name);
        var vis = Child(go.transform, "Visual", kind == DoorKind.Inicio ? arch : closed, new Vector3(0f, h * 0.5f, 0f), DoorScale, 4);
        var door = go.AddComponent<CastleDoor>();
        var so = new SerializedObject(door);
        Set(so, "doorRenderer", vis.GetComponent<SpriteRenderer>());
        Set(so, "closedSprite", closed);
        Set(so, "ajarSprite", ajar);
        Set(so, "openSprite", arch);
        Set(so, "lockedSprite", bars);
        Set(so, "labelHeight", h + 0.45f);

        switch (kind)
        {
            case DoorKind.Inicio:
                // Arco sempre aberto, com brilho azul pulsando: dá para reconhecer de longe.
                Set(so, "doorTitle", "Porta do Início");
                Set(so, "destination", (int)CastleDoor.Destination.InicioDoLabirinto);
                Set(so, "closedSprite", arch);
                Set(so, "ajarSprite", null);
                vis.GetComponent<SpriteRenderer>().color = new Color(0.75f, 0.92f, 1f);
                var glow = Child(go.transform, "Brilho", arch, new Vector3(0f, h * 0.5f, 0f), DoorScale * 1.12f, 3);
                Set(so, "glowRenderer", glow.GetComponent<SpriteRenderer>());
                Set(so, "glowColor", new Color(0.35f, 0.85f, 1f, 0.55f));
                break;

            case DoorKind.Selada:
                Set(so, "doorTitle", "Porta Selada");
                Set(so, "lockType", (int)CastleDoor.LockType.SalaLimpa);
                vis.GetComponent<SpriteRenderer>().sprite = bars;
                break;

            case DoorKind.Alavanca:
                Set(so, "doorTitle", "Grade de Ferro");
                Set(so, "lockType", (int)CastleDoor.LockType.Alavanca);
                Set(so, "requiredFlag", "alavanca_labirinto_1");
                vis.GetComponent<SpriteRenderer>().sprite = bars;
                break;

            case DoorKind.Trancada:
                Set(so, "doorTitle", "Porta Trancada");
                Set(so, "lockType", (int)CastleDoor.LockType.Chave);
                Set(so, "requiredKeyId", GameProgress.KeyLabirinto);
                var frames = FolderSprites(PadlockDir);
                var pad = Child(go.transform, "Cadeado", frames.FirstOrDefault(), new Vector3(0f, h * 0.45f, 0f), 0.8f, 8);
                Set(so, "padlockRenderer", pad.GetComponent<SpriteRenderer>());
                SetArray(so, "padlockFrames", frames);
                break;

            case DoorKind.Traicoeira:
                // Igual às outras, só um pouco arroxeada — o jogador atento percebe.
                Set(so, "doorTitle", "Porta Misteriosa");
                Set(so, "destination", (int)CastleDoor.Destination.Aleatorio);
                Set(so, "rememberDestination", false);
                vis.GetComponent<SpriteRenderer>().color = new Color(0.85f, 0.72f, 1f);
                var g2 = Child(go.transform, "Brilho", closed, new Vector3(0f, h * 0.5f, 0f), DoorScale * 1.08f, 3);
                Set(so, "glowRenderer", g2.GetComponent<SpriteRenderer>());
                Set(so, "glowColor", new Color(0.7f, 0.3f, 1f, 0.35f));
                break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    private static GameObject BuildArrowSign()
    {
        var go = new GameObject(PlacaSeta);
        var sprite = CainosSprite("TX Village Props - Road Sign 01");
        var vis = Child(go.transform, "Visual", sprite, Vector3.zero, 1.5f, 3);
        var sign = go.AddComponent<CastleSign>();
        var so = new SerializedObject(sign);
        Set(so, "mode", (int)CastleSign.Mode.ApontarPrimeiraSala);
        Set(so, "signRenderer", vis.GetComponent<SpriteRenderer>());
        Set(so, "signSpriteFacesRight", true);
        Set(so, "flipSignWithArrow", true);
        Set(so, "arrowHeight", 2.5f);
        Set(so, "textHeight", 3.4f);
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    private static GameObject BuildHintSign()
    {
        var go = new GameObject(PlacaDica);
        var vis = Child(go.transform, "Visual", CainosSprite("TX Village Props - Sign 01"), Vector3.zero, 1.5f, 3);
        var sign = go.AddComponent<CastleSign>();
        var so = new SerializedObject(sign);
        Set(so, "mode", (int)CastleSign.Mode.Manual);
        Set(so, "manualArrow", (int)CastleSign.Arrow.Nenhuma);
        Set(so, "manualText", "Escreva a dica aqui (Inspector → Manual Text).");
        Set(so, "signRenderer", vis.GetComponent<SpriteRenderer>());
        Set(so, "flipSignWithArrow", false);
        Set(so, "showFloatingArrow", false);
        Set(so, "textHeight", 3.0f);
        Set(so, "textRange", 3.5f);
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    private static GameObject BuildLever()
    {
        Sprite[] props = Props();
        Sprite up = P(props, 11), down = P(props, 12);
        float h = up != null ? up.bounds.size.y * 1.6f : 1.2f;
        var go = new GameObject(Alavanca);
        var vis = Child(go.transform, "Visual", up, new Vector3(0f, h * 0.5f, 0f), 1.6f, 5);
        var lever = go.AddComponent<CastleLever>();
        var so = new SerializedObject(lever);
        Set(so, "leverRenderer", vis.GetComponent<SpriteRenderer>());
        Set(so, "upSprite", up);
        Set(so, "downSprite", down);
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    private static GameObject BuildRoom()
    {
        var go = new GameObject(Sala);
        go.AddComponent<CastleMazeRoom>();
        return go;
    }

    private static GameObject BuildStart()
    {
        var go = new GameObject(Inicio);
        go.AddComponent<CastleMazeStart>();
        return go;
    }

    private static GameObject BuildKey()
    {
        var go = new GameObject(Chave);
        go.transform.localScale = Vector3.one * 1.3f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(KeyRust);
        sr.sortingOrder = 60;
        var c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = 0.6f;
        go.AddComponent<ProgressKeyPickup>().Configure(GameProgress.KeyLabirinto);
        return go;
    }

    private static GameObject BuildManager()
    {
        var go = new GameObject(Gerenciador);
        go.AddComponent<CastleMaze>();
        return go;
    }

    // =====================================================================
    // INSERIR NA CENA
    // =====================================================================

    private static void Insert(string name, float yOffset = 0f)
    {
        var prefab = Load(name);
        if (prefab == null) return;
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var view = SceneView.lastActiveSceneView;
        Vector3 pos = view != null ? new Vector3(Mathf.Round(view.pivot.x * 2f) / 2f, Mathf.Round(view.pivot.y), 0f) : Vector3.zero;
        inst.transform.position = pos + Vector3.up * yOffset;
        Undo.RegisterCreatedObjectUndo(inst, "Inserir " + name);
        Selection.activeGameObject = inst;

        // Toda cena com labirinto precisa de um gerenciador (HUD / salas visitadas).
        if (name != Gerenciador && Object.FindAnyObjectByType<CastleMaze>() == null)
        {
            var mgr = (GameObject)PrefabUtility.InstantiatePrefab(Load(Gerenciador));
            Undo.RegisterCreatedObjectUndo(mgr, "Gerenciador do Labirinto");
        }
    }

    // =====================================================================
    // HELPERS (também usados pelo LabirintoCasteloBuilder)
    // =====================================================================

    private static void Save(GameObject go)
    {
        PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/{go.name}.prefab");
        Object.DestroyImmediate(go);
    }

    public static Sprite[] Props() =>
        AssetDatabase.LoadAllAssetsAtPath(MedievalProps).OfType<Sprite>().ToArray();

    public static Sprite P(Sprite[] props, int index) => props.FirstOrDefault(s => s.name == "Medieval_props_free_" + index);

    public static Sprite CainosSprite(string spriteName) =>
        AssetDatabase.LoadAllAssetsAtPath(CainosProps).OfType<Sprite>().FirstOrDefault(s => s.name == spriteName);

    private static Sprite[] FolderSprites(string folder) =>
        AssetDatabase.FindAssets("t:Sprite", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(p => p)
            .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p)).Where(s => s != null).ToArray();

    public static GameObject Child(Transform parent, string name, Sprite sprite, Vector3 localPos, float scale, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    public static void Set(SerializedObject so, string field, object value)
    {
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[Labirinto do Castelo] Campo '{field}' não encontrado em {so.targetObject.GetType().Name}."); return; }
        switch (value)
        {
            case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
            case int i when p.propertyType == SerializedPropertyType.Float: p.floatValue = i; break;
            case int i: p.intValue = i; break;
            case float f: p.floatValue = f; break;
            case bool b: p.boolValue = b; break;
            case string s: p.stringValue = s; break;
            case Vector2 v: p.vector2Value = v; break;
            case Color c: p.colorValue = c; break;
            case Object o: p.objectReferenceValue = o; break;
            case null: p.objectReferenceValue = null; break;
        }
    }

    public static void SetArray(SerializedObject so, string field, Object[] items)
    {
        var p = so.FindProperty(field);
        if (p == null) return;
        items = items ?? new Object[0];
        p.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }
}
#endif
