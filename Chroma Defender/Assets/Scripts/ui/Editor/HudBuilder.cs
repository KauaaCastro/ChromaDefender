#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Menu: Chroma Defender > Criar HUD na cena. Monta (ou refaz) o Canvas do HUD.</summary>
public static class HudBuilder
{
    [MenuItem("Chroma Defender/Criar HUD na cena")]
    public static void BuildHud()
    {
        var old = Object.FindFirstObjectByType<HudController>();
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Criar HUD");
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        // ---------- Vida (canto superior esquerdo) ----------
        var bar = NewRect("BarraVida", canvasGo.transform, new Vector2(0, 1), new Vector2(30, -30), new Vector2(460, 44));
        bar.gameObject.AddComponent<Image>().color = new Color(0.03f, 0.03f, 0.06f, 0.9f);
        var fill = NewRect("Preenchimento", bar, Vector2.zero, Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
        fill.pivot = new Vector2(0, 0.5f);
        fill.offsetMin = new Vector2(5, 5); fill.offsetMax = new Vector2(-5, -5);
        fill.gameObject.AddComponent<Image>().color = new Color(0.85f, 0.15f, 0.2f);
        var healthText = NewText("TextoVida", bar, "100/100", 26, TextAnchor.MiddleCenter);
        Stretch(healthText.rectTransform);
        var healthLabel = NewText("RotuloVida", bar, "VIDA", 22, TextAnchor.MiddleLeft);
        healthLabel.rectTransform.anchorMin = Vector2.zero;
        healthLabel.rectTransform.anchorMax = Vector2.one;
        healthLabel.rectTransform.offsetMin = new Vector2(14, 0);
        healthLabel.rectTransform.offsetMax = new Vector2(-14, 0);
        healthLabel.color = new Color(1f, 0.8f, 0.8f);

        // ---------- Técnica / Combo Leve (abaixo da Vida) ----------
        var comboPanel = NewRect("PainelTecnica", canvasGo.transform, new Vector2(0, 1), new Vector2(30, -82), new Vector2(390, 44), new Vector2(0, 1));
        var comboBorder = comboPanel.gameObject.AddComponent<Image>();
        comboBorder.color = new Color(0.75f, 0.82f, 0.95f, 0.32f);
        comboBorder.raycastTarget = false;

        var comboInner = NewRect("Fundo", comboPanel, Vector2.zero, Vector2.zero, Vector2.zero);
        comboInner.anchorMin = Vector2.zero; comboInner.anchorMax = Vector2.one;
        comboInner.offsetMin = new Vector2(2, 2); comboInner.offsetMax = new Vector2(-2, -2);
        var comboInnerImg = comboInner.gameObject.AddComponent<Image>();
        comboInnerImg.color = new Color(0.04f, 0.04f, 0.08f, 0.88f);
        comboInnerImg.raycastTarget = false;

        var comboIconRt = NewRect("IconeEspada", comboPanel, new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(28, 28), new Vector2(0.5f, 0.5f));
        var comboIcon = comboIconRt.gameObject.AddComponent<Image>();
        comboIcon.preserveAspect = true;
        comboIcon.raycastTarget = false;

        var comboText = NewText("TextoTecnica", comboPanel, "ATAQUE LEVE: 2 GOLPES", 18, TextAnchor.MiddleLeft);
        comboText.rectTransform.anchorMin = Vector2.zero;
        comboText.rectTransform.anchorMax = Vector2.one;
        comboText.rectTransform.offsetMin = new Vector2(46, 0);
        comboText.rectTransform.offsetMax = new Vector2(-96, 0);

        var pips = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var pipRt = NewRect($"Golpe_{i + 1}", comboPanel, new Vector2(1, 0.5f), new Vector2(-72 + i * 26, 0), new Vector2(18, 18), new Vector2(0.5f, 0.5f));
            var pipImg = pipRt.gameObject.AddComponent<Image>();
            pipImg.raycastTarget = false;
            pips[i] = pipImg;
        }

        // ---------- Polaridade (canto superior direito) ----------
        var panel = NewRect("Polaridade", canvasGo.transform, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(300, 220), new Vector2(1, 1));

        var glow = NewImage("Brilho", panel, new Vector2(90, -72), 96);   // fica atrás dos slots
        var fireSlot = NewSlot("SlotFogo", panel, new Vector2(90, -72), out var fireFrame, out var fireIcon);
        var iceSlot = NewSlot("SlotGelo", panel, new Vector2(210, -72), out var iceFrame, out var iceIcon);

        var label = NewText("TextoElemento", panel, "FOGO", 38, TextAnchor.MiddleCenter);
        SetRect(label.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(300, 46), new Vector2(0.5f, 0));

        var hud = canvasGo.AddComponent<HudController>();
        var so = new SerializedObject(hud);
        so.FindProperty("healthFill").objectReferenceValue = fill;
        so.FindProperty("healthText").objectReferenceValue = healthText;
        so.FindProperty("fireSlot").objectReferenceValue = fireSlot;
        so.FindProperty("fireFrame").objectReferenceValue = fireFrame;
        so.FindProperty("fireIcon").objectReferenceValue = fireIcon;
        so.FindProperty("iceSlot").objectReferenceValue = iceSlot;
        so.FindProperty("iceFrame").objectReferenceValue = iceFrame;
        so.FindProperty("iceIcon").objectReferenceValue = iceIcon;
        so.FindProperty("glow").objectReferenceValue = glow;
        so.FindProperty("elementText").objectReferenceValue = label;
        so.FindProperty("comboPanel").objectReferenceValue = comboPanel;
        so.FindProperty("comboBorder").objectReferenceValue = comboBorder;
        so.FindProperty("comboIcon").objectReferenceValue = comboIcon;
        so.FindProperty("comboText").objectReferenceValue = comboText;
        var pipsProp = so.FindProperty("comboPips");
        pipsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            pipsProp.GetArrayElementAtIndex(i).objectReferenceValue = pips[i];
        so.ApplyModifiedProperties();

        Selection.activeGameObject = canvasGo;
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
    }

