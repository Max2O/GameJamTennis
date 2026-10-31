using TMPro;
using UnityEngine;

public class BallCaptureQTE : MonoBehaviour
{
    public float QTEtimer = 0.0f;
    public GameObject qteObject;
    bool QTEstarted = false;
    public bool shouldBegin = true;

    [SerializeField] private GameOver gameOver;
    [SerializeField] private AttackManager attackManager;
    [Tooltip("Camera")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("QTE UI stuff")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private RectTransform indicator;
    [SerializeField] private Transform ball;

    [Header("QTE Timing")]
    [SerializeField] private float normalQteDuration = 3f;
    [SerializeField] private float firstQteDuration = 5f;

    [SerializeField] private float velocityThreshold = 3f;
    [SerializeField] private float velocityDecay = 2f;
    [SerializeField] private float minimumQteDuration = 1f;

    private bool isFirstQte = true;

    public void SetFirstQteDuration(float duration)
    {
        firstQteDuration = Mathf.Max(0.1f, duration);
    }

    private float CalculateQteDuration(float incomingSpeed)
    {
        if (isFirstQte)
        {
            isFirstQte = false;
            return firstQteDuration;
        }

        if (incomingSpeed <= velocityThreshold)
            return normalQteDuration;

        float excessSpeed = incomingSpeed - velocityThreshold;

        float duration =
            minimumQteDuration +
            (normalQteDuration - minimumQteDuration) *
            Mathf.Exp(-velocityDecay * excessSpeed);

        return Mathf.Clamp(duration, minimumQteDuration, normalQteDuration);
    }

    void Start(){
        qteObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!shouldBegin || QTEstarted)
            return;

        if (!other.CompareTag("Ball"))
            return;

        EnemyReturnTrajectory trajectory =
            other.GetComponent<EnemyReturnTrajectory>();

        // First QTE: player starts the game, so no enemy trajectory exists.
        if (!isFirstQte)
        {
            if (trajectory == null || !trajectory.IsHeadingToPlayer)
                return;
        }

        Rigidbody incomingBall = other.attachedRigidbody;

        if (incomingBall == null)
            return;

        QTEtimer = CalculateQteDuration(incomingBall.linearVelocity.magnitude);

        UpdateIndicator();

        QTEstarted = true;
        qteObject.SetActive(true);
        Time.timeScale = 0f;
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
                gameOver.PlayersFault = true;
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

    public void ResetQTEState()
    {
        QTEtimer = 0f;
        QTEstarted = false;
        shouldBegin = true;
        isFirstQte = true;

        qteObject.SetActive(false);

        Time.timeScale = 0f;
    }
}