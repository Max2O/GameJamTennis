using UnityEngine;

public class NetFault : MonoBehaviour
{
    public enum ShotOwner
    {
        None,
        Player,
        Enemy
    }

    [Header("References")]
    [SerializeField] private Collider netCollider;
    [SerializeField] private Collider playerSideTableArea;
    [SerializeField] private Collider enemySideTableArea;
    [SerializeField] private GameOver gameOver;

    private ShotOwner currentShotOwner = ShotOwner.None;
    private bool hasTouchedNet;
    private bool hasTriggeredGameOver;

    public void BeginPlayerShot()
    {
        BeginShot(ShotOwner.Player);
    }

    public void BeginEnemyShot()
    {
        BeginShot(ShotOwner.Enemy);
    }

    private void BeginShot(ShotOwner owner)
    {
        currentShotOwner = owner;
        hasTouchedNet = false;
        hasTriggeredGameOver = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentShotOwner == ShotOwner.None || hasTriggeredGameOver)
            return;

        if (collision.collider == netCollider)
        {
            hasTouchedNet = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTouchedNet || hasTriggeredGameOver)
            return;

        bool returnedToOwnSide =
            (currentShotOwner == ShotOwner.Player &&
             other == playerSideTableArea) ||
            (currentShotOwner == ShotOwner.Enemy &&
             other == enemySideTableArea);

        if (returnedToOwnSide)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        if(currentShotOwner == ShotOwner.Enemy)
        {
            gameOver.PlayersFault = false;
        }
        else
        {
            gameOver.PlayersFault = true;
        }
        hasTriggeredGameOver = true;
        currentShotOwner = ShotOwner.None;

        if (gameOver != null)
        {
            gameOver.GameOverFunc();
        }
        else
        {
            Debug.LogError("NetFault: GameOver reference is missing!");
        }
    }

    public void ResetFault()
    {
        currentShotOwner = ShotOwner.None;
        hasTouchedNet = false;
        hasTriggeredGameOver = false;
    }
}