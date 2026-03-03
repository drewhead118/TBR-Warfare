using UnityEngine;

namespace DefaultNamespace
{
    public class Archer : Unit {
        public GameObject arrowPrefab;
        protected override void Attack() {
            GameObject arrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
            arrow.GetComponent<ArrowProjectile>().SetupArrow(this, target.transform.position, 12f, 2.5f); // Damage, AoE Radius
        }
    }
}