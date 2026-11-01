
using TMPro;
using UnityEngine;

public class BallCaptureQTE : MonoBehaviour
{
    public float QTEtimer = 0.0f;
    public GameObject qteObject;
    private bool QTEstarted = false;
    public bool shouldBegin = true;

    [SerializeField] private GameOver gameOver;
    [SerializeField] private AttackManager attackManager;
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

    [Header("RamBall Modifier")]
    [SerializeField] private CardEffectManager cardEffectManager;
    [SerializeField] private float ramBallTimeBonusPerClick = 0.1f;

    private int requiredClicksRemaining = 1;
    private bool isFirstQte = true;
    private bool isBoomerangQTE = false;

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

    private void Start()
    {
        qteObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!shouldBegin || QTEstarted)
            return;

        if (!other.CompareTag("Ball"))
            return;

        EnemyReturnTrajectory trajectory =
            other.GetComponent<EnemyReturnTrajectory>();

        // The first QTE starts the game.
        // Later normal QTEs require an enemy return trajectory.
        if (!isFirstQte)
        {
            if (trajectory == null || !trajectory.IsHeadingToPlayer)
                return;
        }

        Rigidbody incomingBall = other.attachedRigidbody;

        if (incomingBall == null)
            return;

        ball = incomingBall.transform;
        StartQTE(incomingBall);
    }

    // Called by the Boomerang system when its return flight reaches the player.
    // This deliberately bypasses the normal trigger-based QTE check.
    public void BeginBoomerangQTE(Transform returningBall)
    {
        if (QTEstarted || returningBall == null)
            return;

        Rigidbody incomingBall = returningBall.GetComponent<Rigidbody>();

        if (incomingBall == null)
        {
            return;
        }

        ball = returningBall;
        isBoomerangQTE = true;

        StartQTE(incomingBall);
    }

    private void StartQTE(Rigidbody incomingBall)
    {
        QTEtimer = CalculateQteDuration(incomingBall.linearVelocity.magnitude);

        int ramBallStacks = cardEffectManager != null
            ? cardEffectManager.RamBallStacks
            : 0;

        requiredClicksRemaining = 1 + ramBallStacks;

        UpdateIndicator();

        QTEstarted = true;
        qteObject.SetActive(true);
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (!QTEstarted)
            return;

        if (timerText != null)
            timerText.text = QTEtimer.ToString("F1");

        QTEtimer -= Time.unscaledDeltaTime;

        if (QTEtimer <= 0f)
        {
            // QTE loss.
            qteObject.SetActive(false);
            QTEstarted = false;
            shouldBegin = false;
            requiredClicksRemaining = 1;
            isBoomerangQTE = false;

            Time.timeScale = 1f;

            if (gameOver != null)
                gameOver.GameOverFunc();
        }
    }

    public void QTEwin()
    {
        if (!QTEstarted || QTEtimer <= 0f)
            return;

        // RamBall requires additional successful clicks.
        if (requiredClicksRemaining > 1)
        {
            requiredClicksRemaining--;
            QTEtimer += ramBallTimeBonusPerClick;
            return;
        }

        // All required clicks completed.
        requiredClicksRemaining = 1;

        qteObject.SetActive(false);
        QTEstarted = false;
        shouldBegin = false;

        if (isBoomerangQTE)
        {
            isBoomerangQTE = false;

            if (attackManager != null)
                attackManager.ReplayPreviousShot(ball);
            else
            {
                Time.timeScale = 1f;
            }

            return;
        }

        if (attackManager != null)
        {
            attackManager.BeginAiming(ball);
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    private void UpdateIndicator()
    {
        if (mainCamera == null || indicator == null || ball == null)
            return;

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
        requiredClicksRemaining = 1;
        isBoomerangQTE = false;

        qteObject.SetActive(false);

        Time.timeScale = 0f;
    }
}
