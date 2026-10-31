
using UnityEngine;

public class AttackManager : MonoBehaviour
{
    public enum AttackState
    {
        Inactive,
        MovingToAim,
        Aiming,
        MovingBack
    }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform overheadCameraPosition;
    [SerializeField] private Transform aimMarker;
    [SerializeField] private LineRenderer trajectoryLine;

    [Header("Targeting")]
    [SerializeField] private LayerMask aimSurfaceMask;
    [SerializeField] private float landingHeightOffset = 0.05f;
    [SerializeField] private float raycastDistance = 100f;

    [Header("Camera")]
    [SerializeField] private float cameraTransitionDuration = 0.7f;

    [Header("Trajectory Preview")]
    [SerializeField] private int trajectoryPoints = 40;

    private AttackState state = AttackState.Inactive;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;

    private Vector3 cameraStartPosition;
    private Quaternion cameraStartRotation;

    private Vector3 cameraDestinationPosition;
    private Quaternion cameraDestinationRotation;

    private float transitionTimer;

    private Transform ball;
    private Vector3 selectedTarget;
    private bool hasValidTarget;

    private PlayerShot playerShot;
    private bool shotConfirmed;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        aimMarker.gameObject.SetActive(false);
        trajectoryLine.enabled = false;
        trajectoryLine.useWorldSpace = true;
    }

    public void BeginAiming(Transform receivedBall)
    {
        if (state != AttackState.Inactive || receivedBall == null)
            return;

        ball = receivedBall;

        playerShot = ball.GetComponent<PlayerShot>();

        if (playerShot == null)
        {
            Debug.LogError("The ball is missing its PlayerShot component!");
            return;
        }

        shotConfirmed = false;

        Time.timeScale = 0f;

        originalCameraPosition = mainCamera.transform.position;
        originalCameraRotation = mainCamera.transform.rotation;

        aimMarker.gameObject.SetActive(true);
        trajectoryLine.enabled = true;

        Cursor.visible = false;

        hasValidTarget = false;

        StartCameraTransition(
            overheadCameraPosition.position,
            overheadCameraPosition.rotation,
            AttackState.MovingToAim
        );
    }

    private void Update()
    {
        switch (state)
        {
            case AttackState.MovingToAim:
            case AttackState.MovingBack:
                UpdateCameraTransition();
                break;

            case AttackState.Aiming:
                UpdateAiming();
                break;
        }
    }

    private void StartCameraTransition(
        Vector3 destinationPosition,
        Quaternion destinationRotation,
        AttackState transitionState)
    {
        cameraStartPosition = mainCamera.transform.position;
        cameraStartRotation = mainCamera.transform.rotation;

        cameraDestinationPosition = destinationPosition;
        cameraDestinationRotation = destinationRotation;

        transitionTimer = 0f;
        state = transitionState;
    }

    private void UpdateCameraTransition()
    {
        transitionTimer += Time.unscaledDeltaTime;

        float progress = Mathf.Clamp01(
            transitionTimer / cameraTransitionDuration
        );

        float smoothProgress = progress * progress *
                               (3f - 2f * progress);

        mainCamera.transform.position = Vector3.Lerp(
            cameraStartPosition,
            cameraDestinationPosition,
            smoothProgress
        );

        mainCamera.transform.rotation = Quaternion.Slerp(
            cameraStartRotation,
            cameraDestinationRotation,
            smoothProgress
        );

        if (progress >= 1f)
        {
            if (state == AttackState.MovingToAim)
                state = AttackState.Aiming;

            else if (state == AttackState.MovingBack)
                FinishAiming();
        }
    }

    private void UpdateAiming()
    {
        UpdateTargeting();

        if (hasValidTarget && Input.GetMouseButtonDown(0))
            ConfirmTarget();
    }

    private void UpdateTargeting()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            raycastDistance,
            aimSurfaceMask,
            QueryTriggerInteraction.Ignore))
        {
            Collider ballCollider = ball.GetComponent<Collider>();

            float ballRadius = ballCollider != null
                ? ballCollider.bounds.extents.y
                : 0f;

            selectedTarget = hit.point +
                Vector3.up * (ballRadius + landingHeightOffset);

            hasValidTarget = UpdateTrajectory();

            aimMarker.position = selectedTarget;
            aimMarker.gameObject.SetActive(hasValidTarget);
        }
        else
        {
            hasValidTarget = false;
            aimMarker.gameObject.SetActive(false);
            trajectoryLine.enabled = false;
        }
    }

    private bool UpdateTrajectory()
    {
        if (!playerShot.TryCalculatePreview(
            ball.position,
            selectedTarget,
            out Vector3 velocity,
            out float flightTime))
        {
            trajectoryLine.enabled = false;
            return false;
        }

        int count = Mathf.Max(2, trajectoryPoints);

        trajectoryLine.positionCount = count;
        trajectoryLine.enabled = true;

        for (int i = 0; i < count; i++)
        {
            float progress = (float)i / (count - 1);
            float elapsedTime = flightTime * progress;

            Vector3 position = playerShot.EvaluateTrajectory(
                ball.position,
                velocity,
                elapsedTime
            );

            trajectoryLine.SetPosition(i, position);
        }

        return true;
    }

    private void ConfirmTarget()
    {
        shotConfirmed = true;

        StartCameraTransition(
            originalCameraPosition,
            originalCameraRotation,
            AttackState.MovingBack
        );
    }

    private void FinishAiming()
    {
        aimMarker.gameObject.SetActive(false);
        trajectoryLine.enabled = false;

        Cursor.visible = true;

        if (shotConfirmed)
        {
            playerShot.LaunchShot(selectedTarget);
        }

        state = AttackState.Inactive;
        Time.timeScale = 1f;
    }
}
