using UnityEngine;
using System.Collections.Generic;

public abstract class Projectile : MonoBehaviour {
    protected Unit shooter;
    protected BookData shooterFaction;
    protected float damage;
    protected float aoeRadius;
    protected bool isActive = true;

    public GameObject explosionPrefab; // Assign a Particle System prefab

    public virtual void Setup(Unit shooterUnit, float dmg, float radius) {
        shooter = shooterUnit;
        shooterFaction = shooterUnit.faction;
        damage = dmg;
        aoeRadius = radius;
        
        // Color the projectile to match the faction
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = shooterUnit.factionManager.factionColor;
    }

    protected void Explode() {
        isActive = false;
        
        // AOE Damage Calculation (Matches HTML distance check)
        List<Unit> units = BattleManager.Instance.allUnits;
        foreach (Unit u in units) {
            if (u.hp > 0 && !u.isEscaped && u.faction != shooterFaction) {
                if (Vector2.Distance(transform.position, u.transform.position) <= aoeRadius) {
                    u.TakeDamage(damage);
                }
            }
        }

        // Spawn VFX
        if (explosionPrefab != null) {
            GameObject vfx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            var main = vfx.GetComponent<ParticleSystem>().main;
            main.startColor = BattleManager.Instance.factions.Find(f => f.book == shooterFaction).factionColor;
            Destroy(vfx, 2f);
        }

        // TODO: Play Sound here (e.g., AudioManager.Instance.Play("magicHit"))
        Destroy(gameObject);
    }
}