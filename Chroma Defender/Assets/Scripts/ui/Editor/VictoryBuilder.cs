#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Menus: Chroma Defender > Criar Tela de Vitória  e  Chroma Defender > Criar Gatilho de Vitória.
/// </summary>
public static class VictoryBuilder
{
    private const string FontPath = "Assets/BoldPixels/Assets/font/BoldPixels.ttf";

    private static readonly Color Fire = new Color(1f, 0.35f, 0.1f);
    private static readonly Color Ice = new Color(0.35f, 0.82f, 1f);

    [MenuItem("Chroma Defender/Criar Tela de Vitória")]
    public static void Build()
    {
        var old = Object.FindAnyObjectByType<VictoryController>();
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var root = new GameObject("TelaVitoria", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        // Fundo escurecido, num tom neutro entre Fogo e Gelo
        var tint = NewRect("Fundo", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        tint.offsetMin = tint.offsetMax = Vector2.zero;
        var tintImg = tint.gameObject.AddComponent<Image>();
        tintImg.color = new Color(0.04f, 0.05f, 0.1f, 0.88f);
        tintImg.raycastTarget = true; // bloqueia cliques no jogo por trás

        // Bloco de texto e botões centralizado na tela
        var title = NewText("Titulo", root.transform, font, "VITÓRIA!", 150, Fire);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1400, 170));
        AddShadow(title.gameObject, new Color(0f, 0.05f, 0.15f, 0.9f));

        var sub = NewText("Subtitulo", root.transform, font, "O EQUILÍBRIO ENTRE FOGO E GELO FOI RESTAURADO", 40, new Color(1f, 1f, 1f, 0.85f));
        SetRect(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(1600, 60));

        var next = NewButton("BotaoProximaFase", root.transform, font, "PRÓXIMA FASE", new Vector2(0, -30), Ice);
        var replay = NewButton("BotaoRejogar", root.transform, font, "JOGAR DE NOVO", new Vector2(0, -150), Fire);
        var menu = NewButton("BotaoMenu", root.transform, font, "MENU", new Vector2(0, -270), Ice);
        Navigation(next, replay, menu);

        var ctrl = root.AddComponent<VictoryController>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
        so.FindProperty("title").objectReferenceValue = title;
        so.FindProperty("nextButton").objectReferenceValue = next;
        so.FindProperty("replayButton").objectReferenceValue = replay;
        so.FindProperty("menuButton").objectReferenceValue = menu;
        so.ApplyModifiedProperties();

        root.GetComponent<CanvasGroup>().alpha = 0f;
        root.GetComponent<CanvasGroup>().blocksRaycasts = false;

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[Vitória] Tela criada. Falta posicionar o gatilho: Chroma Defender > Criar Gatilho de Vitória.");
    }

    [MenuItem("Chroma Defender/Criar Gatilho de Vitória")]
    public static void BuildTrigger()
    {
        var go = new GameObject("GatilhoVitoria", typeof(BoxCollider2D), typeof(VictoryTrigger));
        var col = go.GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 6f);

        var view = SceneView.lastActiveSceneView;
        if (view != null) { var p = view.pivot; go.transform.position = new Vector3(p.x, p.y, 0f); }

        Undo.RegisterCreatedObjectUndo(go, "Criar Gatilho de Vitória");
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("[Vitória] Gatilho criado. Arraste-o para o fim da fase (onde o jogador deve vencer).");
    }

    private static void Navigation(params Button[] list)
    {
        for (int k = 0; k < list.Length; k++)
        {
            list[k].navigation = new Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnDown = list[(k + 1) % list.Length],
                selectOnUp = list[(k + list.Length - 1) % list.Length]
            };
        }
    }

    private static Button NewButton(string name, Transform parent, Font font, string label, Vector2 pos, Color accent)
    {
        var border = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(640, 100));
        var borderImg = border.gameObject.AddComponent<Image>();
        borderImg.color = new Color(accent.r, accent.g, accent.b, 0.35f);

        var inner = NewRect("Fundo", border, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        inner.offsetMin = new Vector2(5, 5); inner.offsetMax = new Vector2(-5, -5);
        var innerImg = inner.gameObject.AddComponent<Image>();
        innerImg.color = new Color(accent.r * 0.12f, accent.g * 0.12f, accent.b * 0.12f, 0.92f);

        var text = NewText("Texto", border, font, label, 60, accent);
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;

        var btn = border.gameObject.AddComponent<Button>();
        btn.targetGraphic = innerImg;
        btn.transition = Selectable.Transition.ColorTint;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
        cb.selectedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = cb;

        var fx = border.gameObject.AddComponent<MenuButtonFx>();
        fx.border = borderImg;
        fx.accent = accent;
        return btn;
    }

    private static void AddShadow(GameObject go, Color c)
    {
        var s = go.AddComponent<Shadow>();
        s.effectColor = c;
        s.effectDistance = new Vector2(8, -8);
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        SetRect(rt, aMin, aMax, pos, size);
        return rt;
    }

    private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    private static Text NewText(string name, Transform parent, Font font, string content, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font; t.text = content; t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter; t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        go.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.9f);
        return t;
    }
}
#endif
