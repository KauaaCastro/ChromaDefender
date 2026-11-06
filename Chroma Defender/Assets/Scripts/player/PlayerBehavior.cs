using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema completo de Movimentação, Combate e Progressão do Black Knight.
/// Controla estados, combos, quedas realistas, rolamento, bloqueio/parry e vida.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerBehavior : MonoBehaviour
{
    public enum AttackType { Light1, Light2, Light3, Light4, Heavy1, Heavy2, Heavy3, WeaponArt }
    public enum ActionState { None, Attack, Roll, Parry, Hurt, Dead, Buff }

    // Estados do Animator (compatíveis com PlayerAnimatorBuilder)
    private const string IdleState = "Idle";
    private const string RunState = "Run";
    private const string JumpState = "Jump";
    private const string FallState = "Fall";
    private const string FallLightState = "FallLight";
    private const string FallHeavyState = "FallHeavy";
    private const string LandState = "Land";
    private const string RollState = "Roll";
    private const string Attack1State = "Attack1";
    private const string Attack2State = "Attack2";
    private const string Attack3State = "Attack3";
    private const string Attack4State = "Attack4";
    private const string Heavy1State = "Heavy1";
    private const string Heavy2State = "Heavy2";
    private const string Heavy3State = "Heavy3";
    private const string WeaponArtState = "WeaponArt";
    private const string ParryStartState = "ParryStart";
    private const string ParryState = "Parry";
    private const string BuffState = "Buff";
    private const string HurtState = "Hurt";
    private const string DeathState = "Death";

    [Header("--- Movimentação Base ---")]
    [SerializeField] private float moveSpeed = 5.5f;
    [SerializeField] private float jumpForce = 6.5f;
    [SerializeField] private float groundAcceleration = 65f;
    [SerializeField] private float groundDeceleration = 50f;
    [SerializeField] private float airAcceleration = 35f;
    [SerializeField] private float groundedMaxUpVelocity = 0.1f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("--- Quedas Dinâmicas ---")]
    [Tooltip("Velocidade de descida para ativar a animação de queda pesada com capa.")]
    [SerializeField] private float heavyFallSpeedThreshold = -9f;
    [Tooltip("Impacto mínimo no chão para forçar a animação de impacto/aterrissagem (Land).")]
    [SerializeField] private float heavyLandImpactThreshold = -8f;

    [Header("--- Rolamento (Dash/Esquiva) ---")]
    [SerializeField] private float rollSpeed = 6.5f;
    [SerializeField] private float rollCooldown = 0.4f;
    [SerializeField] private float rollAnimSpeed = 1.4f;
    [Tooltip("Reduz a altura do colisor durante o rolamento para passar por debaixo de inimigos e obstáculos.")]
    [SerializeField] private bool rollShrinksCollider = true;
    [Tooltip("Fração da altura normal do colisor ao rolar (ex: 0.4 = 40% da altura original).")]
    [SerializeField, Range(0.2f, 1f)] private float rollColliderHeightFactor = 0.4f;

    [Header("--- Configuração de Danos do Jogador ---")]
    [Tooltip("Dano do 1º golpe do combo leve.")]
    [SerializeField] private int light1Damage = 20;
    [Tooltip("Dano do 2º golpe do combo leve.")]
    [SerializeField] private int light2Damage = 25;
    [Tooltip("Dano do 3º golpe do combo leve.")]
    [SerializeField] private int light3Damage = 35;
    [Tooltip("Dano do 4º golpe do combo leve (finalizador).")]
    [SerializeField] private int light4Damage = 50;

    [Space(5)]
    [Tooltip("Dano do 1º golpe pesado.")]
    [SerializeField] private int heavy1Damage = 50;
    [Tooltip("Dano do 2º golpe pesado.")]
    [SerializeField] private int heavy2Damage = 70;
    [Tooltip("Dano do 3º golpe pesado (finalizador devastador).")]
    [SerializeField] private int heavy3Damage = 95;

    [Space(5)]
    [Tooltip("Dano de cada corte contínuo durante a tempestade de golpes do Especial.")]
    [SerializeField] private int specialFlurryDamage = 20;
    [Tooltip("Dano do golpe final explosivo do Especial (Weapon Art).")]
    [SerializeField] private int specialFinisherDamage = 85;

    [Header("--- Força de Recuo (Knockback) ---")]
    [Tooltip("Recuo sutil dos primeiros golpes leves para manter o inimigo perto.")]
    [SerializeField] private float lightKnockbackForce = 1.0f;
    [Tooltip("Recuo do finalizador leve (Light 4).")]
    [SerializeField] private float lightFinisherKnockback = 5.5f;
    [Tooltip("Recuo dos golpes pesados iniciais.")]
    [SerializeField] private float heavyKnockbackForce = 2.2f;
    [Tooltip("Recuo do golpe pesado finalizador (Heavy 3).")]
    [SerializeField] private float heavyFinisherKnockback = 7.0f;
    [Tooltip("Recuo do impacto final do Ataque Especial.")]
    [SerializeField] private float specialFinisherKnockback = 8.5f;

    [Header("--- Combate & Animações ---")]
    [SerializeField] private float lightAttackAnimSpeed = 1.35f;
    [SerializeField] private float heavyAttackAnimSpeed = 1.05f;
    [SerializeField, Range(0f, 1f)] private float comboWindowStart = 0.35f;
    [SerializeField, Range(0f, 1f)] private float attackCancelStart = 0.65f;
    [SerializeField, Range(0f, 1f)] private float lightMoveFactor = 0.4f;
    [SerializeField, Range(0f, 1f)] private float heavyMoveFactor = 0.15f;
    [SerializeField] private float lightLungeSpeed = 2.8f;
    [SerializeField] private float heavyLungeSpeed = 4.5f;
    [SerializeField] private float lungeDecay = 16f;

    [Header("--- Progressão do Jogador ---")]
    [Tooltip("Quantos golpes leves o jogador consegue encadear (2 a 4).")]
    [SerializeField, Range(2, 4)] private int unlockedLightHits = 2;
    [Tooltip("Ataque pesado habilitado no Botão Direito?")]
    [SerializeField] private bool heavyAttackUnlocked = false;
    [Tooltip("Quantos golpes pesados o jogador consegue encadear se o pesado estiver liberado (1 a 3).")]
    [SerializeField, Range(1, 3)] private int unlockedHeavyHits = 1;
    [Tooltip("Habilidade suprema (Weapon Art) habilitada?")]
    [SerializeField] private bool weaponArtUnlocked = false;

    [Header("--- Polaridade Elemental (Chroma) ---")]
    [SerializeField] private ElementPolarity currentPolarity = ElementPolarity.Fire;
    [Tooltip("Coloração sutil para manter a armadura escura/preta e destacar o fogo (laranja-dourado).")]
    [SerializeField] private Color fireColor = new Color(1f, 0.75f, 0.65f, 1f); 
    [Tooltip("Coloração sutil para manter a armadura escura/preta e destacar o gelo (ciano gélido sem verde).")]
    [SerializeField] private Color iceColor = new Color(0.65f, 0.85f, 1f, 1f);   
    [SerializeField] private float polarityCooldown = 0.5f;

    [SerializeField, HideInInspector] private GameObject iceAuraObject;
    private GameObject fireAuraObject;

    [Header("--- Efeitos Visuais & VFX ---")]
    [Tooltip("Prefab do corte de espada de fogo.")]
    [SerializeField] private GameObject fireSlashPrefab;
    [Tooltip("Prefab do corte de espada de gelo.")]
    [SerializeField] private GameObject iceSlashPrefab;
    [Tooltip("Prefab ou objeto de efeito de explosão / faísca.")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private Vector2 slashOffset = new Vector2(0.6f, 0f);
    [SerializeField] private Vector2 impactOffset = new Vector2(0.8f, 0f);

    [Header("--- Caixa de Dano do Ataque (Hitbox) ---")]
    [SerializeField] private Vector2 attackHitboxSize = new Vector2(1.6f, 1.2f);
    [SerializeField] private Vector2 attackHitboxOffset = new Vector2(0.85f, 0.1f);
    [SerializeField] private LayerMask enemyLayers;

    [Header("--- Vida & Dano ---")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private float hurtInvulnerabilityTime = 0.6f;

    [Header("--- Chaves & Progressão da Fase ---")]
    [SerializeField] private bool hasCastleKey = false;
    public bool HasCastleKey => hasCastleKey;
    public event Action<bool> OnCastleKeyChanged;

    // Componentes
    private Rigidbody2D rb;
    private IsGroundedChecker isGroundedChecker;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;
    private BoxCollider2D playerBoxCollider;
    private Vector2 defaultColliderSize;
    private Vector2 defaultColliderOffset;
    private readonly List<Collider2D> ignoredRollColliders = new List<Collider2D>();

    // Estados internos
    private ActionState action = ActionState.None;
    private AttackType currentAttack;
    private float actionTime;
    private float actionDuration = 0.5f;
    private float facing = 1f;
    private float lungeVelocity;
    private string currentAnimState;

    // Multi-hit do Ataque Especial (Weapon Art)
    private float nextSpecialHitTime;
    private int specialHitCount;

    // Controle de chão e queda
    private bool isGrounded;
    private float airTime;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float jumpLockout;
    private float maxFallVelocityReached;
    private float landTimer;

    // Filas e Cooldowns
    private bool lightQueued;
    private float lightQueuedTime;
    private bool heavyQueued;
    private float heavyQueuedTime;
    private bool specialQueued;
    private float specialQueuedTime;
    private bool rollQueued;
    private float nextRollTime;
    private bool isParryHeld;
    private float invulnerableUntil;
    private bool specialEffectSpawned;
    private float nextPolaritySwitchTime;

    // Duração aproximada dos clips
    private float attack1Length = 0.6f;
    private float attack2Length = 0.6f;
    private float attack3Length = 0.73f;
    private float attack4Length = 0.73f;
    private float heavy1Length = 1.26f;
    private float heavy2Length = 0.53f;
    private float heavy3Length = 0.86f;
    private float weaponArtLength = 2.33f;
    private float rollLength = 0.8f;
    private float landLength = 0.4f;

    // Propriedades Públicas
    public bool IsAttacking => action == ActionState.Attack;
    public bool IsRolling => action == ActionState.Roll;
    public bool IsParrying => action == ActionState.Parry;
    public bool IsDead => action == ActionState.Dead;
    public bool IsInvulnerable => IsRolling || Time.time < invulnerableUntil;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public ElementPolarity CurrentPolarity => currentPolarity;
    public event Action<ElementPolarity> OnPolarityChanged;

    public event Action<AttackType> OnAttackStarted;
    public event Action<int, int> OnHealthChanged; // (current, max)
    public event Action OnPlayerDeath;

    private float Progress => actionDuration > 0f ? Mathf.Clamp01(actionTime / actionDuration) : 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        isGroundedChecker = GetComponent<IsGroundedChecker>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        playerBoxCollider = GetComponent<BoxCollider2D>();
        playerCollider = playerBoxCollider != null ? (Collider2D)playerBoxCollider : GetComponent<Collider2D>();
        if (playerBoxCollider != null)
        {
            defaultColliderSize = playerBoxCollider.size;
            defaultColliderOffset = playerBoxCollider.offset;
        }

        currentHealth = maxHealth;
        RemoveAuras();
        ApplyPolarityVisuals();
    }

    private void Start()
    {
        var input = GameManager.Instance.InputManager;
        input.OnJump += HandleJumpInput;
        input.OnLightAttack += HandleLightAttackInput;
        input.OnHeavyAttack += HandleHeavyAttackInput;
        input.OnSpecialAttack += HandleSpecialAttackInput;
        input.OnRoll += HandleRollInput;
        input.OnParryStarted += HandleParryStart;
        input.OnParryCanceled += HandleParryEnd;
        input.OnSwitchPolarity += HandleSwitchPolarityInput;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null || GameManager.Instance.InputManager == null) return;
        var input = GameManager.Instance.InputManager;
        input.OnJump -= HandleJumpInput;
        input.OnLightAttack -= HandleLightAttackInput;
        input.OnHeavyAttack -= HandleHeavyAttackInput;
        input.OnSpecialAttack -= HandleSpecialAttackInput;
        input.OnRoll -= HandleRollInput;
        input.OnParryStarted -= HandleParryStart;
        input.OnParryCanceled -= HandleParryEnd;
        input.OnSwitchPolarity -= HandleSwitchPolarityInput;
    }

    // ------------------------------------------------------------------
    // Eventos de Entrada
    // ------------------------------------------------------------------
    private void HandleJumpInput() => jumpBufferTimer = jumpBufferTime;
    private void HandleLightAttackInput() { lightQueued = true; lightQueuedTime = Time.time; }
    private void HandleHeavyAttackInput() { heavyQueued = true; heavyQueuedTime = Time.time; }
    private void HandleSpecialAttackInput() 
    { 
        specialQueued = true; 
        specialQueuedTime = Time.time; 
    }
    private void HandleRollInput() => rollQueued = true;
    private void HandleParryStart() => isParryHeld = true;
    private void HandleParryEnd() => isParryHeld = false;
    private void HandleSwitchPolarityInput() => SwitchPolarity();

    // ------------------------------------------------------------------
    // Loop Principal (Update)
    // ------------------------------------------------------------------
    private void Update()
    {
        if (IsDead) return;

        float dt = Time.deltaTime;
        float moveInput = GameManager.Instance.InputManager.Movement;

        TickTimers(dt);
        UpdateGrounded(dt);
        UpdateAction(dt);
        ProcessQueues();
        UpdateMovement(moveInput, dt);
        UpdateFacing(moveInput);
        UpdateAnimation(moveInput);
    }

    private void TickTimers(float dt)
    {
        jumpBufferTimer -= dt;
        landTimer -= dt;
    }

    private void UpdateGrounded(float dt)
    {
        jumpLockout -= dt;

        // Quando o jogador está subindo rápido pelo pulo, NUNCA está no chão
        bool isMovingUpwards = rb.linearVelocity.y > 0.1f;

        bool touchingGround = isGroundedChecker != null
                              && jumpLockout <= 0f
                              && !isMovingUpwards
                              && isGroundedChecker.IsGrounded();

        if (touchingGround)
        {
            // Detecta aterrissagem pesada
            if (airTime > 0.15f && maxFallVelocityReached <= heavyLandImpactThreshold && action == ActionState.None)
            {
                landTimer = landLength;
            }

            airTime = 0f;
            maxFallVelocityReached = 0f;
            coyoteTimer = coyoteTime;
        }
        else
        {
            airTime += dt;
            coyoteTimer -= dt;
            if (rb.linearVelocity.y < maxFallVelocityReached)
            {
                maxFallVelocityReached = rb.linearVelocity.y;
            }
        }

        isGrounded = touchingGround;
    }

    private void UpdateAction(float dt)
    {
        if (action == ActionState.None) return;

        actionTime += dt;

        // Multi-hit do Ataque Especial (Weapon Art)
        if (action == ActionState.Attack && currentAttack == AttackType.WeaponArt)
        {
            // Golpes contínuos durante a tempestade de cortes
            if (actionTime >= nextSpecialHitTime && actionTime < actionDuration * 0.82f)
            {
                specialHitCount++;
                nextSpecialHitTime = actionTime + 0.30f;
                PerformSpecialFlurryHit(specialHitCount);
            }

            // Golpe final com explosão de impacto
            if (!specialEffectSpawned && Progress >= 0.82f)
            {
                SpawnImpactEffect();
                PerformSpecialFinisherHit();
                specialEffectSpawned = true;
            }
        }

        if (actionTime >= actionDuration)
        {
            // Se estava em Parry e continua segurando, permanece na postura
            if (action == ActionState.Parry && isParryHeld)
            {
                PlayAnimation(ParryState, 1f);
                return;
            }

            EndAction();
        }
    }

    private void EndAction()
    {
        if (action == ActionState.Roll)
        {
            // Restaura o tamanho original do colisor
            if (rollShrinksCollider && playerBoxCollider != null)
            {
                playerBoxCollider.size = defaultColliderSize;
                playerBoxCollider.offset = defaultColliderOffset;
            }

            // Restaura colisões com inimigos
            IgnoreEnemiesCollision(false);
        }

        action = ActionState.None;
        actionTime = 0f;
        if (animator != null) animator.speed = 1f;
    }

    // ------------------------------------------------------------------
    // Máquina de Ações e Filas de Entrada
    // ------------------------------------------------------------------
    private void ProcessQueues()
    {
        bool canInterrupt = action == ActionState.None || (action == ActionState.Attack && Progress >= attackCancelStart);

        // 1. Parry / Defesa
        if (isParryHeld && isGrounded && canInterrupt && action != ActionState.Roll)
        {
            if (action != ActionState.Parry)
            {
                StartParry();
                return;
            }
        }
        else if (action == ActionState.Parry && !isParryHeld)
        {
            EndAction();
        }

        // 2. Rolamento (Dash)
        if (rollQueued && isGrounded && canInterrupt && action != ActionState.Roll && Time.time >= nextRollTime)
        {
            StartRoll();
            rollQueued = false;
            return;
        }
        rollQueued = false;

        // 3. Pulo
        if (jumpBufferTimer > 0f && coyoteTimer > 0f && canInterrupt && action != ActionState.Roll)
        {
            ExecuteJump();
            jumpBufferTimer = 0f;
        }

        if (action == ActionState.Roll) return;

        // Limpeza de buffer de ataque fora de combate
        if (action == ActionState.None)
        {
            if (lightQueued && Time.time - lightQueuedTime > 0.25f) lightQueued = false;
            if (heavyQueued && Time.time - heavyQueuedTime > 0.25f) heavyQueued = false;
            if (specialQueued && Time.time - specialQueuedTime > 0.25f) specialQueued = false;
        }

        // 4. Ataque Especial / Supremo (Tecla F ou E)
        if (specialQueued)
        {
            if (!weaponArtUnlocked)
            {
                specialQueued = false;
                Debug.Log("<color=yellow>[Chroma Defender]</color> O Ataque Especial está bloqueado! Marque 'Weapon Art Unlocked' no Inspector ou colete a relíquia.");
            }
            else if (canInterrupt)
            {
                StartAttack(AttackType.WeaponArt);
                specialQueued = false;
                lightQueued = false;
                heavyQueued = false;
                return;
            }
        }

        // 5. Ataque Pesado (Botão Direito)
        if (heavyQueued)
        {
            if (!heavyAttackUnlocked)
            {
                // Bloqueado por progressão!
                heavyQueued = false;
                Debug.Log("<color=yellow>[Chroma Defender]</color> O Ataque Pesado está bloqueado! Encontre o upgrade na fase.");
            }
            else
            {
                if (action == ActionState.None)
                {
                    StartAttack(AttackType.Heavy1);
                    heavyQueued = false;
                    lightQueued = false;
                    return;
                }
                else if (action == ActionState.Attack && Progress >= comboWindowStart)
                {
                    // Encadeia pesado como finalizador ou combo pesado
                    if (currentAttack == AttackType.Light1 || currentAttack == AttackType.Light2 || currentAttack == AttackType.Light3)
                    {
                        StartAttack(AttackType.Heavy1);
                        heavyQueued = false;
                        lightQueued = false;
                        return;
                    }
                    else if (currentAttack == AttackType.Heavy1 && unlockedHeavyHits >= 2)
                    {
                        StartAttack(AttackType.Heavy2);
                        heavyQueued = false;
                        return;
                    }
                    else if (currentAttack == AttackType.Heavy2 && unlockedHeavyHits >= 3)
                    {
                        StartAttack(AttackType.Heavy3);
                        heavyQueued = false;
                        return;
                    }
                }
            }
        }

        // 5. Ataque Leve (Botão Esquerdo)
        if (lightQueued)
        {
            if (action == ActionState.None)
            {
                StartAttack(AttackType.Light1);
                lightQueued = false;
            }
            else if (action == ActionState.Attack && Progress >= comboWindowStart)
            {
                if (currentAttack == AttackType.Light1 && unlockedLightHits >= 2)
                {
                    StartAttack(AttackType.Light2);
                    lightQueued = false;
                }
                else if (currentAttack == AttackType.Light2 && unlockedLightHits >= 3)
                {
                    StartAttack(AttackType.Light3);
                    lightQueued = false;
                }
                else if (currentAttack == AttackType.Light3 && unlockedLightHits >= 4)
                {
                    StartAttack(AttackType.Light4);
                    lightQueued = false;
                }
                else if (currentAttack == AttackType.Light2 || currentAttack == AttackType.Light3 || currentAttack == AttackType.Light4)
                {
                    // Loop contínuo voltando para Light1
                    StartAttack(AttackType.Light1);
                    lightQueued = false;
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // Disparadores de Ações
    // ------------------------------------------------------------------
    private void ExecuteJump()
    {
        if (action == ActionState.Attack) EndAction();

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpLockout = 0.2f; // Trava imediata de 0.2s para garantir descolamento do chão
        coyoteTimer = 0f;
        airTime = 0.1f;
        landTimer = 0f;
        isGrounded = false;
    }

    private void StartRoll()
    {
        if (action == ActionState.Attack) EndAction();

        action = ActionState.Roll;
        actionTime = 0f;
        actionDuration = rollLength / Mathf.Max(0.01f, rollAnimSpeed);
        nextRollTime = Time.time + rollCooldown;
        lungeVelocity = 0f;
        landTimer = 0f;

        float input = GameManager.Instance.InputManager.Movement;
        if (Mathf.Abs(input) > 0.01f) facing = Mathf.Sign(input);

        // 1. Reduz a altura do colisor para passar por debaixo de inimigos e obstáculos
        if (rollShrinksCollider && playerBoxCollider != null)
        {
            float targetHeight = defaultColliderSize.y * rollColliderHeightFactor;
            playerBoxCollider.size = new Vector2(defaultColliderSize.x, targetHeight);
            float heightDiff = defaultColliderSize.y - targetHeight;
            playerBoxCollider.offset = new Vector2(defaultColliderOffset.x, defaultColliderOffset.y - (heightDiff * 0.5f));
        }

        // 2. Ignora colisores de inimigos para atravessar livremente por debaixo deles
        IgnoreEnemiesCollision(true);

        PlayAnimation(RollState, rollAnimSpeed, true);
    }

    private void IgnoreEnemiesCollision(bool ignore)
    {
        if (playerCollider == null) return;

        if (ignore)
        {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 4f);
            foreach (var col in nearby)
            {
                if (col == null || col == playerCollider || col.transform.IsChildOf(transform)) continue;

                // Nunca ignora chão/plataformas
                if (col.gameObject.layer == LayerMask.NameToLayer("Ground")) continue;

                bool isEnemy = col.GetComponent<TrainingDummy>() != null
                            || col.GetComponentInParent<TrainingDummy>() != null
                            || col.CompareTag("Enemy")
                            || (enemyLayers != 0 && ((1 << col.gameObject.layer) & enemyLayers) != 0);

                if (isEnemy)
                {
                    Physics2D.IgnoreCollision(playerCollider, col, true);
                    if (!ignoredRollColliders.Contains(col))
                    {
                        ignoredRollColliders.Add(col);
                    }
                }
            }
        }
        else
        {
            foreach (var col in ignoredRollColliders)
            {
                if (col != null && playerCollider != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, col, false);
                }
            }
            ignoredRollColliders.Clear();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (action == ActionState.Roll && playerCollider != null)
        {
            var col = collision.collider;
            if (col != null && col.gameObject.layer != LayerMask.NameToLayer("Ground"))
            {
                bool isEnemy = col.GetComponent<TrainingDummy>() != null
                            || col.GetComponentInParent<TrainingDummy>() != null
                            || col.CompareTag("Enemy")
                            || (enemyLayers != 0 && ((1 << col.gameObject.layer) & enemyLayers) != 0);

                if (isEnemy)
                {
                    Physics2D.IgnoreCollision(playerCollider, col, true);
                    if (!ignoredRollColliders.Contains(col))
                    {
                        ignoredRollColliders.Add(col);
                    }
                }
            }
        }
    }

    private void StartParry()
    {
        action = ActionState.Parry;
        actionTime = 0f;
        actionDuration = 0.4f;
        lungeVelocity = 0f;
        landTimer = 0f;

        PlayAnimation(ParryStartState, 1f, true);
    }

    private void StartAttack(AttackType type)
    {
        action = ActionState.Attack;
        currentAttack = type;
        actionTime = 0f;

        float input = GameManager.Instance.InputManager.Movement;
        if (Mathf.Abs(input) > 0.01f) facing = Mathf.Sign(input);

        string stateName;
        float length;
        float speed = lightAttackAnimSpeed;
        float lunge = lightLungeSpeed;

        switch (type)
        {
            case AttackType.Light2:
                stateName = Attack2State; length = attack2Length; lunge = lightLungeSpeed * 1.35f;
                break;
            case AttackType.Light3:
                stateName = Attack3State; length = attack3Length; lunge = lightLungeSpeed * 1.6f;
                break;
            case AttackType.Light4:
                stateName = Attack4State; length = attack4Length; lunge = lightLungeSpeed * 1.9f;
                break;
            case AttackType.Heavy1:
                stateName = Heavy1State; length = heavy1Length; speed = heavyAttackAnimSpeed; lunge = heavyLungeSpeed;
                break;
            case AttackType.Heavy2:
                stateName = Heavy2State; length = heavy2Length; speed = heavyAttackAnimSpeed * 1.2f; lunge = heavyLungeSpeed * 1.35f;
                break;
            case AttackType.Heavy3:
                stateName = Heavy3State; length = heavy3Length; speed = heavyAttackAnimSpeed; lunge = heavyLungeSpeed * 1.6f;
                break;
            case AttackType.WeaponArt:
                stateName = WeaponArtState; length = weaponArtLength; speed = 1f; lunge = 1.0f;
                break;
            default:
                stateName = Attack1State; length = attack1Length; lunge = lightLungeSpeed;
                break;
        }

        actionDuration = length / Mathf.Max(0.01f, speed);
        lungeVelocity = facing * lunge;
        landTimer = 0f;
        specialEffectSpawned = false;

        PlayAnimation(stateName, speed, true);

        if (type == AttackType.WeaponArt)
        {
            specialHitCount = 1;
            nextSpecialHitTime = 0.32f;
            PerformSpecialFlurryHit(1);
        }
        else
        {
            SpawnSlashVFX(type);
            PerformAttackHitbox(type);
        }

        OnAttackStarted?.Invoke(type);
    }

    private void SpawnSlashVFX(AttackType type)
    {
        GameObject prefabToSpawn = (currentPolarity == ElementPolarity.Fire) ? fireSlashPrefab : iceSlashPrefab;
        if (prefabToSpawn != null)
        {
            Vector3 pos = transform.position + new Vector3(slashOffset.x * facing, slashOffset.y, 0f);
            GameObject vfx = Instantiate(prefabToSpawn, pos, Quaternion.identity);

            // Inverte a direção se estiver olhando para a esquerda
            Vector3 scale = vfx.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facing;
            vfx.transform.localScale = scale;

            // Os packs de VFX usam sorting order 0 (ficam atrás dos sprites no 2D): traz para frente
            IceVFX.BringToFront(vfx);

            // Destrói o efeito após terminar
            Destroy(vfx, 0.8f);
        }

        // Camada extra procedural de elemento: arco de corte + partículas (sempre visível no URP 2D)
        {
            float[] angles = { -25f, 25f, -10f, 35f, -20f, 20f, 0f, 0f };
            float angle = angles[Mathf.Clamp((int)type, 0, angles.Length - 1)];
            bool big = type == AttackType.Heavy1 || type == AttackType.Heavy2 || type == AttackType.Heavy3 || type == AttackType.Light4;

            Vector3 arcPos = transform.position + new Vector3(slashOffset.x * facing + 0.2f * facing, slashOffset.y + 0.15f, 0f);
            Vector3 particlePos = arcPos + new Vector3(0.3f * facing, 0f, 0f);

            if (currentPolarity == ElementPolarity.Ice)
            {
                IceVFX.SpawnSlashArc(arcPos, facing, angle, big ? 2.1f : 1.6f);
                IceVFX.SpawnSlashShards(particlePos, facing, big ? 22 : 12);
            }
            else
            {
                FireVFX.SpawnSlashArc(arcPos, facing, angle, big ? 2.1f : 1.6f);
                FireVFX.SpawnSlashEmbers(particlePos, facing, big ? 26 : 14);
            }
        }
    }

    private void PerformAttackHitbox(AttackType type)
    {
        Vector2 hitboxPos = (Vector2)transform.position + new Vector2(attackHitboxOffset.x * facing, attackHitboxOffset.y);
        Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxPos, attackHitboxSize, 0f);

        int damage = light1Damage;
        float knockbackX = lightKnockbackForce;
        float knockbackY = 0f;

        switch (type)
        {
            case AttackType.Light1:
                damage = light1Damage;
                knockbackX = lightKnockbackForce;
                knockbackY = 0f;
                break;
            case AttackType.Light2:
                damage = light2Damage;
                knockbackX = lightKnockbackForce * 1.15f;
                knockbackY = 0f;
                break;
            case AttackType.Light3:
                damage = light3Damage;
                knockbackX = lightKnockbackForce * 1.5f;
                knockbackY = 0.4f;
                break;
            case AttackType.Light4:
                damage = light4Damage;
                knockbackX = lightFinisherKnockback;
                knockbackY = 2.2f;
                break;
            case AttackType.Heavy1:
                damage = heavy1Damage;
                knockbackX = heavyKnockbackForce;
                knockbackY = 0.5f;
                break;
            case AttackType.Heavy2:
                damage = heavy2Damage;
                knockbackX = heavyKnockbackForce * 1.3f;
                knockbackY = 0.8f;
                break;
            case AttackType.Heavy3:
                damage = heavy3Damage;
                knockbackX = heavyFinisherKnockback;
                knockbackY = 3.0f;
                break;
            default:
                break;
        }

        foreach (var hit in hits)
        {
            if (hit == null || hit.transform.IsChildOf(transform) || hit.gameObject == gameObject) continue;

            IDamageableTarget target = hit.GetComponent<IDamageableTarget>();
            if (target == null) target = hit.GetComponentInParent<IDamageableTarget>();

            if (target != null)
            {
                Vector2 knockback = new Vector2(facing * knockbackX, knockbackY);
                target.TakeDamage(damage, knockback, currentPolarity);
            }
        }
    }

    private void PerformSpecialFlurryHit(int hitNumber)
    {
        SpawnSlashVFX(AttackType.WeaponArt);

        // Avanço contínuo do cavaleiro durante os cortes da tempestade
        lungeVelocity = facing * 2.0f;

        Vector2 hitboxPos = (Vector2)transform.position + new Vector2(attackHitboxOffset.x * facing, attackHitboxOffset.y);
        Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxPos, attackHitboxSize * 1.25f, 0f);

        foreach (var hit in hits)
        {
            if (hit == null || hit.transform.IsChildOf(transform) || hit.gameObject == gameObject) continue;

            IDamageableTarget target = hit.GetComponent<IDamageableTarget>();
            if (target == null) target = hit.GetComponentInParent<IDamageableTarget>();

            if (target != null)
            {
                // Knockback sutil prendendo o alvo na tempestade de golpes
                Vector2 knockback = new Vector2(facing * 0.8f, 0f);
                target.TakeDamage(specialFlurryDamage, knockback, currentPolarity);
            }
        }
    }

    private void PerformSpecialFinisherHit()
    {
        Vector2 hitboxPos = (Vector2)transform.position + new Vector2(impactOffset.x * facing, impactOffset.y);
        Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxPos, attackHitboxSize * 1.45f, 0f);

        foreach (var hit in hits)
        {
            if (hit == null || hit.transform.IsChildOf(transform) || hit.gameObject == gameObject) continue;

            IDamageableTarget target = hit.GetComponent<IDamageableTarget>();
            if (target == null) target = hit.GetComponentInParent<IDamageableTarget>();

            if (target != null)
            {
                Vector2 knockback = new Vector2(facing * specialFinisherKnockback, 3.5f);
                target.TakeDamage(specialFinisherDamage, knockback, currentPolarity);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 hitboxPos = (Vector2)transform.position + new Vector2(attackHitboxOffset.x * facing, attackHitboxOffset.y);
        Gizmos.DrawWireCube(hitboxPos, attackHitboxSize);
    }

    private void SpawnImpactEffect()
    {
        if (impactEffectPrefab != null)
        {
            Vector3 pos = transform.position + new Vector3(impactOffset.x * facing, impactOffset.y, 0f);
            GameObject fx = Instantiate(impactEffectPrefab, pos, Quaternion.identity);
            var fxSr = fx.GetComponent<SpriteRenderer>();
            if (fxSr != null)
            {
                fxSr.flipX = facing < 0f;
            }
        }
    }

    // ------------------------------------------------------------------
    // Movimentação & Física Suave
    // ------------------------------------------------------------------
    private void UpdateMovement(float input, float dt)
    {
        Vector2 vel = rb.linearVelocity;

        if (action == ActionState.Roll)
        {
            float ease = Mathf.Lerp(1f, 0.3f, Mathf.InverseLerp(0.5f, 1f, Progress));
            vel.x = facing * rollSpeed * ease;
        }
        else if (action == ActionState.Parry)
        {
            vel.x = Mathf.MoveTowards(vel.x, 0f, groundDeceleration * dt);
        }
        else
        {
            float speedMult = 1f;
            if (action == ActionState.Attack)
            {
                speedMult = (currentAttack == AttackType.Heavy1 || currentAttack == AttackType.Heavy2 || currentAttack == AttackType.Heavy3)
                    ? heavyMoveFactor : lightMoveFactor;
            }

            lungeVelocity = Mathf.MoveTowards(lungeVelocity, 0f, lungeDecay * dt);
            float target = (input * moveSpeed * speedMult) + lungeVelocity;

            float accel = !isGrounded ? airAcceleration : (Mathf.Abs(target) > 0.01f ? groundAcceleration : groundDeceleration);
            vel.x = Mathf.MoveTowards(vel.x, target, accel * dt);
        }

        rb.linearVelocity = vel;
    }

    private void UpdateFacing(float input)
    {
        if (action == ActionState.None && Mathf.Abs(input) > 0.01f)
        {
            facing = Mathf.Sign(input);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = facing < 0f;
        }
    }

    // ------------------------------------------------------------------
    // Animações de Locomoção & Queda Dinâmica
    // ------------------------------------------------------------------
    private void UpdateAnimation(float input)
    {
        if (animator == null || action != ActionState.None) return;

        string desired;
        float speed = 1f;

        // Prioridade 1: No ar (pulo ou queda) NUNCA toca Run ou Idle, mesmo segurando A ou D!
        if (!isGrounded)
        {
            float vy = rb.linearVelocity.y;
            if (vy > 0.1f)
            {
                desired = JumpState;
            }
            else if (vy <= -6.5f)
            {
                desired = FallHeavyState;
            }
            else
            {
                desired = FallLightState;
            }
        }
        else if (landTimer > 0f && Mathf.Abs(input) < 0.1f)
        {
            desired = LandState;
            speed = 1.4f;
        }
        else
        {
            desired = Mathf.Abs(input) > 0.1f ? RunState : IdleState;
        }

        PlayAnimation(desired, speed);
    }

    private void PlayAnimation(string stateName, float speed, bool force = false)
    {
        if (animator == null) return;

        animator.speed = speed;
        if (currentAnimState == stateName && !force) return;

        currentAnimState = stateName;
        animator.Play(stateName, 0, 0f);
    }

    // ------------------------------------------------------------------
    // Sistema de Dano e Morte
    // ------------------------------------------------------------------
    public void TakeDamage(int amount, Vector2 knockbackDirection)
    {
        if (IsDead || IsInvulnerable) return;

        if (IsParrying)
        {
            // Bloqueou o ataque!
            PlayAnimation(ParryState, 1.2f, true);
            rb.linearVelocity = -knockbackDirection.normalized * 2f;
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        invulnerableUntil = Time.time + hurtInvulnerabilityTime;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            action = ActionState.Hurt;
            actionTime = 0f;
            actionDuration = 0.35f;
            rb.linearVelocity = knockbackDirection;
            PlayAnimation(HurtState, 1f, true);
        }
    }

    private void Die()
    {
        action = ActionState.Dead;
        rb.linearVelocity = Vector2.zero;
        PlayAnimation(DeathState, 1f, true);
        OnPlayerDeath?.Invoke();
        Debug.Log("<color=red>[Player]</color> O Cavaleiro tombou em batalha.");
    }

    public void SwitchPolarity()
    {
        if (IsDead || Time.time < nextPolaritySwitchTime) return;

        // Alterna entre Fogo e Gelo
        currentPolarity = (currentPolarity == ElementPolarity.Fire) ? ElementPolarity.Ice : ElementPolarity.Fire;
        nextPolaritySwitchTime = Time.time + polarityCooldown;

        // Se estiver no chão e livre, toca a animação de canalização de Buff
        if (isGrounded && action == ActionState.None)
        {
            action = ActionState.Buff;
            actionTime = 0f;
            actionDuration = 0.45f; // Transição ágil de concentração
            PlayAnimation(BuffState, 1.4f, true);
        }

        ApplyPolarityVisuals();
        OnPolarityChanged?.Invoke(currentPolarity);

        string cor = currentPolarity == ElementPolarity.Fire ? "<color=orange>FOGO</color>" : "<color=cyan>GELO</color>";
        Debug.Log($"[Chroma Defender] Polaridade alterada para: {cor}");
    }

    private void ApplyPolarityVisuals()
    {
        // O corpo do cavaleiro permanece com a armadura preta original e limpa (Color.white = sem tint verde)
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        // Remove quaisquer auras ou efeitos ao redor do personagem
        RemoveAuras();

        // Altera unicamente a lâmina da espada para azul gélido no modo de gelo
        ApplySwordTint(currentPolarity == ElementPolarity.Ice);
    }

    private void RemoveAuras()
    {
        if (iceAuraObject != null)
        {
            Destroy(iceAuraObject);
            iceAuraObject = null;
        }
        Transform iceFound = transform.Find("IceAura");
        if (iceFound != null) Destroy(iceFound.gameObject);

        if (fireAuraObject != null)
        {
            Destroy(fireAuraObject);
            fireAuraObject = null;
        }
        Transform fireFound = transform.Find("FireAura");
        if (fireFound != null) Destroy(fireFound.gameObject);
    }

    // ------------------------------------------------------------------
    // Cor da espada (shader troca apenas os pixels laranja/dourados por azul gélido)
    // ------------------------------------------------------------------
    [Header("--- Cor da Espada no Modo Gelo ---")]
    [Tooltip("Shader ChromaDefender/SpriteElementTint. Se vazio, é buscado automaticamente.")]
    [SerializeField] private Shader swordTintShader;
    private Material defaultSpriteMaterial;
    private Material iceSwordMaterial;

    private void ApplySwordTint(bool ice)
    {
        if (spriteRenderer == null) return;

        if (defaultSpriteMaterial == null) defaultSpriteMaterial = spriteRenderer.sharedMaterial;

        if (ice)
        {
            if (iceSwordMaterial == null)
            {
                if (swordTintShader == null) swordTintShader = Shader.Find("ChromaDefender/SpriteElementTint");
                if (swordTintShader == null)
                {
                    Debug.LogWarning("[Chroma Defender] Shader 'ChromaDefender/SpriteElementTint' não encontrado. A espada continuará laranja.");
                    return;
                }
                iceSwordMaterial = new Material(swordTintShader) { name = "IceSwordTint" };
            }
            spriteRenderer.sharedMaterial = iceSwordMaterial;
        }
        else if (defaultSpriteMaterial != null)
        {
            spriteRenderer.sharedMaterial = defaultSpriteMaterial;
        }
    }

    // ------------------------------------------------------------------
    // Métodos Públicos de Progressão (Itens / Fases)
    // ------------------------------------------------------------------
    public void UnlockHeavyAttack(int hitChain = 1)
    {
        heavyAttackUnlocked = true;
        unlockedHeavyHits = Mathf.Clamp(hitChain, 1, 3);
        Debug.Log($"<color=green>[Progressão]</color> Ataque Pesado desbloqueado! (Cadeia de {unlockedHeavyHits} golpes)");
    }

    public void UpgradeLightCombo(int hits)
    {
        unlockedLightHits = Mathf.Clamp(hits, 2, 4);
        Debug.Log($"<color=green>[Progressão]</color> Combo Leve melhorado para {unlockedLightHits} golpes!");
    }

    public void UnlockWeaponArt()
    {
        weaponArtUnlocked = true;
        Debug.Log("<color=green>[Progressão]</color> Habilidade Suprema (Weapon Art) desbloqueada!");
    }

    public void CollectCastleKey()
    {
        hasCastleKey = true;
        OnCastleKeyChanged?.Invoke(true);
        Debug.Log("<color=gold>[CHAVE COLETADA]</color> Você obteve a Chave Dourada do Castelo! A porta trancada agora pode ser aberta.");
    }

    public void ConsumeCastleKey()
    {
        hasCastleKey = false;
        OnCastleKeyChanged?.Invoke(false);
        Debug.Log("<color=yellow>[CHAVE UTILIZADA]</color> A Chave Dourada destrancou o portão!");
    }
}
