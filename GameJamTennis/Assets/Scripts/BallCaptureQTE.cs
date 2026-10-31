using TMPro;
using UnityEngine;

public class BallCaptureQTE : MonoBehaviour
{
    public float QTEtimer = 0.0f;
    public GameObject qteObject;
    bool QTEstarted = false;
    bool shouldBegin = true;
    [Tooltip("Cameras")]
    public Camera attackcam;
    [SerializeField] private Camera mainCamera;

    [SerializeField] private TMP_Text timerText;
    [SerializeField] private RectTransform indicator;
    [SerializeField] private Transform ball;
    void Start(){
        attackcam.enabled = false;
        mainCamera.enabled = true;
        qteObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other){
        if (other.TryGetComponent<EnemyReturnTrajectory>(out EnemyReturnTrajectory ert) && shouldBegin){
            UpdateIndicator();
            Time.timeScale = 0f;
            QTEstarted = true;
            qteObject.SetActive(true);
            QTEtimer = 3.0f;
            // Camera.main.WorldToScreenPoint(worldPosition);
        }
    }

    void Update(){
        if (QTEstarted){
            if(timerText != null)timerText.text = QTEtimer.ToString("F1");
            QTEtimer -= Time.unscaledDeltaTime;
            if (QTEtimer <= 0){ // QTE loss
                qteObject.SetActive(false);
                QTEstarted = false;
                shouldBegin = false;
                Time.timeScale = 1f;
            }
        }
    }

    public void QTEwin(){
        if(QTEtimer > 0){
            qteObject.SetActive(false);
            QTEstarted = false;
            shouldBegin = false;
            mainCamera.enabled = false;
            attackcam.enabled = true;
        }
    }

    private void UpdateIndicator()
    {
        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(ball.position);

        indicator.position = screenPosition;
    }
}