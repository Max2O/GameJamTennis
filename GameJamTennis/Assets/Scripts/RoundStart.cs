using UnityEngine;

public class RoundStart : MonoBehaviour
{
    public GameObject Player;
    [SerializeField] private GameObject StartingShade;
    private BallCaptureQTE ballCaptureQTE;

    void Start(){
        Time.timeScale = 0f;
        ballCaptureQTE = Player.GetComponent<BallCaptureQTE>();
    }

    public void StartRound(){
        Time.timeScale = 1f;
        if(ballCaptureQTE != null)
        {
            ballCaptureQTE.enabled = true;
        }

        // keep this at the very end
        StartingShade.SetActive(false);
    }
}
