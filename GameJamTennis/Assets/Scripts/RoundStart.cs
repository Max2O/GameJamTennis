using UnityEngine;

public class RoundStart : MonoBehaviour
{
    [SerializeField] private float firstQteDuration = 5f;
    public GameObject player;
    public GameObject ball;
    [SerializeField] private GameObject StartingShade;
    private BallCaptureQTE ballCaptureQTE;

    void Start(){
        Time.timeScale = 0f;
        ballCaptureQTE = player.GetComponent<BallCaptureQTE>();
        ballCaptureQTE.SetFirstQteDuration(firstQteDuration);
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
