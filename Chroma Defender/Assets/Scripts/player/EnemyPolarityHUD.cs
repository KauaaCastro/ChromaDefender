using UnityEngine;

/// <summary>
/// Indicador visual flutuante acima dos inimigos.
/// Exibe a fraqueza elemental (qual polaridade o jogador DEVE usar: FOGO ou GELO),
/// o ícone temático correspondente e uma barra de vida compacta.
/// </summary>
public class EnemyPolarityHUD : MonoBehaviour
{
    private EnemyBehavior enemy;
    private GameObject hudRoot;
    private SpriteRenderer iconRenderer;
    private SpriteRenderer barBgRenderer;
    private const float hudGap = 0.18f; // distância entre a cabeça do mob e a barra
    private SpriteRenderer barFillRenderer;

    private float baseFillWidth = 0.85f;
    private float barHeight = 0.08f;
    private float headOffset = 0.6f;
    private float bobOffset = 0f;

    private static Sprite fireIconSprite;
    private static Sprite iceIconSprite;
    private static Sprite neutralIconSprite;
    private static Sprite barPixelSprite;

    public void Initialize(EnemyBehavior targetEnemy)
    {
        enemy = targetEnemy;
        BuildHUD();
        UpdatePolarityDisplay();
        UpdateHealthDisplay();
    }

    private void Awake()
    {
        if (enemy == null)
        {
            enemy = GetComponent<EnemyBehavior>();
            if (enemy != null) Initialize(enemy);
        }
    }

    private void BuildHUD()
    {
        if (hudRoot != null) Destroy(hudRoot);

        hudRoot = new GameObject("Polarity_HUD");
        hudRoot.transform.SetParent(transform, false);

        // Calcula altura da cabeça baseada no colisor ou sprite
        Collider2D col = GetComponent<Collider2D>();
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        if (col != null)
        {
            headOffset = col.bounds.extents.y + 0.35f;
        }
        else if (sr != null)
        {
            headOffset = sr.bounds.extents.y + 0.35f;
        }
        else
        {
            headOffset = 0.7f;
        }

        hudRoot.transform.localPosition = new Vector3(0f, headOffset, 0f);

        // 1. Ícone Elemental pequeno, à esquerda da barra de vida (Chama de Fogo ou Cristal de Gelo)
        GameObject iconObj = new GameObject("Elemental_Icon");
        iconObj.transform.SetParent(hudRoot.transform, false);
        iconObj.transform.localPosition = new Vector3(-baseFillWidth * 0.5f - 0.2f, 0.05f, 0f);
        iconObj.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

        iconRenderer = iconObj.AddComponent<SpriteRenderer>();
        iconRenderer.sortingOrder = 30;

        // 2. Contorno colorido no próprio mob, na cor do elemento que o jogador deve usar
        BuildOutline();

        // 3. Barra de Vida (Fundo Escuro)
        EnsureBarSprite();
        GameObject bgObj = new GameObject("Bar_Background");
        bgObj.transform.SetParent(hudRoot.transform, false);
        bgObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);

        barBgRenderer = bgObj.AddComponent<SpriteRenderer>();
        barBgRenderer.sprite = barPixelSprite;
        barBgRenderer.color = new Color(0.1f, 0.12f, 0.15f, 0.85f);
        barBgRenderer.drawMode = SpriteDrawMode.Sliced;
        barBgRenderer.size = new Vector2(baseFillWidth + 0.06f, barHeight + 0.04f);
        barBgRenderer.sortingOrder = 28;

        // 4. Barra de Vida (Preenchimento com a cor correspondente)
        GameObject fillObj = new GameObject("Bar_Fill");
        fillObj.transform.SetParent(hudRoot.transform, false);
        fillObj.transform.localPosition = new Vector3(-baseFillWidth * 0.5f, 0.05f, 0f);

