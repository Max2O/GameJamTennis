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

        if (gameOver != null)
            gameOver.BeginPlayerShot();
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

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggeredGameOver)
            return;

        // Check whether the player's shot reached the enemy's side.
        if (currentShotOwner == ShotOwner.Player &&
            enemySideTableArea != null &&
            (other == enemySideTableArea ||
            other.transform.IsChildOf(enemySideTableArea.transform)))
        {
            if (gameOver != null)
                gameOver.PlayerShotReachedEnemySide();
        }
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

    private void Update()
    {
        if (!hasTouchedNet || hasTriggeredGameOver)
            return;

        Collider ownSide = currentShotOwner == ShotOwner.Player
            ? playerSideTableArea
            : enemySideTableArea;

        if (ownSide != null && ownSide.bounds.Intersects(GetComponent<Collider>().bounds))
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        hasTriggeredGameOver = true;

        if (gameOver != null)
        {
            gameOver.PlayersFault = currentShotOwner == ShotOwner.Player;
            gameOver.GameOverFunc();
        }
        else
        {
            Debug.LogError("NetFault: GameOver reference is missing!");
        }

        currentShotOwner = ShotOwner.None;
    }

    public void ResetFault()
    {
        currentShotOwner = ShotOwner.None;
        hasTouchedNet = false;
        hasTriggeredGameOver = false;
    }
}