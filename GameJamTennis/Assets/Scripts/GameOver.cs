using UnityEngine;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    public bool PlayersFault;
    public GameObject GameOverUI;
    public Image WinLossImg;
    public Collider ball;

    public Sprite Victory;
    public Sprite Defeat;

    [Header("Card Effects")]
    [SerializeField] private CardEffectManager cardEffectManager;

    private bool trackingPlayerShot;
    private bool playerShotReachedEnemySide;

    public void BeginPlayerShot()
    {
        trackingPlayerShot = true;
        playerShotReachedEnemySide = false;
    }

    public void PlayerShotReachedEnemySide()
    {
        if (trackingPlayerShot)
        {
            playerShotReachedEnemySide = true;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider != ball)
            return;

        // Determine the outcome of the player's shot.
        if (trackingPlayerShot)
        {
            PlayersFault = !playerShotReachedEnemySide;
            trackingPlayerShot = false;
        }

        GameOverFunc();
    }

    public void GameOverFunc()
    {
        // Remove all card modifiers.
        if (cardEffectManager != null)
        {
            cardEffectManager.ClearAllEffects();
        }
        else
        {
            Debug.LogWarning("GameOver: CardEffectManager reference is missing.", this);
        }

        Time.timeScale = 0f;
        GameOverUI.SetActive(true);

        if (PlayersFault)
            WinLossImg.sprite = Defeat;
        else
            WinLossImg.sprite = Victory;
    }

    public void ResetGameOver()
    {
        PlayersFault = false;
        trackingPlayerShot = false;
        playerShotReachedEnemySide = false;
        GameOverUI.SetActive(false);
    }
}