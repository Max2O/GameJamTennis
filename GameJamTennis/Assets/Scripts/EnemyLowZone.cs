
using UnityEngine;

public class EnemyLowZone : MonoBehaviour
{
    [SerializeField] private EnemyController enemy;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            enemy.SetLowZoneActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            enemy.SetLowZoneActive(false);
        }
    }
}
