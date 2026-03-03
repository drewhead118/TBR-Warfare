using UnityEngine;

public abstract class Unit : MonoBehaviour {
    public BookData faction;
    public FactionManager factionManager;
    
    [Header("Stats")]
    public float maxHp = 100f;
    public float hp;
    public float speed = 2f;
    public float attackRange = 1.5f;
    public float cooldownTime = 1f;

    protected float currentCooldown;
    protected Unit target;
    protected bool isFleeing;
    public bool isEscaped;
    public float fleeThreshold;

    [Header("Visuals")]
    public Transform visualSprite; // Drag the sprite child here (used for levitation Y offset)
    public Unit levitatedBy;

    public virtual void Setup(BookData bookFaction, FactionManager manager) {
        faction = bookFaction;
        factionManager = manager;
        hp = maxHp;
        fleeThreshold = Random.Range(0f, 0.35f);
        GetComponentInChildren<SpriteRenderer>().color = manager.factionColor;
    }

    protected virtual void Update() {
        if (hp <= 0 || isEscaped) return;

        // Levitation mechanic from your Mage logic
        if (levitatedBy != null) {
            if (levitatedBy.hp <= 0) levitatedBy = null;
            else {
                visualSprite.localPosition = Vector3.Lerp(visualSprite.localPosition, new Vector3(0, 2f, 0), Time.deltaTime * 5);
                transform.position = Vector3.MoveTowards(transform.position, levitatedBy.transform.position, Time.deltaTime);
                return; // Stunned while levitated
            }
        } else {
            visualSprite.localPosition = Vector3.Lerp(visualSprite.localPosition, Vector3.zero, Time.deltaTime * 5);
        }

        currentCooldown -= Time.deltaTime * BattleManager.Instance.simSpeed;

        if (Vector2.Distance(Vector2.zero, transform.position) > BattleManager.Instance.escapeRadius) {
            Escape(); return;
        }

        ManageTargetingAndMovement();
    }

    void ManageTargetingAndMovement() {
        if (target == null || target.hp <= 0 || target.isEscaped || Random.value < 0.01f) FindTarget();

        float hpRatio = hp / maxHp;
        isFleeing = hpRatio < fleeThreshold;

        if (isFleeing) {
            Vector2 fleeDir = transform.position.normalized; // Run away from center
            transform.position += (Vector3)fleeDir * (speed * 1.3f * Time.deltaTime * BattleManager.Instance.simSpeed);
            return;
        }

        if (target != null) {
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist > attackRange * 0.8f) {
                transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime * BattleManager.Instance.simSpeed);
            }
            if (dist <= attackRange && currentCooldown <= 0) {
                Attack();
                currentCooldown = cooldownTime + Random.Range(0f, 0.5f);
            }
        }
    }

    protected void FindTarget() {
        target = BattleManager.Instance.GetClosestEnemy(this);
    }

    protected abstract void Attack();

    public virtual void TakeDamage(float amount) {
        hp -= amount;
        if (hp <= 0) Die();
    }

    protected virtual void Die() {
        factionManager.aliveCount--;
        Destroy(gameObject); // Or use object pooling
    }

    protected void Escape() {
        isEscaped = true;
        factionManager.aliveCount--;
        factionManager.escapedCount++;
        Destroy(gameObject);
    }
}