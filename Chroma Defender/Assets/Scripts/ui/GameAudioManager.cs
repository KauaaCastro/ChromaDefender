using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sistema completo de Áudio do Chroma Defender (Requisito Mínimo nº 7 da Etapa 5).
/// Carrega e utiliza diretamente os arquivos .wav da pasta:
///   "Assets/Gregor Quendel - Designed Series - Free Cinematic Sound Effects"
/// para a Trilha Sonora (Designed Atmospheres) e todos os Efeitos Sonoros (Designed Fire,
/// Designed Ice, Designed Mecha, Designed Rocks, Designed Plastic e Designed Sci-Fi),
/// recortando o silêncio inicial dos efeitos longos para resposta imediata nos golpes.
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    private const string BaseDir = "Assets/Gregor Quendel - Designed Series - Free Cinematic Sound Effects/";
    private const string AtmosDir = BaseDir + "Gregor Quendel - Designed Atmospheres - Free Sounds/";
    private const string FireDir = BaseDir + "Gregor Quendel - Designed Fire - Free Sounds/";
    private const string IceDir = BaseDir + "Gregor Quendel - Designed Ice - Free Sounds/";
    private const string MechaDir = BaseDir + "Gregor Quendel - Designed Mecha - Free Sounds/";
    private const string PlasticDir = BaseDir + "Gregor Quendel - Designed Plastic - Free Sounds/";
    private const string RocksDir = BaseDir + "Gregor Quendel - Designed Rocks - Free Sounds/";
    private const string SciFiTexDir = BaseDir + "Gregor Quendel - Designed Sci-Fi - Free Sounds/01_Textures/";
    private const string SciFiFxDir = BaseDir + "Gregor Quendel - Designed Sci-Fi - Free Sounds/02_Effects/";

    [Header("--- Volumes ---")]
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.38f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.75f;
    [SerializeField] private bool muted;

    [Header("--- Clipes Carregados da Pasta Assets (Gregor Quendel) ---")]
    [SerializeField] private AudioClip bgmAtmosphereClip;
    [SerializeField] private AudioClip fireSlashAsset;
    [SerializeField] private AudioClip iceSlashAsset;
    [SerializeField] private AudioClip heavyImpactAsset;
    [SerializeField] private AudioClip heavyIceImpactAsset;
    [SerializeField] private AudioClip fireSwitchAsset;
    [SerializeField] private AudioClip iceSwitchAsset;
    [SerializeField] private AudioClip jumpAsset;
    [SerializeField] private AudioClip rollAsset;
    [SerializeField] private AudioClip hurtAsset;
    [SerializeField] private AudioClip healAsset;
    [SerializeField] private AudioClip deathAsset;
    [SerializeField] private AudioClip victoryAsset;
    [SerializeField] private AudioClip respawnAsset;
    [SerializeField] private AudioClip rewardAsset;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private AudioClip bgmClip;
    private AudioClip fireSlashClip;
    private AudioClip iceSlashClip;
    private AudioClip heavyFireSlashClip;
    private AudioClip heavyIceSlashClip;
    private AudioClip fireSwitchClip;
    private AudioClip iceSwitchClip;
    private AudioClip jumpClip;
    private AudioClip rollClip;
    private AudioClip hurtClip;
    private AudioClip healClip;
    private AudioClip deathClip;
    private AudioClip victoryClip;
    private AudioClip respawnClip;
    private AudioClip rewardClip;

    private PlayerBehavior boundPlayer;
    private int lastPlayerHealth = -1;
    private bool inputHooked;

    public bool IsMuted => muted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot() => EnsureInstance();

    public static GameAudioManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        var existing = FindFirstObjectByType<GameAudioManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        var prefab = Resources.Load<GameObject>("GameAudioManager");
        if (prefab != null)
        {
            var inst = Instantiate(prefab);
            inst.name = "[GameAudioManager]";
            DontDestroyOnLoad(inst);
            Instance = inst.GetComponent<GameAudioManager>();
            return Instance;
        }

        var go = new GameObject("[GameAudioManager]");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<GameAudioManager>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;
        bgmSource.volume = bgmVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = sfxVolume;

        LoadAssetsFromProject();
        PrepareAllClips();
        StartBgm();
    }

    private void LoadAssetsFromProject()
    {
#if UNITY_EDITOR
        if (bgmAtmosphereClip == null)
            bgmAtmosphereClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AtmosDir + "Gregor Quendel - Designed Atmospheres - Harmonic - 018.wav");
        if (fireSlashAsset == null)
            fireSlashAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(FireDir + "Gregor Quendel - Designed Fire - Short - Swoosh Burst Sizzling 01.wav");
        if (iceSlashAsset == null)
            iceSlashAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(IceDir + "Gregor Quendel - Designed Ice - Sequences 01.wav");
        if (heavyImpactAsset == null)
            heavyImpactAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(FireDir + "Gregor Quendel - Designed Fire - Impacts - Complex 04.wav");
        if (heavyIceImpactAsset == null)
            heavyIceImpactAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(IceDir + "Gregor Quendel - Designed Ice - Sequences 15.wav");
        if (fireSwitchAsset == null)
            fireSwitchAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(FireDir + "Gregor Quendel - Designed Fire - Swooshes - Burst Evolving 19.wav");
        if (iceSwitchAsset == null)
            iceSwitchAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(IceDir + "Gregor Quendel - Designed Ice - Sequences 02.wav");
        if (jumpAsset == null)
            jumpAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(PlasticDir + "Gregor Quendel - Designed Plastic - Elements - Sfx 03.wav");
        if (rollAsset == null)
            rollAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(FireDir + "Gregor Quendel - Designed Fire - Winds - Swoosh 04.wav");
        if (hurtAsset == null)
            hurtAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(MechaDir + "Gregor Quendel - Designed Mecha - Elements - Impact 01.wav");
        if (healAsset == null)
            healAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(SciFiFxDir + "Gregor Quendel - Designed Sci-Fi - Science_07.wav");
        if (deathAsset == null)
            deathAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(AtmosDir + "Gregor Quendel - Designed Atmospheres - Sub - 003 - III.wav");
        if (victoryAsset == null)
            victoryAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(SciFiTexDir + "Gregor Quendel - Designed Sci-Fi - MusicOfTheSpheres_PrettyScape.wav");
        if (respawnAsset == null)
            respawnAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(SciFiTexDir + "Gregor Quendel - Designed Sci-Fi - AlienChime.wav");
        if (rewardAsset == null)
            rewardAsset = AssetDatabase.LoadAssetAtPath<AudioClip>(SciFiFxDir + "Gregor Quendel - Designed Sci-Fi - Summon_01.wav");
