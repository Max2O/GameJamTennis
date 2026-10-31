using UnityEngine;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    // works for both a win and a loss
    public bool PlayersFault;
    public GameObject GameOverUI;
    public Image WinLossImg;
    public Collider ball;

    public Sprite Victory;
    public Sprite Defeat;

    void OnCollisionEnter(Collision collision){
        if(collision.collider == ball){
            Time.timeScale = 0f;
            GameOverUI.SetActive(true);
            if(PlayersFault){
                WinLossImg.sprite = Defeat;
            }
            else
            {
                WinLossImg.sprite = Victory;
            }
        }
    }
}