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
                ProgressHud.Toast("Nova Técnica! <color=#FFD54A>Ataque Pesado</color>\nClique com o Botão Direito do Mouse para desferir um golpe pesado!", 4.5f);
                break;
            case UpgradeType.UnlockHeavyComboFull:
                player.UnlockHeavyAttack(3);
                ProgressHud.Toast("Nova Técnica! <color=#FFD54A>Combo Pesado Nível 3</color>\nSeu Ataque Pesado agora encadeia 3 golpes devastadores!", 4.5f);
                break;
            case UpgradeType.UpgradeLightCombo3:
                player.UpgradeLightCombo(3);
                ProgressHud.Toast("Nova Técnica! <color=#FFD54A>Combo Leve — 3º Golpe</color>\nSeu ataque rápido agora encadeia 3 golpes!", 4.5f);
                break;
            case UpgradeType.UpgradeLightCombo4:
                player.UpgradeLightCombo(4);
                ProgressHud.Toast("Nova Técnica! <color=#FFD54A>Combo Leve — 4º Golpe</color>\nSeu ataque rápido agora encadeia 4 golpes!", 4.5f);
                break;
            case UpgradeType.UnlockWeaponArt:
                player.UnlockWeaponArt();
                ProgressHud.Toast("Nova Técnica! <color=#FFD54A>Ataque Especial Supremo</color>\nPressione [F] ou [E] para liberar a Habilidade Suprema!", 4.5f);
                break;
        }
    }
}
