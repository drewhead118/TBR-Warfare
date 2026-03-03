using UnityEngine;

public class ArrowProjectile : Projectile {
    private Vector2 startPos;
    private Vector2 targetPos;
    private float progress = 0f;
    private float speed = 5f;
    private float totalDist;
    public Transform visualSprite; // Assign child sprite here

    public void SetupArrow(Unit shooterUnit, Vector2 target, float dmg, float radius) {
        base.Setup(shooterUnit, dmg, radius);
        startPos = shooterUnit.transform.position;
        // Add random scatter like the HTML
        targetPos = target + new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f)); 
        totalDist = Vector2.Distance(startPos, targetPos);
        
        // Aim arrow
        Vector2 dir = targetPos - startPos;
        visualSprite.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    void Update() {
        if (!isActive) return;
        float simSpeed = BattleManager.Instance.simSpeed;
        
        progress += (speed * Time.deltaTime * simSpeed) / totalDist;
        
        // Move Base
        Vector2 currentPos = Vector2.Lerp(startPos, targetPos, progress);
        transform.position = currentPos;

        // Simulate Z-Arc (Height)
        float height = Mathf.Sin(progress * Mathf.PI) * 3f;
        visualSprite.localPosition = new Vector3(0, height, 0);

        if (progress >= 1f) Explode();
    }
}