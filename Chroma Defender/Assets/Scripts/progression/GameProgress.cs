using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Progresso do jogador que sobrevive à troca de cenas (chaves, chefes derrotados, portões abertos).
/// Fica só na memória durante a sessão; o Menu Inicial zera tudo ao clicar em "Jogar".
///
/// Fluxo planejado:
///  Fase 1 → derrota o Guardião (boss) → ganha a Chave do Castelo → entra no Castelo (Fase 2)
///  Fase 2 → derrota o Mestre de Armas → ganha a Chave do Poço → volta à Fase 1 → abre o cadeado do Poço → Área Mágica (Fase 3)
///  Fase 3 → derrota o Arquimago do Caos → tela de vitória
/// </summary>
public static class GameProgress
{
    // --- Identificadores de chaves ---
    public const string KeyCastelo = "chave_castelo";
    public const string KeyPoco = "chave_poco";
    public const string KeyLabirinto = "chave_labirinto";
    public const string KeyAreaB2 = "chave_area_b2";
    public const string KeyRunica = "chave_runica";

    // --- Flags de mundo ---
    public const string FlagBossFase1 = "boss_fase1_derrotado";
    public const string FlagBossFase2 = "boss_fase2_derrotado";
    public const string FlagBossFase3 = "boss_fase3_derrotado";

    // --- Nomes das cenas ---
    public const string SceneFase1 = "SampleScene";
    public const string SceneFase2 = "Fase2_Castelo";
    public const string SceneFase3 = "Fase3_AreaMagica";

    // --- Pontos de spawn entre cenas ---
    public const string SpawnFase1Castelo = "fase1_saida_castelo";
    public const string SpawnFase2Entrada = "fase2_entrada";
    public const string SpawnFase3Entrada = "fase3_entrada";

    private static readonly HashSet<string> keys = new HashSet<string>();
    private static readonly HashSet<string> flags = new HashSet<string>();

    /// <summary>Id do SpawnPoint onde o jogador deve aparecer na próxima cena carregada.</summary>
    public static string PendingSpawnId;

    /// <summary>Disparado quando chaves ou flags mudam (usado pela HUD de progresso).</summary>
    public static event Action OnChanged;

    /// <summary>Golpes do combo leve liberados por baús/itens (0 = nenhuma melhoria). O jogador aplica ao entrar na cena.</summary>
    public static int LightComboHits { get; private set; }

    /// <summary>Golpes do ataque pesado liberados (0 = bloqueado, 1..3 = desbloqueado com N golpes).</summary>
    public static int HeavyComboHits { get; private set; }

    /// <summary>Indica se o Ataque Especial / Supremo (Weapon Art, teclas F/E) foi desbloqueado.</summary>
    public static bool WeaponArtUnlocked { get; private set; }

    public static void UnlockLightCombo(int hits)
    {
        if (hits <= LightComboHits) return;
        LightComboHits = hits;
        OnChanged?.Invoke();
    }

    public static void UnlockHeavyCombo(int hits = 1)
    {
        int clamped = Mathf.Clamp(hits, 1, 3);
        if (clamped <= HeavyComboHits) return;
        HeavyComboHits = clamped;
        OnChanged?.Invoke();
    }

    public static void UnlockWeaponArt()
    {
        if (WeaponArtUnlocked) return;
        WeaponArtUnlocked = true;
        OnChanged?.Invoke();
    }

    /// <summary>Bônus de dano (%) aplicado a todos os golpes do cavaleiro (ex.: 15 = +15%).</summary>
    public static int DamageBonusPercent { get; private set; }

    /// <summary>Multiplicador de dano derivado do bônus (1.15 para +15%).</summary>
    public static float DamageMultiplier => 1f + DamageBonusPercent / 100f;

    public static void UnlockDamageBonus(int percent)
    {
        if (percent <= DamageBonusPercent) return;
        DamageBonusPercent = percent;
        OnChanged?.Invoke();
    }

    public static IEnumerable<string> Keys => keys;

    public static bool HasKey(string keyId) => !string.IsNullOrEmpty(keyId) && keys.Contains(keyId);

    public static void AddKey(string keyId)
    {
        if (string.IsNullOrEmpty(keyId) || !keys.Add(keyId)) return;
        Debug.Log($"<color=gold>[Progresso]</color> Chave obtida: {KeyDisplayName(keyId)}");
        OnChanged?.Invoke();
    }

    public static bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

    public static void SetFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag) || !flags.Add(flag)) return;
        OnChanged?.Invoke();
    }

    public static void ResetProgress()
    {
        keys.Clear();
        flags.Clear();
        LightComboHits = 0;
        HeavyComboHits = 0;
        WeaponArtUnlocked = false;
        DamageBonusPercent = 0;
        PendingSpawnId = null;
        OnChanged?.Invoke();
    }

    public static string KeyDisplayName(string keyId)
    {
        switch (keyId)
        {
            case KeyCastelo: return "Chave Dourada do Castelo";
            case KeyPoco: return "Chave de Prata do Poço";
            case KeyLabirinto: return "Chave de Ferro do Labirinto";
            case KeyAreaB2: return "Chave da Área B2";
            case KeyRunica: return "Chave Rúnica";
            default: return keyId;
        }
    }

    // Garante estado limpo ao entrar no Play Mode mesmo com "Domain Reload" desativado no Editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        keys.Clear();
        flags.Clear();
        LightComboHits = 0;
        HeavyComboHits = 0;
        WeaponArtUnlocked = false;
        DamageBonusPercent = 0;
        PendingSpawnId = null;
        OnChanged = null;
    }
}
