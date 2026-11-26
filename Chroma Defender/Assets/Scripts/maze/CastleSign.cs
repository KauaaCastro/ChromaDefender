using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Labirinto do Castelo — PLACA.
///
/// Modos:
///  - ApontarPrimeiraSala (padrão): calcula sozinha, pela rede de portas, qual porta desta sala leva
///    mais rápido à Primeira Sala e aponta a seta para ela (← / → / ↑ "entre por esta porta").
///  - ApontarSala: igual, mas para a sala escolhida em "Target Room Name".
///  - Manual: seta e texto definidos no Inspector (também serve como placa de dica/charada, com seta "Nenhuma").
///
/// O texto aparece quando o cavaleiro chega perto. Uma seta dourada flutua sobre a placa.
/// </summary>
public class CastleSign : MonoBehaviour
{
    public enum Mode { ApontarPrimeiraSala, ApontarSala, Manual }
    public enum Arrow { Nenhuma, Esquerda, Direita, Cima, Baixo }

    [SerializeField] private Mode mode = Mode.ApontarPrimeiraSala;
    [SerializeField] private string targetRoomName = "";

    [Header("--- Modo Manual ---")]
    [SerializeField] private Arrow manualArrow = Arrow.Direita;
    [SerializeField, TextArea] private string manualText = "";

    [Header("--- Visual ---")]
    [Tooltip("Sprite da placa (a Road Sign 01 da Cainos aponta para a DIREITA e é espelhada para a esquerda).")]
    [SerializeField] private SpriteRenderer signRenderer;
    [SerializeField] private bool signSpriteFacesRight = true;
    [Tooltip("Gira a placa junto com a seta (desligue em placas sem formato de seta).")]
    [SerializeField] private bool flipSignWithArrow = true;
    [SerializeField] private bool showFloatingArrow = true;
    [SerializeField] private float arrowHeight = 2.4f;
    [SerializeField] private Color arrowColor = new Color(1f, 0.82f, 0.3f);
    [SerializeField] private float textRange = 4.5f;
    [SerializeField] private float textHeight = 3.3f;

    private Arrow arrow = Arrow.Nenhuma;
    private string text = "";
    private bool computed;
    private Transform arrowT;
    private SpriteRenderer arrowSr;
    private PlayerBehavior player;

    private static Sprite arrowSprite;

    public void ConfigureManual(Arrow a, string t)
    {
        mode = Mode.Manual;
        manualArrow = a;
        manualText = t;
    }

    public void ConfigureAuto(string targetRoom = "")
    {
        mode = string.IsNullOrEmpty(targetRoom) ? Mode.ApontarPrimeiraSala : Mode.ApontarSala;
        targetRoomName = targetRoom;
    }

    private void Start()
    {
        if (showFloatingArrow)
        {
            var go = new GameObject("SetaFlutuante");
            go.transform.SetParent(transform, false);
            arrowT = go.transform;
            arrowSr = go.AddComponent<SpriteRenderer>();
            arrowSr.sprite = GetArrowSprite();
            arrowSr.sortingOrder = signRenderer != null ? signRenderer.sortingOrder + 1 : 20;
            arrowSr.color = arrowColor;
        }
    }

    private void Update()
    {
        if (!computed) { Compute(); computed = true; }

        if (arrowT != null)
        {
            bool show = arrow != Arrow.Nenhuma;
            arrowSr.enabled = show;
            if (show)
            {
                float t = Time.time * 3f;
                Vector3 dir = ArrowVector(arrow);
                arrowT.localPosition = new Vector3(0f, arrowHeight, 0f) + dir * (Mathf.Sin(t) * 0.12f);
                arrowT.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                arrowT.localScale = Vector3.one * 2.2f;
                arrowSr.color = new Color(arrowColor.r, arrowColor.g, arrowColor.b, 0.75f + 0.25f * Mathf.Sin(t * 1.3f));
            }
        }
    }

    /// <summary>Recalcula a direção (chame se mudar portas em tempo de jogo).</summary>
    public void Compute()
    {
        if (mode == Mode.Manual)
        {
            arrow = manualArrow;
            text = manualText;
        }
        else
        {
            var target = mode == Mode.ApontarPrimeiraSala ? CastleMazeRoom.StartRoom : CastleMazeRoom.Find(targetRoomName);
            var here = CastleMazeRoom.At(transform.position + Vector3.up);
            string targetName = target != null ? target.RoomName : (mode == Mode.ApontarPrimeiraSala ? "Primeira Sala" : targetRoomName);

            if (target == null || here == null)
            {
                arrow = Arrow.Nenhuma;
                text = target == null ? $"{targetName}: sala não encontrada" : "Esta placa está fora de uma sala do labirinto.";
            }
            else if (here == target)
            {
                arrow = Arrow.Nenhuma;
                text = $"Você está na <color=#FFD54A>{targetName}</color>.";
            }
            else
            {
                CastleDoor first = FirstDoorTowards(here, target, out int steps);
                if (first == null)
                {
                    arrow = Arrow.Nenhuma;
                    text = $"Nenhum caminho conhecido até a {targetName}...";
                }
                else
                {
                    float dx = first.transform.position.x - transform.position.x;
                    arrow = Mathf.Abs(dx) < 1.3f ? Arrow.Cima : (dx < 0f ? Arrow.Esquerda : Arrow.Direita);
                    string symbol = arrow == Arrow.Esquerda ? "←" : arrow == Arrow.Direita ? "→" : "↑";
                    string extra = arrow == Arrow.Cima ? "\n<size=12>(entre por esta porta)</size>" : "";
                    string dist = steps <= 1 ? "" : $"\n<size=12>{steps} portas de distância</size>";
                    text = $"{symbol} <color=#FFD54A>{targetName}</color>{extra}{dist}";
                }
            }
        }

        if (signRenderer != null && flipSignWithArrow)
        {
            bool left = arrow == Arrow.Esquerda;
            signRenderer.flipX = signSpriteFacesRight ? left : !left;
        }
    }

