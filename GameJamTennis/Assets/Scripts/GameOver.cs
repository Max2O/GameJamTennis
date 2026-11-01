
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

    public enum TableSide
    {
        None,
        Player,
        Enemy
    }

    private TableSide lastTouchedSide = TableSide.None;

    public void RecordTableTouch(bool playerSide)
    {
        lastTouchedSide = playerSide
            ? TableSide.Player
            : TableSide.Enemy;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (ball == null || collision.collider != ball)
            return;

        GameOverFunc();
    }

    public void GameOverFunc()
    {
        // The player loses if the ball last touched their half.
        PlayersFault = lastTouchedSide == TableSide.Player;

        if (cardEffectManager != null)
            cardEffectManager.ClearAllEffects();

        Time.timeScale = 0f;
        GameOverUI.SetActive(true);
        WinLossImg.sprite = PlayersFault ? Defeat : Victory;
    }

    public void ResetGameOver()
    {
        PlayersFault = false;
        lastTouchedSide = TableSide.None;
        GameOverUI.SetActive(false);
    }
}