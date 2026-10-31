
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
    [SerializeField] private float previewFlightTime = 1.2f;
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

        Time.timeScale = 0f;

        originalCameraPosition = mainCamera.transform.position;
        originalCameraRotation = mainCamera.transform.rotation;

        aimMarker.gameObject.SetActive(true);
        trajectoryLine.enabled = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

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

        if (Input.GetMouseButtonDown(1))
            CancelAiming();
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
            selectedTarget = hit.point;
            selectedTarget.y += landingHeightOffset;

            aimMarker.position = selectedTarget;
            aimMarker.gameObject.SetActive(true);

            hasValidTarget = true;

            UpdateTrajectory();
        }
        else
        {
            hasValidTarget = false;
            aimMarker.gameObject.SetActive(false);
            trajectoryLine.enabled = false;
        }
    }

    private Vector3 CalculatePreviewVelocity(
        Vector3 origin,
        Vector3 destination)
    {
        Vector3 gravity = Physics.gravity;

        return (destination - origin) / previewFlightTime
               - 0.5f * gravity * previewFlightTime;
    }

    private void UpdateTrajectory()
    {
        Vector3 origin = ball.position;

        Vector3 velocity = CalculatePreviewVelocity(
            origin,
            selectedTarget
        );

        int count = Mathf.Max(2, trajectoryPoints);

        trajectoryLine.positionCount = count;

        for (int i = 0; i < count; i++)
        {
            float t = previewFlightTime * i / (count - 1);

            Vector3 position =
                origin +
                velocity * t +
                0.5f * Physics.gravity * t * t;

            trajectoryLine.SetPosition(i, position);
        }
    }

    private void ConfirmTarget()
    {
        StartCameraTransition(
            originalCameraPosition,
            originalCameraRotation,
            AttackState.MovingBack
        );
    }

    private void CancelAiming()
    {
        hasValidTarget = false;

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

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        state = AttackState.Inactive;

        // Temporary: resume gameplay after aiming.
        // PlayerShot will take over this responsibility later.
        Time.timeScale = 1f;
    }
}
