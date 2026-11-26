using UnityEngine;

/// <summary>
/// Projétil de inimigo (flecha do arqueiro, orbe do mago, magia do Arquimago).
///
/// Regra de polaridade (estilo Ikaruga):
///  - Projétil NEUTRO  → sempre causa dano.
///  - Projétil de FOGO/GELO → se o cavaleiro estiver com o MESMO elemento ativo, ele é ABSORVIDO
///    (não causa dano e cura um pouquinho). Com o elemento oposto, causa dano normal.
///
/// Criado por código: EnemyProjectile.Spawn(...). Não precisa de prefab.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    private Vector2 velocity;
    private int damage;
    private float knockback;
    private EnemyPolarityType element;
    private float lifeTime;
    private float homing;
    private int absorbHeal;

    private Sprite[] frames;
    private float fps = 12f;
    private int frame;
    private float frameTimer;
    private SpriteRenderer sr;
    private Transform target;
    private bool done;

    private static int groundMask = -1;

    /// <summary>Cria e dispara um projétil.</summary>
    /// <param name="homing">0 = reto. Valores como 1.5 fazem a magia curvar devagar na direção do jogador.</param>
    public static EnemyProjectile Spawn(Vector3 position, Vector2 velocity, int damage, EnemyPolarityType element,
        Sprite[] frames, float scale = 1f, float lifeTime = 4f, float homing = 0f, int sortingOrder = 40,
        float knockback = 5f, Color? tint = null, int absorbHeal = 2)
    {
        var go = new GameObject("EnemyProjectile");
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var p = go.AddComponent<EnemyProjectile>();
        p.velocity = velocity;
        p.damage = damage;
        p.element = element;
        p.frames = frames;
        p.lifeTime = lifeTime;
        p.homing = homing;
        p.knockback = knockback;
        p.absorbHeal = absorbHeal;

        p.sr = go.AddComponent<SpriteRenderer>();
        p.sr.sortingOrder = sortingOrder;
        if (frames != null && frames.Length > 0) p.sr.sprite = frames[0];
        p.sr.color = tint ?? DefaultTint(element);

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.12f / Mathf.Max(0.01f, scale) * 1.6f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;

        var pl = FindAnyObjectByType<PlayerBehavior>();
        if (pl != null) p.target = pl.transform;

        p.ApplyRotation();
        return p;
    }

    public static Color DefaultTint(EnemyPolarityType e)
    {
        switch (e)
        {
            case EnemyPolarityType.Fire: return new Color(1f, 0.55f, 0.3f);
            case EnemyPolarityType.Ice: return new Color(0.5f, 0.85f, 1f);
            default: return Color.white;
        }
    }

    private void Update()
    {
        if (done) return;
        float dt = Time.deltaTime;

        if (homing > 0f && target != null)
        {
            Vector2 to = ((Vector2)target.position + Vector2.up * 0.8f - (Vector2)transform.position).normalized;
            float speed = velocity.magnitude;
            velocity = Vector2.Lerp(velocity.normalized, to, homing * dt).normalized * speed;
        }

        transform.position += (Vector3)(velocity * dt);
        ApplyRotation();

        if (frames != null && frames.Length > 1)
        {
            frameTimer += dt;
            if (frameTimer >= 1f / fps)
            {
                frameTimer = 0f;
                frame = (frame + 1) % frames.Length;
                sr.sprite = frames[frame];
            }
        }

        lifeTime -= dt;
        if (lifeTime <= 0f) Explode(false);
    }

    private void ApplyRotation()
    {
        // Sprites dos projéteis olham para a DIREITA.
        float ang = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (done) return;

        var player = other.GetComponentInParent<PlayerBehavior>();
        if (player != null)
        {
            if (player.IsDead) return;

            bool sameElement =
                (element == EnemyPolarityType.Fire && player.CurrentPolarity == ElementPolarity.Fire) ||
                (element == EnemyPolarityType.Ice && player.CurrentPolarity == ElementPolarity.Ice);

            if (sameElement)
            {
                // Absorvido pelo elemento igual: sem dano.
                int healed = absorbHeal > 0 ? player.Heal(absorbHeal) : 0;
                FloatingText.Spawn(player.transform.position + Vector3.up * 1.6f,
                    healed > 0 ? "Absorvido +" + healed : "Absorvido", new Color(0.7f, 1f, 0.7f));
            }
            else if (!player.IsInvulnerable)
            {
                float dir = Mathf.Sign(velocity.x == 0f ? 1f : velocity.x);
                player.TakeDamage(damage, new Vector2(dir * knockback, 2.5f));
            }
            Explode(true);
            return;
        }

        // Some ao bater no cenário (chão/paredes), ignorando gatilhos e outros inimigos.
        if (other.isTrigger) return;
        if (other.GetComponentInParent<EnemyBehavior>() != null) return;
        if (other.GetComponentInParent<BossBase>() != null) return;
        if (groundMask == -1) groundMask = LayerMask.GetMask("Ground", "Default");
        if ((groundMask & (1 << other.gameObject.layer)) != 0) Explode(true);
    }

    private void Explode(bool impact)
    {
        if (done) return;
        done = true;
        if (impact)
        {
            if (element == EnemyPolarityType.Ice) IceVFX.SpawnFrostBurst(transform.position, 10);
            else if (element == EnemyPolarityType.Fire) FireVFX.SpawnFlameBurst(transform.position, 10);
        }
        Destroy(gameObject);
    }
}
