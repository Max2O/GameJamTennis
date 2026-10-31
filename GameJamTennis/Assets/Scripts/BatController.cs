using UnityEngine;

public class BatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private AttackManager attackManager;

    [Header("Movement")]
    [SerializeField] private float followSpeed = 20f;

    private Vector3 cursorOffset;
    private float fixedZ;

    private Plane movementPlane;

    private void Start()
    {
        fixedZ = transform.position.z;

        movementPlane = new Plane(
            Vector3.forward,
            new Vector3(0f, 0f, fixedZ)
        );

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (movementPlane.Raycast(ray, out float distance))
        {
            Vector3 cursorPosition = ray.GetPoint(distance);
            cursorOffset = transform.position - cursorPosition;
        }
    }

    private void Update()
    {
        if (attackManager != null && attackManager.IsAiming)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (movementPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPosition = ray.GetPoint(distance) + cursorOffset;

            targetPosition.z = fixedZ;

            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                followSpeed * Time.unscaledDeltaTime
            );
        }
    }
}