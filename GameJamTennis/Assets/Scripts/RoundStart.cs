using UnityEngine;

public class RoundStart : MonoBehaviour
{
    [Header("Round Settings")]
    [SerializeField] private float firstQteDuration = 5f;

    [Header("Game Objects")]
    public GameObject player;
    public GameObject GameOverScreen;
    public GameObject ball;

    [SerializeField] private GameObject StartingShade;
    [SerializeField] private EnemyBounceBack enemyBounceBack;

    private BallCaptureQTE ballCaptureQTE;
    private EnemyReturnTrajectory enemyReturnTrajectory;
    private Rigidbody ballRigidbody;

    private Vector3 playerStartPosition;
    private Quaternion playerStartRotation;

    private Vector3 ballStartPosition;
    private Quaternion ballStartRotation;

    private void Awake()
    {
        // Save the original scene positions before gameplay begins.
        playerStartPosition = player.transform.position;
        playerStartRotation = player.transform.rotation;

        ballStartPosition = ball.transform.position;
        ballStartRotation = ball.transform.rotation;

        ballRigidbody = ball.GetComponent<Rigidbody>();
        ballCaptureQTE = player.GetComponent<BallCaptureQTE>();
        enemyReturnTrajectory = ball.GetComponent<EnemyReturnTrajectory>();
    }

    private void Start()
    {
        Time.timeScale = 0f;

        if (ballCaptureQTE != null)
            ballCaptureQTE.SetFirstQteDuration(firstQteDuration);

        StartingShade.SetActive(true);
        GameOverScreen.SetActive(false);
    }

    public void StartRound()
    {
        if (ballCaptureQTE != null)
            ballCaptureQTE.enabled = true;

        Time.timeScale = 1f;
        StartingShade.SetActive(false);
    }

    public void RestartRound()
    {
        // Pause everything before changing positions or resetting scripts.
        Time.timeScale = 0f;

        // Restore the player's original position and rotation.
        player.transform.SetPositionAndRotation(
            playerStartPosition,
            playerStartRotation
        );

        // Restore the ball's original position and rotation.
        ball.transform.SetPositionAndRotation(
            ballStartPosition,
            ballStartRotation
        );

        // Clear all existing ball movement.
        if (ballRigidbody != null)
        {
            ballRigidbody.linearVelocity = Vector3.zero;
            ballRigidbody.angularVelocity = Vector3.zero;
            ballRigidbody.position = ballStartPosition;
            ballRigidbody.rotation = ballStartRotation;
        }

        NetFault netFault = ball.GetComponent<NetFault>();

        if (netFault != null)
            netFault.ResetFault();

        // Reset the trajectory state.
        enemyReturnTrajectory = ball.GetComponent<EnemyReturnTrajectory>();

        if (enemyReturnTrajectory != null)
            enemyReturnTrajectory.ResetTrajectory();

        // Allow the enemy to return the ball again.
        if (enemyBounceBack != null)
            enemyBounceBack.ResetRoundState();

        // Reset QTE state, including the first-QTE timer.
        if (ballCaptureQTE != null)
        {
            ballCaptureQTE.ResetQTEState();
            ballCaptureQTE.SetFirstQteDuration(firstQteDuration);
            ballCaptureQTE.enabled = true;
        }

        // Show the starting screen again.
        StartingShade.SetActive(true);

        Physics.SyncTransforms();
        GameOverScreen.SetActive(false);
    }
}