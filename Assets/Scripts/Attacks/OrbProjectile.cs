using UnityEngine;

public class OrbProjectile : Projectile {
    private Vector2 velocity;
    private float life = 3f; // Seconds before self-destructing

    public void SetupOrb(Unit shooterUnit, Unit targetUnit, float dmg, float radius) {
        base.Setup(shooterUnit, dmg, radius);
        Vector2 dir = ((Vector2)targetUnit.transform.position - (Vector2)transform.position).normalized;
        velocity = dir * 15f; // Speed
    }

    void Update() {
        if (!isActive) return;
        float simSpeed = BattleManager.Instance.simSpeed;
        
        transform.position += (Vector3)velocity * Time.deltaTime * simSpeed;
        life -= Time.deltaTime * simSpeed;

        // Check Proximity Hit
        bool hit = false;
        foreach (Unit u in BattleManager.Instance.allUnits) {
            if (u.hp > 0 && !u.isEscaped && u.faction != shooterFaction) {
                if (Vector2.Distance(transform.position, u.transform.position) < 1.5f) { // Projectile Hitbox
                    hit = true; break;
                }
            }
        }

        if (hit || life <= 0) Explode();
    }
}