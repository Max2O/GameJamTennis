using TMPro;
using UnityEngine;

public class BallCaptureQTE : MonoBehaviour
{
    public float QTEtimer = 0.0f;
    public GameObject qteObject;
    bool QTEstarted = false;
    bool shouldBegin = true;

    [SerializeField] private AttackManager attackManager;
    [Tooltip("Camera")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("QTE UI stuff")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private RectTransform indicator;
    [SerializeField] private Transform ball;
    void Start(){
        //startingCamPos = mainCamera.gameObject.Transform;
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

    public void QTEwin()
    {
        if (!QTEstarted || QTEtimer <= 0f)
            return;

        qteObject.SetActive(false);
        QTEstarted = false;
        shouldBegin = false;

        if (attackManager != null)
        {
            attackManager.BeginAiming(ball);
        }
        else
        {
            Debug.LogError("AttackManager is not assigned!");
            Time.timeScale = 1f;
        }
    }

    private void UpdateIndicator()
    {
        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(ball.position);

        indicator.position = screenPosition;
    }
}