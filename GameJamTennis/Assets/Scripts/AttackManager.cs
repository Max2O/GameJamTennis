using TMPro;
using UnityEngine.UI;
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

    public bool IsAiming { get; private set; }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform overheadCameraPosition;
    [SerializeField] private Transform aimMarker;
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private CardSelectionManager cardSelectionManager;

    [Header("Targeting")]
    [SerializeField] private LayerMask aimSurfaceMask;
    [SerializeField] private float landingHeightOffset = 0.05f;
    [SerializeField] private float raycastDistance = 100f;

    [Header("Camera")]
    [SerializeField] private float cameraTransitionDuration = 0.7f;

    [Header("Trajectory Preview")]
    [SerializeField] private int trajectoryPoints = 40;

    [Header("Aim Timer")]
    [SerializeField] private float aimTimeLimit = 1.3f;
    [SerializeField] private Collider netCollider;

    [Header("Timeout Shot")]
    [SerializeField] private float timeoutShotSpeed = 5f;
    [SerializeField] private float timeoutRandomness = 0.25f;

    [Header("Aim Timer UI")]
    [SerializeField] private GameObject aimTimerUI;
    [SerializeField] private Slider aimTimerSlider;
    [SerializeField] private TMP_Text aimTimerText;

    private float aimTimeRemaining;
    private bool firstAimingPhase = true;
    private bool shotTimedOut;
    private Vector3 timeoutTarget;

    private AttackState state = AttackState.Inactive;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;

    private Vector3 cameraStartPosition;
    private Quaternion cameraStartRotation;

    private Vector3 cameraDestinationPosition;
    private Vector3 originalShotTarget;
    private bool hasOriginalShotTarget;
    private Quaternion cameraDestinationRotation;

    private float transitionTimer;

    private Transform ball;
    private Vector3 selectedTarget;
    private bool hasValidTarget;

    private PlayerShot playerShot;

    private void Awake(){
        if (mainCamera == null)
            mainCamera = Camera.main;

        aimTimerUI.SetActive(false);

        aimTimerSlider.minValue = 0f;
        aimTimerSlider.maxValue = 1f;
        aimTimerSlider.value = 1f;

        aimMarker.gameObject.SetActive(false);
        trajectoryLine.enabled = false;
        trajectoryLine.useWorldSpace = true;
    }

    public void BeginAiming(Transform receivedBall){
        if (state != AttackState.Inactive || receivedBall == null)
            return;

        ball = receivedBall;

        playerShot = ball.GetComponent<PlayerShot>();

        if (playerShot == null){
            return;
        }

        shotTimedOut = false;
        aimTimeRemaining = 0f;


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
        IsAiming = true;
    }

    private void Update(){
        switch (state){
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
        AttackState transitionState){
        cameraStartPosition = mainCamera.transform.position;
        cameraStartRotation = mainCamera.transform.rotation;

        cameraDestinationPosition = destinationPosition;
        cameraDestinationRotation = destinationRotation;

        transitionTimer = 0f;
        state = transitionState;
    }

    private void UpdateCameraTransition(){
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

        if (progress >= 1f){
            if (state == AttackState.MovingToAim){
                state = AttackState.Aiming;

                if (firstAimingPhase){
                    aimTimeRemaining = Mathf.Infinity;
                    firstAimingPhase = false;
                }else{
                    aimTimeRemaining = aimTimeLimit;
                }

                aimTimerUI.SetActive(true);
                UpdateAimTimerUI();
            }
            else if (state == AttackState.MovingBack){
                FinishAiming();
            }
        }
    }

    private void UpdateAiming(){
        UpdateTargeting();

        if (hasValidTarget && Input.GetMouseButtonDown(0)){
            ConfirmTarget();
            return;
        }

        if (!float.IsInfinity(aimTimeRemaining)){
            aimTimeRemaining -= Time.unscaledDeltaTime;

            UpdateAimTimerUI();

            if (aimTimeRemaining <= 0f){
                TimeoutShot();
            }
        }
    }

    private void TimeoutShot(){
        if (state != AttackState.Aiming)
            return;

        if (netCollider == null){
            return;
        }

        shotTimedOut = true;

        timeoutTarget = GetRandomNetTarget();

        aimMarker.gameObject.SetActive(false);
        trajectoryLine.enabled = false;

        StartCameraTransition(
            originalCameraPosition,
            originalCameraRotation,
            AttackState.MovingBack
        );
    }

    private void UpdateAimTimerUI(){
        if (float.IsInfinity(aimTimeRemaining)){
            aimTimerSlider.value = 1f;
            aimTimerText.text = "∞";
            return;
        }

        float normalizedTime = Mathf.Clamp01(
            aimTimeRemaining / aimTimeLimit
        );

        aimTimerSlider.value = normalizedTime;
        aimTimerText.text = Mathf.Max(0f, aimTimeRemaining).ToString("F1") + "s";
    }

    private Vector3 GetRandomNetTarget(){
        Bounds bounds = netCollider.bounds;

        // Find the thinnest world-space axis of the net.
        Vector3 size = bounds.size;

        int thinAxis = 0;
        if (size.y < size.x) thinAxis = 1;
        if (size.z < size[thinAxis]) thinAxis = 2;

        // Start with the point on the net closest to the ball.
        Vector3 basePoint = netCollider.ClosestPoint(ball.position);

        Vector3 randomOffset = Vector3.zero;

        // Randomize along the two broad axes, but only slightly.
        for (int axis = 0; axis < 3; axis++){
            if (axis == thinAxis)
                continue;

            float variation = size[axis] * timeoutRandomness;

            randomOffset[axis] = Random.Range(
                -variation,
                variation
            );
        }

        // Project the randomized position onto the actual collider.
        Vector3 candidate = basePoint + randomOffset;
        Vector3 target = netCollider.ClosestPoint(candidate);

        return target;
    }

    private void UpdateTargeting(){
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
        }else{
            hasValidTarget = false;
            aimMarker.gameObject.SetActive(false);
            trajectoryLine.enabled = false;
        }
    }

    private bool UpdateTrajectory(){
        if (!playerShot.TryCalculatePreview(
            ball.position,
            selectedTarget,
            out Vector3 velocity,
            out float flightTime)){
            trajectoryLine.enabled = false;
            return false;
        }

        int count = Mathf.Max(2, trajectoryPoints);

        trajectoryLine.positionCount = count;
        trajectoryLine.enabled = true;

        for (int i = 0; i < count; i++){
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

    private void ConfirmTarget(){
        originalShotTarget = selectedTarget;
        hasOriginalShotTarget = true;

        StartCameraTransition(
            originalCameraPosition,
            originalCameraRotation,
            AttackState.MovingBack
        );
    }

    public void ReplayPreviousShot(Transform returningBall){
        if (returningBall == null){
            Time.timeScale = 1f;
            return;
        }

        if (!hasOriginalShotTarget){
            Time.timeScale = 1f;
            return;
        }

        if (state != AttackState.Inactive){
            Time.timeScale = 1f;
            return;
        }

        ball = returningBall;
        playerShot = ball.GetComponent<PlayerShot>();

        if (playerShot == null){
            Time.timeScale = 1f;
            return;
        }

        // Resume physics and launch toward the original target.
        Time.timeScale = 1f;

        if (playerShot.LaunchShot(originalShotTarget)){
            NetFault netFault = ball.GetComponent<NetFault>();
        }
    }

    private void FinishAiming()
    {
        aimMarker.gameObject.SetActive(false);
        trajectoryLine.enabled = false;
        Cursor.visible = true;
        aimTimerUI.SetActive(false);

        // Resume physics before launching the ball.
        Time.timeScale = 1f;

        if (shotTimedOut)
        {
            Rigidbody ballBody = ball.GetComponent<Rigidbody>();

            if (ballBody != null)
            {
                Vector3 direction =
                    (timeoutTarget - ball.position).normalized;

                ballBody.linearVelocity = Vector3.zero;
                ballBody.angularVelocity = Vector3.zero;

                ballBody.AddForce(
                    direction * timeoutShotSpeed * ballBody.mass,
                    ForceMode.Impulse
                );
            }
        }
        else
        {
            // Launch toward the player's selected landing point.
            if (playerShot != null)
            {
                bool launched = playerShot.LaunchShot(selectedTarget);

                if (!launched)
                    Debug.LogError("Player shot failed to launch!", this);
            }
        }

        state = AttackState.Inactive;
        IsAiming = false;

        if (cardSelectionManager != null)
            cardSelectionManager.RegisterPlayerHit();
    }
}