        barFillRenderer = fillObj.AddComponent<SpriteRenderer>();
        barFillRenderer.sprite = barPixelSprite;
        barFillRenderer.drawMode = SpriteDrawMode.Sliced;
        barFillRenderer.size = new Vector2(baseFillWidth, barHeight);
        barFillRenderer.sortingOrder = 29;
    }

    public void UpdatePolarityDisplay()
    {
        if (enemy == null) return;

        EnsureIcons();

        ElementPolarity? required = enemy.RequiredAttackPolarity;

        if (!required.HasValue)
        {
            // Inimigo NEUTRO -> Qualquer ataque funciona (sem contorno colorido)
            if (iconRenderer != null)
            {
                iconRenderer.sprite = neutralIconSprite;
                iconRenderer.color = new Color(1f, 1f, 1f, 0.85f);
            }
            if (barFillRenderer != null) barFillRenderer.color = new Color(0.25f, 0.85f, 0.45f, 1f); // Verde clássico
            outlineColor = Color.clear;
            return;
        }

        // Tudo na cor do elemento que o jogador DEVE usar: ícone, barra de vida e contorno do mob.
        bool useFire = required.Value == ElementPolarity.Fire;
        Color c = useFire ? FireColor : IceColor;
        if (iconRenderer != null)
        {
            iconRenderer.sprite = useFire ? fireIconSprite : iceIconSprite;
            iconRenderer.color = Color.white;
        }
        if (barFillRenderer != null) barFillRenderer.color = c;
        outlineColor = c;
    }

    // ------------------------------------------------------------------
    // Contorno colorido (4 cópias do sprite, deslocadas 1 pixel, pintadas de cor sólida)
    // ------------------------------------------------------------------
    private static readonly Color FireColor = new Color(1f, 0.45f, 0.1f, 1f);
    private static readonly Color IceColor = new Color(0.25f, 0.85f, 1f, 1f);
    private static readonly Vector2[] OutlineDirs = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
    private static Material outlineMaterial;

    private GameObject outlineRoot;
    private SpriteRenderer[] outlineRenderers;
    private SpriteRenderer bodyRenderer;
    private Color outlineColor = Color.clear;
    private PlayerBehavior player;

    private void BuildOutline()
    {
        if (outlineRoot != null) Destroy(outlineRoot);
        outlineRenderers = null;

        bodyRenderer = GetComponent<SpriteRenderer>();
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        if (bodyRenderer == null) return;

        if (outlineMaterial == null)
        {
            // Shader que pinta o formato do sprite com uma cor sólida (o mesmo usado pelo TextMesh, então sempre vai no build).
            Shader solid = Shader.Find("GUI/Text Shader");
            if (solid == null) return;
            outlineMaterial = new Material(solid) { name = "Enemy_Outline" };
        }

        outlineRoot = new GameObject("Polarity_Outline");
        outlineRoot.transform.SetParent(bodyRenderer.transform, false);

        outlineRenderers = new SpriteRenderer[OutlineDirs.Length];
        for (int i = 0; i < OutlineDirs.Length; i++)
        {
            var go = new GameObject("Outline_" + i);
            go.transform.SetParent(outlineRoot.transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sharedMaterial = outlineMaterial;
            r.sortingLayerID = bodyRenderer.sortingLayerID;
            r.sortingOrder = bodyRenderer.sortingOrder - 1;
            outlineRenderers[i] = r;
        }
    }

    private void UpdateOutline()
    {
        if (outlineRenderers == null || bodyRenderer == null) return;

        bool visible = outlineColor.a > 0f && bodyRenderer.enabled && bodyRenderer.sprite != null;
        outlineRoot.SetActive(visible);
        if (!visible) return;

        // Mais forte e pulsando quando o jogador já está com o elemento certo; mais fraco quando está errado.
        if (player == null) player = FindAnyObjectByType<PlayerBehavior>();
        ElementPolarity? required = enemy.RequiredAttackPolarity;
        bool playerMatches = player != null && required.HasValue && player.CurrentPolarity == required.Value;
        float alpha = playerMatches ? 0.75f + Mathf.Sin(Time.time * 8f) * 0.25f : 0.55f;
        Color c = outlineColor;
        c.a = alpha * bodyRenderer.color.a; // some junto no fade da morte

        Sprite s = bodyRenderer.sprite;
        float px = 1f / Mathf.Max(1f, s.pixelsPerUnit);
        for (int i = 0; i < outlineRenderers.Length; i++)
        {
            var r = outlineRenderers[i];
            r.sprite = s;
            r.flipX = bodyRenderer.flipX;
            r.flipY = bodyRenderer.flipY;
            r.color = c;
            r.sortingOrder = bodyRenderer.sortingOrder - 1;
            r.transform.localPosition = OutlineDirs[i] * px;
        }
    }

    public void UpdateHealthDisplay()
    {
        if (enemy == null || barFillRenderer == null) return;

        float ratio = Mathf.Clamp01((float)enemy.CurrentHealth / Mathf.Max(1, enemy.MaxHealth));
        float currentW = baseFillWidth * ratio;

        barFillRenderer.size = new Vector2(currentW, barHeight);
        // Mantém alinhado à esquerda do fundo da barra
        barFillRenderer.transform.localPosition = new Vector3(-baseFillWidth * 0.5f + (currentW * 0.5f), 0.05f, 0f);

        if (enemy.IsDead && hudRoot != null)
        {
            hudRoot.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (hudRoot == null || enemy == null) return;

        UpdateOutline();

        if (enemy.IsDead)
        {
            hudRoot.SetActive(false);
            return;
        }

        // Flutuação rítmica sutil
        bobOffset = Mathf.Sin(Time.time * 3f) * 0.03f;

        // Topo do CORPO do inimigo. O colisor vem primeiro: os sprites têm muito espaço vazio acima do monstro,
        // então usar o topo do sprite deixava o indicador longe da cabeça.
        SpriteRenderer sr = bodyRenderer != null ? bodyRenderer : GetComponentInChildren<SpriteRenderer>();
        Collider2D col = GetComponent<Collider2D>();

        float worldTopY = transform.position.y + 0.6f;
        float centerX = transform.position.x;
        if (col != null && col.enabled && col.bounds.size.y > 0.05f)
        {
            worldTopY = col.bounds.max.y;
            centerX = col.bounds.center.x;
        }
        else if (sr != null && sr.bounds.size.y > 0.05f)
        {
            worldTopY = sr.bounds.max.y;
        }

        // Posiciona o HUD logo acima da cabeça do monstro
        hudRoot.transform.position = new Vector3(centerX, worldTopY + hudGap + bobOffset, transform.position.z);
        hudRoot.transform.rotation = Quaternion.identity;

        // Compensa a escala do pai para que o HUD mantenha tamanho 100% nítido e constante em QUALQUER monstro (Rato, Slime, Morcego, etc.)
        Vector3 parentLossy = transform.lossyScale;
        float invX = Mathf.Abs(parentLossy.x) > 0.0001f ? (1f / Mathf.Abs(parentLossy.x)) : 1f;
        float invY = Mathf.Abs(parentLossy.y) > 0.0001f ? (1f / Mathf.Abs(parentLossy.y)) : 1f;
        hudRoot.transform.localScale = new Vector3(invX, invY, 1f);
    }

    private void OnDestroy()
    {
        if (hudRoot != null) Destroy(hudRoot);
        if (outlineRoot != null) Destroy(outlineRoot);
    }

    // ------------------------------------------------------------------
    // Geradores Procedurais de Texturas & Sprites Pixel Art
    // ------------------------------------------------------------------
    private static void EnsureBarSprite()
    {
        if (barPixelSprite != null) return;
        Texture2D tex = new Texture2D(4, 4);
        Color[] c = new Color[16];
        for (int i = 0; i < 16; i++) c[i] = Color.white;
        tex.SetPixels(c);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        barPixelSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100);
    }

    private static void EnsureIcons()
    {
        EnsureBarSprite();
        if (fireIconSprite == null) fireIconSprite = GeneratePixelFlameSprite();
        if (iceIconSprite == null) iceIconSprite = GeneratePixelIceSprite();
        if (neutralIconSprite == null) neutralIconSprite = GeneratePixelNeutralSprite();
    }

    private static Sprite GeneratePixelFlameSprite()
    {
        int w = 16, h = 16;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        // Cores da chama
        Color outline = new Color(0.7f, 0.12f, 0f, 1f); // Vermelho escuro
        Color flame = new Color(1f, 0.45f, 0f, 1f);    // Laranja
        Color core = new Color(1f, 0.9f, 0.2f, 1f);     // Amarelo dourado
        Color white = new Color(1f, 1f, 0.8f, 1f);      // Núcleo quente

        int[] flamePattern = new int[]
        {
            0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,1,2,1,0,0,0,0,0,0,0,
            0,0,0,0,0,1,2,2,2,1,0,0,0,0,0,0,
            0,0,0,0,1,2,3,3,2,1,0,0,0,0,0,0,
            0,0,0,1,2,3,4,3,2,1,0,0,0,0,0,0,
            0,0,1,2,3,4,4,3,2,1,0,1,0,0,0,0,
            0,1,2,3,4,4,3,2,1,1,2,1,0,0,0,0,
            0,1,2,3,4,3,2,2,2,3,2,1,0,0,0,0,
            0,1,2,3,3,2,1,2,3,3,2,1,0,0,0,0,
            0,0,1,2,2,1,0,1,2,2,1,0,0,0,0,0,
            0,0,0,1,1,0,0,0,1,1,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
        };

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int val = flamePattern[(h - 1 - y) * w + x];
                if (val == 1) px[y * w + x] = outline;
                else if (val == 2) px[y * w + x] = flame;
                else if (val == 3) px[y * w + x] = core;
                else if (val == 4) px[y * w + x] = white;
            }
        }

        tex.SetPixels(px);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32);
    }

    private static Sprite GeneratePixelIceSprite()
    {
        int w = 16, h = 16;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        // Cores do cristal de gelo
        Color edge = new Color(0f, 0.35f, 0.7f, 1f);     // Azul cobalto
        Color ice = new Color(0.2f, 0.8f, 1f, 1f);       // Ciano gelo
        Color bright = new Color(0.7f, 0.95f, 1f, 1f);   // Brilho
        Color white = Color.white;

        int[] icePattern = new int[]
        {
            0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,1,3,1,0,0,0,0,0,0,0,
            0,0,0,1,0,1,2,4,2,1,0,1,0,0,0,0,
            0,0,1,2,1,2,3,4,3,2,1,2,1,0,0,0,
            0,0,0,1,2,3,4,4,4,3,2,1,0,0,0,0,
            0,0,1,2,3,4,4,4,4,4,3,2,1,0,0,0,
            0,1,0,3,4,4,3,4,3,4,4,3,0,1,0,0,
            1,3,4,4,4,4,4,4,4,4,4,4,4,3,1,0,
            0,1,0,3,4,4,3,4,3,4,4,3,0,1,0,0,
            0,0,1,2,3,4,4,4,4,4,3,2,1,0,0,0,
            0,0,0,1,2,3,4,4,4,3,2,1,0,0,0,0,
            0,0,1,2,1,2,3,4,3,2,1,2,1,0,0,0,
            0,0,0,1,0,1,2,4,2,1,0,1,0,0,0,0,
            0,0,0,0,0,0,1,3,1,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
        };

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int val = icePattern[(h - 1 - y) * w + x];
                if (val == 1) px[y * w + x] = edge;
                else if (val == 2) px[y * w + x] = ice;
                else if (val == 3) px[y * w + x] = bright;
                else if (val == 4) px[y * w + x] = white;
            }
        }

        tex.SetPixels(px);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32);
    }

    private static Sprite GeneratePixelNeutralSprite()
    {
        int w = 16, h = 16;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        Color edge = new Color(0.2f, 0.25f, 0.3f, 1f);
        Color steel = new Color(0.75f, 0.8f, 0.85f, 1f);
        Color bright = Color.white;

        int[] shieldPattern = new int[]
        {
            0,0,1,1,1,1,1,1,1,1,1,1,1,1,0,0,
            0,1,3,3,2,2,2,2,2,2,2,2,3,3,1,0,
            0,1,3,2,2,2,2,3,2,2,2,2,2,3,1,0,
            0,1,2,2,2,2,3,3,3,2,2,2,2,2,1,0,
            0,1,2,2,2,3,3,3,3,3,2,2,2,2,1,0,
            0,1,2,2,2,2,3,3,3,2,2,2,2,2,1,0,
            0,0,1,2,2,2,2,3,2,2,2,2,2,1,0,0,
            0,0,1,2,2,2,2,3,2,2,2,2,2,1,0,0,
            0,0,0,1,2,2,2,3,2,2,2,2,1,0,0,0,
            0,0,0,1,2,2,2,3,2,2,2,2,1,0,0,0,
            0,0,0,0,1,2,2,3,2,2,2,1,0,0,0,0,
            0,0,0,0,1,2,2,3,2,2,2,1,0,0,0,0,
            0,0,0,0,0,1,2,3,2,2,1,0,0,0,0,0,
            0,0,0,0,0,0,1,3,2,1,0,0,0,0,0,0,
            0,0,0,0,0,0,0,1,1,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
        };

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int val = shieldPattern[(h - 1 - y) * w + x];
                if (val == 1) px[y * w + x] = edge;
                else if (val == 2) px[y * w + x] = steel;
                else if (val == 3) px[y * w + x] = bright;
            }
        }

        tex.SetPixels(px);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32);
    }
}
