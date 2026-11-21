using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sempre que o cavaleiro renasce (morte ou queda no abismo), todos os inimigos comuns da cena
/// voltam para a posição original com vida cheia — inclusive os que já tinham sido derrotados.
///
/// Funciona tirando uma "foto" (cópia desativada) de cada inimigo no início da cena.
/// No respawn, TODO inimigo vivo na cena é removido e as fotos são recriadas — assim nunca duplica.
/// Basta ter UM objeto com este script na cena (cópias extras se desligam sozinhas).
/// </summary>
[DefaultExecutionOrder(-1000)]
public class EnemyRespawnManager : MonoBehaviour
{
    private class Entry
    {
        public GameObject template;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Transform parent;
    }

    private static EnemyRespawnManager active;

    private readonly List<Entry> entries = new List<Entry>();
    private readonly List<GameObject> spawned = new List<GameObject>();
    private Transform templateHolder;
    private PlayerBehavior player;
    private int lastRespawnFrame = -1;

    private void Awake()
    {
        if (active != null && active != this)
        {
            enabled = false;
            return;
        }
        active = this;

        // Container desativado: as cópias aqui dentro não rodam Awake/Update.
        var holder = new GameObject("_ModelosDeRespawn");
        holder.SetActive(false);
        templateHolder = holder.transform;

        foreach (var enemy in FindObjectsByType<EnemyBehavior>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var t = enemy.transform;
            var template = Instantiate(enemy.gameObject, templateHolder);
            template.name = enemy.gameObject.name;
            CleanRuntimeLeftovers(template);

            entries.Add(new Entry
            {
                template = template,
                position = t.position,
                rotation = t.rotation,
                scale = t.lossyScale,
                parent = t.parent
            });
        }
    }

    private void Start()
    {
        if (!enabled) return;
        player = FindFirstObjectByType<PlayerBehavior>();
        if (player != null) player.OnPlayerRespawn += RespawnAll;
    }

    private void OnDestroy()
    {
        if (player != null) player.OnPlayerRespawn -= RespawnAll;
        if (active == this) active = null;
    }

    public void RespawnAll()
    {
        if (lastRespawnFrame == Time.frameCount) return; // evita respawn duplo no mesmo frame
        lastRespawnFrame = Time.frameCount;

        // Remove todos os inimigos vivos/morrendo da cena (exceto os modelos guardados).
        foreach (var enemy in FindObjectsByType<EnemyBehavior>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (enemy.transform.IsChildOf(templateHolder)) continue;
            enemy.gameObject.SetActive(false); // some imediatamente
            Destroy(enemy.gameObject);
        }
        spawned.Clear();

        foreach (var e in entries)
        {
            var go = Instantiate(e.template, e.position, e.rotation, e.parent);
            go.name = e.template.name;
            Vector3 parentScale = e.parent != null ? e.parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(e.scale.x / parentScale.x, e.scale.y / parentScale.y, e.scale.z / parentScale.z);
            go.SetActive(true);
            spawned.Add(go);
        }
    }

    /// <summary>Remove HUD de polaridade criado em tempo de execução (o inimigo recria o dele ao acordar).</summary>
    private static void CleanRuntimeLeftovers(GameObject template)
    {
        foreach (Transform child in template.transform)
        {
            if (child.name == "Polarity_HUD") Destroy(child.gameObject);
        }
        var hud = template.GetComponent<EnemyPolarityHUD>();
        if (hud != null) Destroy(hud);
    }
}
