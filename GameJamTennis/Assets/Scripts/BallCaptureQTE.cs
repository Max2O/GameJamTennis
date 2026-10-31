using UnityEngine;

public class BallCaptureQTE : MonoBehaviour
{
    public float QTEtimer = 0.0f;
    bool QTEstarted = false;
    bool shouldBegin = true;
    // this script is responcibe for the player getting the QTE when the ball enters it's area
    void OnTriggerEnter(Collider other){
        if (other.TryGetComponent<EnemyReturnTrajectory>(out EnemyReturnTrajectory ert) && shouldBegin){
            Time.timeScale = 0f;
            QTEstarted = true;
            QTEtimer = 3.0f;
            // Camera.main.WorldToScreenPoint(worldPosition);
        }
    }

    void Update(){
        if (QTEstarted)
        {
            QTEtimer -= Time.unscaledDeltaTime;
            if (QTEtimer <= 0)
            {
                QTEstarted = false;
                shouldBegin = false;
                Time.timeScale = 1f;
            }
        }
    }
}