#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Menu: Chroma Defender > Criar Tela de Game Over. Monta (ou refaz) a tela na cena.</summary>
public static class GameOverBuilder
{
    private const string FontPath = "Assets/BoldPixels/Assets/font/BoldPixels.ttf";
    private const string SkullDir = "Assets/Screw And Tape/2D Pixel Art Skull Icons  Emblems/";

    [MenuItem("Chroma Defender/Criar Tela de Game Over")]
    public static void Build()
    {
        var old = Object.FindFirstObjectByType<GameOverController>();
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var fireSkull = AssetDatabase.LoadAssetAtPath<Texture2D>(SkullDir + "skull_red_0-export.png");
        var iceSkull = AssetDatabase.LoadAssetAtPath<Texture2D>(SkullDir + "skull_cyan_0-export.png");

        var root = new GameObject("GameOver", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Undo.RegisterCreatedObjectUndo(root, "Criar Game Over");
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // por cima do HUD
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        // Fundo escurecido (cor muda com a polaridade)
        var tintRt = NewRect("Fundo", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        tintRt.offsetMin = tintRt.offsetMax = Vector2.zero;
        var tint = tintRt.gameObject.AddComponent<Image>();
        tint.color = new Color(0.1f, 0.03f, 0.02f, 0.88f);
        tint.raycastTarget = false;

        // Caveira
        var skullRt = NewRect("Caveira", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(190, 190));
        var skull = skullRt.gameObject.AddComponent<RawImage>();
        skull.texture = fireSkull;
        skull.raycastTarget = false;

        // Título
        var title = NewText("Titulo", root.transform, font, "GAME OVER", 130, new Color(1f, 0.35f, 0.1f));
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1200, 150));

        // Barra de renascimento
        var barBg = NewRect("BarraRenascer", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(520, 20));
        var barBgImg = barBg.gameObject.AddComponent<Image>();
        barBgImg.color = new Color(0, 0, 0, 0.8f);
        barBgImg.raycastTarget = false;
        var fill = NewRect("Preenchimento", barBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fill.pivot = new Vector2(0, 0.5f);
        fill.offsetMin = new Vector2(3, 3); fill.offsetMax = new Vector2(-3, -3);
        var fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.color = new Color(1f, 0.35f, 0.1f);
        fillImg.raycastTarget = false;

        var sub = NewText("Subtitulo", root.transform, font, "RENASCENDO...", 40, Color.white);
        SetRect(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -195), new Vector2(800, 50));

        var ctrl = root.AddComponent<GameOverController>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
        so.FindProperty("tint").objectReferenceValue = tint;
        so.FindProperty("skull").objectReferenceValue = skull;
        so.FindProperty("title").objectReferenceValue = title;
        so.FindProperty("subtitle").objectReferenceValue = sub;
        so.FindProperty("progressFill").objectReferenceValue = fill;
        so.FindProperty("fireSkull").objectReferenceValue = fireSkull;
        so.FindProperty("iceSkull").objectReferenceValue = iceSkull;
        so.ApplyModifiedProperties();

        // Começa invisível também no editor
        root.GetComponent<CanvasGroup>().alpha = 0f;

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        if (fireSkull == null || iceSkull == null)
            Debug.LogWarning("[GameOver] Caveiras não encontradas em " + SkullDir + ". Atribua as texturas no GameOverController.");
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        SetRect(rt, aMin, aMax, pos, size);
        return rt;
    }

    private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static Text NewText(string name, Transform parent, Font font, string content, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        go.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.9f);
        return t;
    }
}
#endif

