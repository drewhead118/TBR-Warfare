using UnityEngine;

namespace DefaultNamespace
{
    public class Knight : Unit {
        protected override void Attack() {
            target.TakeDamage(Random.Range(15f, 20f));
            // Play Melee Sound
        }
    }
}