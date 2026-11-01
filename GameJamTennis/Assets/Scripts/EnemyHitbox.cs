
using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] private EnemyController enemy;

    [Tooltip("Enable this for the low/crouching swing hitbox.")]
    [SerializeField] private bool isLowSwingHitbox = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            enemy.OnBallHit(isLowSwingHitbox);
        }
    }
}
