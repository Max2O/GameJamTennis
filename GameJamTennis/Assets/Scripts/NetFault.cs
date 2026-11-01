
using UnityEngine;

public class NetFault : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider netCollider;
    [SerializeField] private Collider playerSideTableArea;
    [SerializeField] private Collider enemySideTableArea;
    [SerializeField] private GameOver gameOver;
    private bool hasTriggeredGameOver;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggeredGameOver || gameOver == null)
            return;

        if (IsSameColliderOrChild(other, playerSideTableArea))
        {
            gameOver.RecordTableTouch(true);
            return;
        }

        if (IsSameColliderOrChild(other, enemySideTableArea))
        {
            gameOver.RecordTableTouch(false);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasTriggeredGameOver)
            return;
    }

    private static bool IsSameColliderOrChild(
        Collider other,
        Collider parent)
    {
        return parent != null &&
            (other == parent ||
             other.transform.IsChildOf(parent.transform));
    }

    public void ResetFault()
    {
        hasTriggeredGameOver = false;
    }
}