    [MenuItem("Chroma Defender/Testar HUD: Desbloquear Todas as Técnicas")]
    public static void UnlockAllForTest()
    {
        var player = Object.FindFirstObjectByType<PlayerBehavior>();
        GameProgress.UnlockLightCombo(3);
        GameProgress.UnlockHeavyCombo(3);
        GameProgress.UnlockWeaponArt();

        if (player != null)
        {
            player.UpgradeLightCombo(3);
            player.UnlockHeavyAttack(3);
            player.UnlockWeaponArt();
        }

        if (HudController.Instance != null)
            HudController.Instance.RefreshCombo();

        if (Application.isPlaying)
            ProgressHud.Toast("Teste de HUD: <color=#FFD54A>Todas as Técnicas Desbloqueadas!</color>\nCombo Nv. 3, Ataque Pesado (Botão Direito) e Supremo (F/E) ativos!", 4.5f);
    }

    // Slot = moldura redonda + ícone por cima
    private static RectTransform NewSlot(string name, Transform parent, Vector2 pos, out Image frame, out Image icon)
    {
        var slot = NewRect(name, parent, new Vector2(0, 1), pos, new Vector2(96, 96), new Vector2(0.5f, 0.5f));
        frame = slot.gameObject.AddComponent<Image>();
        var ic = NewRect("Icone", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72, 72), new Vector2(0.5f, 0.5f));
        icon = ic.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        return slot;
    }

    private static Image NewImage(string name, Transform parent, Vector2 pos, float size)
    {
        var rt = NewRect(name, parent, new Vector2(0, 1), pos, new Vector2(size, size), new Vector2(0.5f, 0.5f));
        var img = rt.gameObject.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        SetRect(rt, anchor, pos, size, pivot ?? anchor);
        return rt;
    }

    private static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot ?? anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static Text NewText(string name, Transform parent, string content, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text = content;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = anchor;
        t.color = Color.white;
        t.raycastTarget = false;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
        return t;
    }
}
#endif


