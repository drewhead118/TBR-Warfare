using System.Collections;
using UnityEngine;

public class ArrowProjectile : Projectile
{
    [Header("Refs")]
    public Transform visualSprite;              // Child transform holding the arrow art
    public SpriteRenderer[] spriteRenderers;    // Optional: auto-filled if empty

    [Header("Planar Motion (XY)")]
    public float planarSpeed = 10f;            // Units/sec across the battlefield (XY plane)
    public float scatterRadius = 2f;           // Random aim scatter

    [Header("Arc / Height (Fake Z)")]
    public float heightPerUnit = 0.20f;        // How “lofty” the shot is per unit of distance
    public float minApexHeight = 0.75f;        // Clamp minimum arc height
    public float maxApexHeight = 4.0f;         // Clamp maximum arc height

    [Header("Impact Juice")]
    public float stickWobbleDuration = 0.35f;
    public float wobbleDegrees = 10f;          // Initial wobble amplitude
    public float wobbleFrequency = 22f;        // Higher = faster vibration
    public float wobbleDamping = 18f;          // Higher = damps quicker
    public float fadeDelay = 0.20f;
    public float fadeDuration = 0.40f;

    // Internal
    private Vector2 startPos;
    private Vector2 targetPos;

    private float flightTime;
    private float elapsed;

    // Ballistic height params (fake Z)
    private float apexHeight;
    private float gravity;     // derived so we land at height=0 at t=flightTime and reach apexHeight
    private float v0z;         // initial vertical (height) velocity

    private bool arrived;
    private Quaternion baseImpactRotation; // rotation we “stick” at

    public void SetupArrow(Unit shooterUnit, Vector2 target, float dmg, float radius)
    {
        base.Setup(shooterUnit, dmg, radius);

        if (!visualSprite)
        {
            Debug.LogError($"{name}: ArrowProjectile requires visualSprite assigned.");
            return;
        }

        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = visualSprite.GetComponentsInChildren<SpriteRenderer>(true);

        startPos = shooterUnit.transform.position;

        // Random scatter
        targetPos = target + Random.insideUnitCircle * scatterRadius;

        float dist = Vector2.Distance(startPos, targetPos);
        dist = Mathf.Max(0.001f, dist);

        // Variable travel time based on planar speed
        flightTime = dist / Mathf.Max(0.001f, planarSpeed);
        // Optional clamp to avoid extremely tiny/huge times:
        flightTime = Mathf.Clamp(flightTime, 0.12f, 2.5f);

        // Choose an apex height (loft) based on distance (clamped)
        apexHeight = Mathf.Clamp(dist * heightPerUnit, minApexHeight, maxApexHeight);

        // Derive gravity so that:
        // h(t) = v0z*t - 0.5*g*t^2
        gravity = (8f * apexHeight) / (flightTime * flightTime);
        v0z = (4f * apexHeight) / flightTime;

        elapsed = 0f;
        arrived = false;

        // Initialize orientation based on initial velocity (planar path + initial upward jump)
        Vector2 planarVel = (targetPos - startPos) / flightTime;
        Vector2 initialVel = planarVel + new Vector2(0f, v0z);
        
        SetVisualRotationFromVelocity(initialVel);
        visualSprite.localPosition = Vector3.zero;
    }

    private void Update()
    {
        if (!isActive || arrived) return;

        float simSpeed = BattleManager.Instance.simSpeed;
        float dt = Time.deltaTime * simSpeed;

        elapsed += dt;
        float t = Mathf.Clamp(elapsed, 0f, flightTime);

        // --- Planar motion (XY) ---
        float u = t / flightTime;
        Vector2 planarPos = Vector2.Lerp(startPos, targetPos, u);
        transform.position = planarPos;

        // --- Height motion (fake Z, displayed as local Y offset) ---
        float h = (v0z * t) - (0.5f * gravity * t * t);
        if (h < 0f) h = 0f;

        // Apply height as local offset
        Vector3 lp = visualSprite.localPosition;
        lp.y = h;
        visualSprite.localPosition = lp;

        // --- Rotation matches instantaneous visual velocity ---
        Vector2 planarVel = (targetPos - startPos) / flightTime;

        // Calculate current height velocity (dh/dt) so it points exactly along its true visual arc!
        float currentZVel = v0z - (gravity * t);
        Vector2 visualVel = planarVel + new Vector2(0f, currentZVel);

        SetVisualRotationFromVelocity(visualVel);

        // Arrive / impact
        if (elapsed >= flightTime - Mathf.Epsilon)
        {
            ArriveAndStick(planarVel);
        }
    }

    private void SetVisualRotationFromVelocity(Vector2 vel)
    {
        if (vel.sqrMagnitude < 0.000001f) return;

        float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
        // Removed the errant negative sign so it actually faces where it's traveling
        visualSprite.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ArriveAndStick(Vector2 planarVel)
    {
        arrived = true;

        // Ensure final position & height are settled
        transform.position = targetPos;
        var lp = visualSprite.localPosition;
        lp.y = 0f;
        visualSprite.localPosition = lp;

        // Freeze “stick” rotation at impact direction, factoring in final downward falling speed
        float finalZVel = v0z - (gravity * flightTime);
        Vector2 finalVisualVel = planarVel + new Vector2(0f, finalZVel);

        if (finalVisualVel.sqrMagnitude > 0.000001f)
        {
            float angle = Mathf.Atan2(finalVisualVel.y, finalVisualVel.x) * Mathf.Rad2Deg;
            baseImpactRotation = Quaternion.Euler(0f, 0f, angle);
            visualSprite.rotation = baseImpactRotation;
        }
        else
        {
            baseImpactRotation = visualSprite.rotation;
        }

        // Do the gameplay effect now (damage / hit / explode), but don't instantly kill the visuals.
        Explode();

        // Start juicy impact wobble + fade
        StartCoroutine(ImpactWobbleThenFade());
    }

    private IEnumerator ImpactWobbleThenFade()
    {
        // Small damped rotational vibration around the stuck angle
        float t = 0f;
        while (t < stickWobbleDuration)
        {
            t += Time.deltaTime * BattleManager.Instance.simSpeed;

            // Damped sinusoid
            float damp = Mathf.Exp(-wobbleDamping * (t / Mathf.Max(0.0001f, stickWobbleDuration)));
            float wobble = Mathf.Sin(t * wobbleFrequency) * wobbleDegrees * damp;

            visualSprite.rotation = baseImpactRotation * Quaternion.Euler(0f, 0f, wobble);
            yield return null;
        }

        // Snap back to base rotation
        visualSprite.rotation = baseImpactRotation;

        // Fade out
        float delay = fadeDelay;
        while (delay > 0f)
        {
            delay -= Time.deltaTime * BattleManager.Instance.simSpeed;
            yield return null;
        }

        float fadeT = 0f;
        while (fadeT < fadeDuration)
        {
            fadeT += Time.deltaTime * BattleManager.Instance.simSpeed;
            float a = 1f - Mathf.Clamp01(fadeT / fadeDuration);
            SetAlpha(a);
            yield return null;
        }

        SetAlpha(0f);

        // If your base Projectile doesn’t auto-destroy on Explode(), do it here.
        Destroy(gameObject);
    }

    private void SetAlpha(float a)
    {
        if (spriteRenderers == null) return;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            var sr = spriteRenderers[i];
            if (!sr) continue;
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }
}