#endif
    }

    private void PrepareAllClips()
    {
        const int fallbackRate = 44100;

        // Trilha Sonora (BGM): usa diretamente o arquivo Harmonic - 018.wav da pasta Designed Atmospheres
        bgmClip = bgmAtmosphereClip != null ? bgmAtmosphereClip : BuildFallbackBgm(fallbackRate);

        // Efeitos Sonoros (SFX): recorta o trecho de impacto dos arquivos .wav da pasta Assets para resposta instantânea
        fireSlashClip = TrimAndEnvelopeAsset(fireSlashAsset, "SFX_FireSlash_Asset", 0.36f, 0.45f) ?? BuildFallbackFireSlash(fallbackRate);
        iceSlashClip = TrimAndEnvelopeAsset(iceSlashAsset, "SFX_IceSlash_Asset", 0.38f, 0.45f) ?? BuildFallbackIceSlash(fallbackRate);
        heavyFireSlashClip = TrimAndEnvelopeAsset(heavyImpactAsset, "SFX_HeavyFireImpact_Asset", 0.65f, 0.50f) ?? fireSlashClip;
        heavyIceSlashClip = TrimAndEnvelopeAsset(heavyIceImpactAsset != null ? heavyIceImpactAsset : iceSlashAsset, "SFX_HeavyIceImpact_Asset", 0.65f, 0.50f) ?? iceSlashClip;
        fireSwitchClip = TrimAndEnvelopeAsset(fireSwitchAsset, "SFX_FireSwitch_Asset", 0.55f, 0.50f) ?? fireSlashClip;
        iceSwitchClip = TrimAndEnvelopeAsset(iceSwitchAsset, "SFX_IceSwitch_Asset", 0.55f, 0.50f) ?? iceSlashClip;
        jumpClip = TrimAndEnvelopeAsset(jumpAsset, "SFX_Jump_Asset", 0.26f, 0.45f) ?? fireSlashClip;
        rollClip = TrimAndEnvelopeAsset(rollAsset, "SFX_Roll_Asset", 0.32f, 0.45f) ?? fireSlashClip;
        hurtClip = TrimAndEnvelopeAsset(hurtAsset, "SFX_Hurt_Asset", 0.45f, 0.50f) ?? heavyFireSlashClip;
        healClip = TrimAndEnvelopeAsset(healAsset, "SFX_Heal_Asset", 0.65f, 0.45f) ?? iceSwitchClip;
        deathClip = TrimAndEnvelopeAsset(deathAsset, "SFX_GameOver_Sub003", 2.80f, 0.55f, 0.20f) ?? heavyFireSlashClip;
        victoryClip = TrimAndEnvelopeAsset(victoryAsset, "SFX_Victory_PrettyScape", 4.50f, 0.60f, 0.18f) ?? healClip;
        respawnClip = TrimAndEnvelopeAsset(respawnAsset, "SFX_Respawn_Asset", 0.90f, 0.50f) ?? healClip;
        rewardClip = TrimAndEnvelopeAsset(rewardAsset, "SFX_Reward_Asset", 1.15f, 0.50f) ?? healClip;
    }

    /// <summary>
    /// Lê os dados de áudio do AudioClip da pasta Assets, remove eventual silêncio no início
    /// e aplica um fade-out suave na duração ideal para gameplay rápida.
    /// </summary>
    private static AudioClip TrimAndEnvelopeAsset(AudioClip source, string newName, float maxDurationSec, float fadeStartRatio, float startThresholdRatio = 0.06f)
    {
        if (source == null || source.samples <= 0) return null;
        if (source.loadState != AudioDataLoadState.Loaded)
        {
            source.LoadAudioData();
            if (source.loadState != AudioDataLoadState.Loaded) return source;
        }

        int channels = Mathf.Max(1, source.channels);
        int freq = source.frequency;
        int totalFrames = source.samples;

        // Lê até os primeiros 12 segundos do arquivo para encontrar o pico de ataque
        int readFrames = Mathf.Min(totalFrames, freq * 12);
        float[] raw = new float[readFrames * channels];
        if (!source.GetData(raw, 0))
            return source;

        // Converte para mono e acha a amplitude máxima
        float[] mono = new float[readFrames];
        float maxAmp = 0f;
        for (int i = 0; i < readFrames; i++)
        {
            float sum = 0f;
            int baseIdx = i * channels;
            for (int c = 0; c < channels; c++)
                sum += raw[baseIdx + c];
            float val = sum / channels;
            mono[i] = val;
            float abs = Mathf.Abs(val);
            if (abs > maxAmp) maxAmp = abs;
        }

        if (maxAmp < 0.0001f) return source;

        // Encontra onde o som realmente começa (acima de startThresholdRatio do pico) para não ter atraso
        float threshold = maxAmp * startThresholdRatio;
        int startFrame = 0;
        for (int i = 0; i < readFrames; i++)
        {
            if (Mathf.Abs(mono[i]) >= threshold)
            {
                startFrame = Mathf.Max(0, i - Mathf.RoundToInt(freq * 0.01f));
                break;
            }
        }

        int outFrames = Mathf.Min(readFrames - startFrame, Mathf.RoundToInt(maxDurationSec * freq));
        if (outFrames <= 64) return source;

        float[] outSamples = new float[outFrames];
        float normFactor = 0.92f / maxAmp;
        int fadeInSamples = Mathf.Min(outFrames / 10, Mathf.RoundToInt(freq * 0.008f));

        for (int i = 0; i < outFrames; i++)
        {
            float t = i / (float)outFrames;
            float env = 1f;
            if (i < fadeInSamples && fadeInSamples > 0)
                env = i / (float)fadeInSamples;
            else if (t > fadeStartRatio)
            {
                float k = (t - fadeStartRatio) / Mathf.Max(0.01f, 1f - fadeStartRatio);
                env = Mathf.Pow(1f - Mathf.Clamp01(k), 1.6f);
            }

            outSamples[i] = Mathf.Clamp(mono[startFrame + i] * normFactor * env, -0.98f, 0.98f);
        }

        var trimmed = AudioClip.Create(newName, outFrames, 1, freq, false);
        trimmed.SetData(outSamples, 0);
        return trimmed;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameProgress.OnChanged += OnProgressChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameProgress.OnChanged -= OnProgressChanged;
        UnbindPlayer();
        UnhookInput();
    }

    private void Start()
    {
        BindSceneObjects();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindSceneObjects();
        if (!bgmSource.isPlaying && !muted) StartBgm();
    }

    private void Update()
    {
        if (boundPlayer == null && SceneManager.GetActiveScene().name != "MainMenu")
            BindSceneObjects();

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.mKey.wasPressedThisFrame)
        {
            ToggleMute();
            ProgressHud.Toast(muted ? "Áudio: <color=#FF7070>MUDO</color> (Pressione [M] para ativar)" : "Áudio: <color=#7CFF7C>ATIVADO</color>", 2f);
        }
    }

    public void ToggleMute()
    {
        muted = !muted;
        bgmSource.mute = muted;
        sfxSource.mute = muted;
    }

    private void StartBgm()
    {
        bgmSource.clip = bgmClip;
        bgmSource.volume = bgmVolume;
        bgmSource.mute = muted;
        if (bgmSource.clip != null && !bgmSource.isPlaying)
            bgmSource.Play();
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (muted || clip == null || sfxSource == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    // ------------------------------------------------------------------
    // Conexão automática com os eventos do Jogador e Input
    // ------------------------------------------------------------------
    private void BindSceneObjects()
    {
        UnbindPlayer();
        boundPlayer = FindFirstObjectByType<PlayerBehavior>();
        if (boundPlayer != null)
        {
            lastPlayerHealth = boundPlayer.CurrentHealth;
            boundPlayer.OnAttackStarted += HandlePlayerAttack;
            boundPlayer.OnPolarityChanged += HandlePolarityChanged;
            boundPlayer.OnHealthChanged += HandleHealthChanged;
            boundPlayer.OnPlayerDeath += HandlePlayerDeath;
            boundPlayer.OnPlayerRespawn += HandlePlayerRespawn;
        }

        if (!inputHooked && GameManager.Instance != null && GameManager.Instance.InputManager != null)
        {
            var input = GameManager.Instance.InputManager;
            input.OnJump += HandleJump;
            input.OnRoll += HandleRoll;
            inputHooked = true;
        }
    }

    private void UnbindPlayer()
    {
        if (boundPlayer == null) return;
        boundPlayer.OnAttackStarted -= HandlePlayerAttack;
        boundPlayer.OnPolarityChanged -= HandlePolarityChanged;
        boundPlayer.OnHealthChanged -= HandleHealthChanged;
        boundPlayer.OnPlayerDeath -= HandlePlayerDeath;
        boundPlayer.OnPlayerRespawn -= HandlePlayerRespawn;
        boundPlayer = null;
    }

    private void UnhookInput()
    {
        if (!inputHooked || GameManager.Instance == null || GameManager.Instance.InputManager == null) return;
        var input = GameManager.Instance.InputManager;
        input.OnJump -= HandleJump;
        input.OnRoll -= HandleRoll;
        inputHooked = false;
    }

    private void HandlePlayerAttack(PlayerBehavior.AttackType type)
    {
        bool isIce = boundPlayer != null && boundPlayer.CurrentPolarity == ElementPolarity.Ice;
        bool isHeavyOrSpecial = type == PlayerBehavior.AttackType.Heavy1 ||
                                type == PlayerBehavior.AttackType.Heavy2 ||
                                type == PlayerBehavior.AttackType.Heavy3 ||
                                type == PlayerBehavior.AttackType.WeaponArt;

        if (isHeavyOrSpecial)
        {
            AudioClip heavyClip = isIce ? heavyIceSlashClip : heavyFireSlashClip;
            float heavyPitch = type == PlayerBehavior.AttackType.WeaponArt
                ? 0.85f // Supremo soa mais grave e imponente
                : (type == PlayerBehavior.AttackType.Heavy3 ? 1.12f : (type == PlayerBehavior.AttackType.Heavy2 ? 1.04f : 0.96f));
            PlaySfx(heavyClip, 0.95f, heavyPitch);
            return;
        }

        AudioClip clip = isIce ? iceSlashClip : fireSlashClip;

        float pitch = 1f;
        if (type == PlayerBehavior.AttackType.Light2) pitch = 1.08f;
        else if (type == PlayerBehavior.AttackType.Light3) pitch = 1.18f;
        else if (type == PlayerBehavior.AttackType.Light4) pitch = 1.26f;

        PlaySfx(clip, 0.85f, pitch);
    }

    private void HandlePolarityChanged(ElementPolarity polarity)
    {
        PlaySfx(polarity == ElementPolarity.Fire ? fireSwitchClip : iceSwitchClip, 0.9f, 1f);
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (lastPlayerHealth >= 0)
        {
            if (current < lastPlayerHealth && current > 0)
                PlaySfx(hurtClip, 0.9f, Random.Range(0.95f, 1.05f));
            else if (current > lastPlayerHealth && lastPlayerHealth > 0)
                PlaySfx(healClip, 0.75f, 1f);
        }
        lastPlayerHealth = current;
    }

    private void HandlePlayerDeath()
    {
        PlaySfx(deathClip, 1f, 1f);
    }

    public void PlayVictory()
    {
        PlaySfx(victoryClip, 1f, 1f);
    }

    private void HandlePlayerRespawn()
    {
        if (boundPlayer != null) lastPlayerHealth = boundPlayer.CurrentHealth;
        PlaySfx(respawnClip, 0.8f, 1f);
    }

    private void HandleJump()
    {
        if (Time.timeScale <= 0.01f || (boundPlayer != null && boundPlayer.IsDead)) return;
        PlaySfx(jumpClip, 0.55f, Random.Range(0.96f, 1.06f));
    }

    private void HandleRoll()
    {
        if (Time.timeScale <= 0.01f || (boundPlayer != null && boundPlayer.IsDead)) return;
        PlaySfx(rollClip, 0.65f, Random.Range(0.95f, 1.05f));
    }

    private void OnProgressChanged()
    {
        if (Time.timeSinceLevelLoad < 0.25f) return;
        PlaySfx(rewardClip, 0.95f, 1f);
    }

    // ------------------------------------------------------------------
    // Fallbacks leves caso algum asset seja movido de pasta
    // ------------------------------------------------------------------
    private static AudioClip BuildFallbackBgm(int rate)
    {
        int len = rate * 4;
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)rate;
            data[i] = Mathf.Sin(2f * Mathf.PI * 146.83f * t) * 0.15f;
        }
        var clip = AudioClip.Create("Fallback_BGM", len, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip BuildFallbackFireSlash(int rate)
    {
        int len = Mathf.RoundToInt(0.22f * rate);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)len;
            data[i] = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(480f, 140f, t) * (i / (float)rate)) * (1f - t) * 0.5f;
        }
        var clip = AudioClip.Create("Fallback_Fire", len, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip BuildFallbackIceSlash(int rate)
    {
        int len = Mathf.RoundToInt(0.22f * rate);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)len;
            data[i] = Mathf.Sin(2f * Mathf.PI * 1320f * (i / (float)rate)) * (1f - t) * 0.5f;
        }
        var clip = AudioClip.Create("Fallback_Ice", len, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
