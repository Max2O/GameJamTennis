using UnityEngine;

public class GameOver : MonoBehaviour
{
    // works for both a win and a loss
    public GameObject ball;
    public BallCaptureQTE ballCaptureQTE;

    void OnTriggerEnter(Collider other){
        if(other == ball){
            Time.timeScale = 0f;
            
        }
    }
}