    /// <summary>Busca em largura pelas portas: devolve a porta DESTA sala que inicia o caminho mais curto.</summary>
    private static CastleDoor FirstDoorTowards(CastleMazeRoom from, CastleMazeRoom to, out int steps)
    {
        steps = 0;
        var firstDoor = new Dictionary<CastleMazeRoom, CastleDoor>();
        var depth = new Dictionary<CastleMazeRoom, int>();
        var queue = new Queue<CastleMazeRoom>();
        depth[from] = 0;
        queue.Enqueue(from);

        // Agrupa as portas por sala uma vez só.
        var doorsByRoom = new Dictionary<CastleMazeRoom, List<CastleDoor>>();
        foreach (var d in CastleDoor.All)
        {
            if (d == null) continue;
            var r = d.Room;
            if (r == null) continue;
            if (!doorsByRoom.TryGetValue(r, out var list)) doorsByRoom[r] = list = new List<CastleDoor>();
            list.Add(d);
        }

        while (queue.Count > 0)
        {
            var room = queue.Dequeue();
            if (!doorsByRoom.TryGetValue(room, out var doors)) continue;

            // Prefere portas comuns às "do Início" quando o caminho tem o mesmo tamanho (fica mais natural).
            doors.Sort((a, b) => (a.DestinationMode == CastleDoor.Destination.InicioDoLabirinto ? 1 : 0)
                               - (b.DestinationMode == CastleDoor.Destination.InicioDoLabirinto ? 1 : 0));

            foreach (var d in doors)
            {
                var next = d.DestinationRoom;
                if (next == null || depth.ContainsKey(next)) continue;
                depth[next] = depth[room] + 1;
                firstDoor[next] = room == from ? d : firstDoor[room];
                if (next == to) { steps = depth[next]; return firstDoor[next]; }
                queue.Enqueue(next);
            }
        }
        return null;
    }

    private static Vector3 ArrowVector(Arrow a)
    {
        switch (a)
        {
            case Arrow.Esquerda: return Vector3.left;
            case Arrow.Cima: return Vector3.up;
            case Arrow.Baixo: return Vector3.down;
            default: return Vector3.right;
        }
    }

    /// <summary>Seta em pixel art (aponta para a direita), gerada por código — não depende de asset.</summary>
    private static Sprite GetArrowSprite()
    {
        if (arrowSprite != null) return arrowSprite;
        string[] rows =
        {
            "....##....",
            "....###...",
            "########..",
            "#########.",
            "##########",
            "#########.",
            "########..",
            "....###...",
            "....##....",
        };
        int w = rows[0].Length, h = rows.Length;
        var tex = new Texture2D(w + 2, h + 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < h + 2; y++) for (int x = 0; x < w + 2; x++) tex.SetPixel(x, y, clear);
        // contorno escuro + miolo branco (a cor vem do SpriteRenderer)
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (rows[h - 1 - y][x] == '#')
                    for (int oy = -1; oy <= 1; oy++)
                        for (int ox = -1; ox <= 1; ox++)
                            if (tex.GetPixel(x + 1 + ox, y + 1 + oy).a < 0.5f) tex.SetPixel(x + 1 + ox, y + 1 + oy, new Color(0.15f, 0.1f, 0.05f, 1f));
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (rows[h - 1 - y][x] == '#') tex.SetPixel(x + 1, y + 1, Color.white);
        tex.Apply();
        arrowSprite = Sprite.Create(tex, new Rect(0, 0, w + 2, h + 2), new Vector2(0.5f, 0.5f), 32f);
        arrowSprite.name = "SetaLabirinto";
        return arrowSprite;
    }

    private void OnGUI()
    {
        if (player == null) player = FindAnyObjectByType<PlayerBehavior>();
        if (player == null || string.IsNullOrEmpty(text)) return;
        if (Vector2.Distance(player.transform.position, transform.position) > textRange) return;
        MazeGui.WorldLabel(transform.position + Vector3.up * textHeight, text, 15, new Color(0.96f, 0.92f, 0.82f), 300f);
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        string label = mode == Mode.Manual ? $"Placa: {manualArrow}" : mode == Mode.ApontarPrimeiraSala ? "Placa → Primeira Sala (auto)" : $"Placa → {targetRoomName} (auto)";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.2f, label);
#endif
    }
}
