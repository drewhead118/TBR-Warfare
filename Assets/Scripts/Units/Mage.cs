using UnityEngine;

namespace DefaultNamespace
{
    public class Mage : Unit {
        public GameObject orbPrefab;
        private float channelTimer;

        protected override void Update() {
            if (levitatedBy == null && target != null && channelTimer > 0) {
                channelTimer -= Time.deltaTime * BattleManager.Instance.simSpeed;
                if (channelTimer <= 0 || target.hp <= 0) {
                    if (target != null) target.levitatedBy = null;
                    currentCooldown = cooldownTime;
                } else {
                    target.levitatedBy = this;
                    return; // Channeling, don't move
                }
            }
            base.Update();
        }

        protected override void Attack() {
            if (Random.value < 0.25f && target.levitatedBy == null) {
                channelTimer = 3f; // 3 seconds of levitation
                target.levitatedBy = this;
            } else {
                GameObject orb = Instantiate(orbPrefab, transform.position, Quaternion.identity);
                orb.GetComponent<OrbProjectile>().SetupOrb(this, target, 25f, 4.5f);
            }
        }
    }
}