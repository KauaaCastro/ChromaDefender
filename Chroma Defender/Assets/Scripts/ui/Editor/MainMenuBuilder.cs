#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Menu: Chroma Defender > Criar Menu Inicial. Cria a cena MainMenu e a coloca como primeira do Build.</summary>
public static class MainMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/SampleScene.unity";
    private const string FontPath = "Assets/BoldPixels/Assets/font/BoldPixels.ttf";
    private const string ForestDir = "Assets/FREE Parallax Forest Background HQ/Parallax Forest Background/";
    private const string SkullDir = "Assets/Screw And Tape/2D Pixel Art Skull Icons  Emblems/";

    private static readonly Color Fire = new Color(1f, 0.35f, 0.1f);
    private static readonly Color Ice = new Color(0.35f, 0.82f, 1f);

    [MenuItem("Chroma Defender/Criar Menu Inicial")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var cam = Object.FindAnyObjectByType<Camera>();
        if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f); }

        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var fireSkull = AssetDatabase.LoadAssetAtPath<Texture2D>(SkullDir + "skull_red_0-export.png");
        var iceSkull = AssetDatabase.LoadAssetAtPath<Texture2D>(SkullDir + "skull_cyan_0-export.png");
        if (fireSkull != null) fireSkull.filterMode = FilterMode.Point;
        if (iceSkull != null) iceSkull.filterMode = FilterMode.Point;

        var root = new GameObject("MainMenu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        // ---- Fundo: floresta em camadas (do fundo para a frente) ----
        string[] files = { "01_Sky", "02_Forest", "03_Forest", "04_Forest", "05_Forest", "06_Particles", "07_Forest", "08_Particles", "09_Bushes", "10_Mist" };
        float[] speeds = { 0f, 0.002f, 0.004f, 0.006f, 0.009f, 0.014f, 0.012f, 0.02f, 0.016f, 0.01f };
        float[] shifts = { 0.002f, 0.005f, 0.008f, 0.012f, 0.016f, 0.02f, 0.024f, 0.028f, 0.034f, 0.02f };
        var layers = new RawImage[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ForestDir + files[i] + ".png");
            var rt = NewRect("Camada " + files[i], root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // Um pouco maior que a tela para o parallax do mouse nÃ£o mostrar bordas
            rt.offsetMin = new Vector2(-40, -30); rt.offsetMax = new Vector2(40, 30);
            var img = rt.gameObject.AddComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;
            layers[i] = img;
        }

        // Escurece o fundo e dÃ¡ vinheta para o texto se destacar
        var shade = NewImage("Escurecer", root.transform, new Color(0.02f, 0.03f, 0.08f, 0.45f));
        Stretch(shade.rectTransform);
        // (faixas de vinheta removidas: deixavam o topo e a base com tom diferente)

        // ---- TÃ­tulo ----
        var titleRoot = NewRect("Titulo", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1400, 340));
        var chroma = NewText("Chroma", titleRoot, font, "CHROMA", 190, Fire);
        SetRect(chroma.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1400, 190));
        var defender = NewText("Defender", titleRoot, font, "DEFENDER", 190, Ice);
        SetRect(defender.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(1400, 190));
        AddShadow(chroma.gameObject, new Color(0.35f, 0.05f, 0f, 0.9f));
        AddShadow(defender.gameObject, new Color(0f, 0.15f, 0.35f, 0.9f));

        // ---- BotÃµes ----
        var play = NewButton("BotaoJogar", root.transform, font, "JOGAR", new Vector2(0, -90), Fire);
        var quit = NewButton("BotaoSair", root.transform, font, "SAIR", new Vector2(0, -220), Ice);
        SetNavigation(play, quit);

        var credit = NewText("Creditos", root.transform, font, "S.K.M.  -  UNITAU", 30, new Color(1f, 1f, 1f, 0.55f));
        SetRect(credit.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(800, 40));

        // ---- Fade (preto) por cima de tudo ----
        var fadeGo = new GameObject("Fade", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        fadeGo.transform.SetParent(root.transform, false);
        Stretch((RectTransform)fadeGo.transform);
        var fadeImg = fadeGo.GetComponent<Image>(); fadeImg.color = Color.black; fadeImg.raycastTarget = false;
        var fade = fadeGo.GetComponent<CanvasGroup>(); fade.alpha = 0f; fade.blocksRaycasts = false; fade.interactable = false;

        // ---- MÃºsica ambiente (se existir) ----
        var guid = AssetDatabase.FindAssets("Designed Atmospheres - Harmonic - 015 t:AudioClip").FirstOrDefault();
        if (guid != null)
        {
            var src = new GameObject("MusicaAmbiente", typeof(AudioSource)).GetComponent<AudioSource>();
            src.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
            src.loop = true; src.volume = 0.35f; src.playOnAwake = true;
        }

        // ---- Controlador ----
        var ctrl = root.AddComponent<MainMenuController>();
        var so = new SerializedObject(ctrl);
        SetArray(so.FindProperty("layers"), layers);
        SetFloats(so.FindProperty("layerSpeeds"), speeds);
        SetFloats(so.FindProperty("layerMouseShift"), shifts);
        so.FindProperty("title").objectReferenceValue = titleRoot;
        so.FindProperty("fade").objectReferenceValue = fade;
        so.FindProperty("playButton").objectReferenceValue = play;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, ScenePath);
        SetBuildOrder();
        Selection.activeGameObject = root;
        Debug.Log("[Chroma Defender] Menu inicial criado em " + ScenePath + " e colocado como primeira cena do Build.");
        if (fireSkull == null || iceSkull == null)
            Debug.LogWarning("[Chroma Defender] NÃ£o achei as caveiras em " + SkullDir);
    }

    private static void SetBuildOrder()
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != ScenePath) list.Add(s);
        if (!list.Any(s => s.path == GameScenePath)) list.Add(new EditorBuildSettingsScene(GameScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    private static Button NewButton(string name, Transform parent, Font font, string label, Vector2 pos, Color accent)
    {
        var border = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(540, 100));
        var borderImg = border.gameObject.AddComponent<Image>();
        borderImg.color = new Color(accent.r, accent.g, accent.b, 0.35f);

        var inner = NewRect("Fundo", border, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        inner.offsetMin = new Vector2(5, 5); inner.offsetMax = new Vector2(-5, -5);
        var innerImg = inner.gameObject.AddComponent<Image>();
        innerImg.color = new Color(accent.r * 0.12f, accent.g * 0.12f, accent.b * 0.12f, 0.92f);

        var text = NewText("Texto", border, font, label, 64, accent);
        Stretch(text.rectTransform);

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

    private static void SetNavigation(Button a, Button b)
    {
        var na = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = b, selectOnUp = b };
        var nb = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = a, selectOnDown = a };
        a.navigation = na; b.navigation = nb;
    }

    private static void SetArray(SerializedProperty p, Object[] items)
    {
        p.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    private static void SetFloats(SerializedProperty p, float[] items)
    {
        p.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++) p.GetArrayElementAtIndex(i).floatValue = items[i];
    }

    private static void AddShadow(GameObject go, Color c)
    {
        var s = go.AddComponent<Shadow>();
        s.effectColor = c;
        s.effectDistance = new Vector2(8, -8);
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color; img.raycastTarget = false;
        return img;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
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

