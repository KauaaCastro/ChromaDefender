using UnityEngine;

/// <summary>
/// Coloque este script em um item na fase (baú, cristal ou orbe).
/// Quando o jogador encosta nele, desbloqueia um golpe ou melhoria e toca efeito.
/// </summary>
public class UpgradePickup : MonoBehaviour
{
    public enum UpgradeType
    {
        UnlockHeavyAttack1,
        UnlockHeavyComboFull,
        UpgradeLightCombo3,
        UpgradeLightCombo4,
        UnlockWeaponArt
    }

    [Header("Configuração do Upgrade")]
    [SerializeField] private UpgradeType upgradeType = UpgradeType.UnlockHeavyAttack1;
    [SerializeField] private string upgradeMessage = "Ataque Pesado Desbloqueado! (Botão Direito do Mouse)";

    [Header("Visual")]
    [SerializeField] private GameObject pickupEffectPrefab;
    [SerializeField] private bool destroyOnPickup = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerBehavior player = other.GetComponent<PlayerBehavior>();
        if (player == null)
        {
            player = other.GetComponentInParent<PlayerBehavior>();
        }

        if (player != null)
        {
            ApplyUpgrade(player);

            if (pickupEffectPrefab != null)
            {
                Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            }

            Debug.Log($"<color=cyan>[UPGRADE COLETADO]</color> {upgradeMessage}");

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
    }

    private void ApplyUpgrade(PlayerBehavior player)
    {
        switch (upgradeType)
        {
            case UpgradeType.UnlockHeavyAttack1:
                player.UnlockHeavyAttack(1);
                break;
            case UpgradeType.UnlockHeavyComboFull:
                player.UnlockHeavyAttack(3);
                break;
            case UpgradeType.UpgradeLightCombo3:
                player.UpgradeLightCombo(3);
                break;
            case UpgradeType.UpgradeLightCombo4:
                player.UpgradeLightCombo(4);
                break;
            case UpgradeType.UnlockWeaponArt:
                player.UnlockWeaponArt();
                break;
        }
    }
}
