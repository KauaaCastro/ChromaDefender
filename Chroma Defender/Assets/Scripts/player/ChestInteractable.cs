using System.Collections;
using UnityEngine;
using Cainos.PixelArtPlatformer_VillageProps;

public enum ChestRewardType
{
    UnlockHeavyAttack,
    UpgradeLightCombo3,
    FullHeal,
    CastleKey,
    SecretDemoTreasure
}

/// <summary>
/// Baú interativo que pode ser aberto por ataque de espada ou interagindo próximo a ele.
/// Ao abrir, aciona o upgrade no jogador e toca efeitos de abertura.
/// </summary>
public class ChestInteractable : MonoBehaviour, IDamageableTarget
{
    [Header("--- Tipo de Recompensa ---")]
    [SerializeField] private ChestRewardType rewardType = ChestRewardType.UnlockHeavyAttack;

    [Header("--- Feedback & Efeitos ---")]
    [SerializeField] private Color rewardSparkleColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private GameObject spawnedPickupPrefab;

    private Chest cainosChest;
    private Animator anim;
    private Collider2D col;
    private bool isOpened = false;

    private void Awake()
    {
        cainosChest = GetComponent<Chest>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isOpened) return;

        PlayerBehavior player = other.GetComponent<PlayerBehavior>();
        if (player == null) player = other.GetComponentInParent<PlayerBehavior>();

        if (player != null && Input.GetKeyDown(KeyCode.E))
        {
            OpenChest(player);
        }
    }

    public void TakeDamage(int damage, Vector2 knockback, ElementPolarity attackPolarity)
    {
        if (isOpened) return;

        PlayerBehavior player = FindFirstObjectByType<PlayerBehavior>();
        OpenChest(player);
    }

    public void OpenChest(PlayerBehavior player)
    {
        if (isOpened) return;
        isOpened = true;

        // Animação da Cainos ou Animator direto
        if (cainosChest != null)
        {
            cainosChest.Open();
        }
        else if (anim != null)
        {
            anim.SetBool("IsOpened", true);
        }

        // Partículas douradas de abertura
        FireVFX.SpawnFlameBurst(transform.position + Vector3.up * 0.5f, 22);

        // Aplica a recompensa no jogador
        if (player != null)
        {
            ApplyReward(player);
        }
    }

    private void ApplyReward(PlayerBehavior player)
    {
        switch (rewardType)
        {
            case ChestRewardType.UnlockHeavyAttack:
                player.UnlockHeavyAttack(1);
                Debug.Log("<color=gold>[BAÚ ABERTO]</color> Você encontrou a Técnica do <color=orange>Ataque Pesado</color>! (Pressione o botão de Ataque Pesado)");
                break;

            case ChestRewardType.UpgradeLightCombo3:
                player.UpgradeLightCombo(3);
                Debug.Log("<color=gold>[BAÚ ABERTO]</color> Combo Leve melhorado para <color=orange>3 golpes</color>!");
                break;

            case ChestRewardType.CastleKey:
                player.CollectCastleKey();
                break;

            case ChestRewardType.FullHeal:
                Debug.Log("<color=green>[BAÚ ABERTO]</color> Elixir Restaurador encontrado! Vida totalmente regenerada!");
                break;

            case ChestRewardType.SecretDemoTreasure:
                Debug.Log("<color=yellow>[CÂMARA SECRETA]</color> 🏆 Parabéns! Você encontrou o Grande Tesouro Secreto da Demo de Chroma Defender!");
                break;
        }

        if (spawnedPickupPrefab != null)
        {
            Instantiate(spawnedPickupPrefab, transform.position + Vector3.up * 0.7f, Quaternion.identity);
        }
    }
}
