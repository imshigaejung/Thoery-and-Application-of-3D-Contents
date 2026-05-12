using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // 플레이어
    public Vector3 offset = new Vector3(1, 2, -4);
    public Vector3 lookOffset = new Vector3(0, 1.5f, 2f); 
    public float tiltAmount = 10f;
    public float tiltSmooth = 5f;
    private float currentTilt = 0f;
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        Vector3 desiredPosition = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        PlayerController player = target.GetComponent<PlayerController>();

        float targetTilt = 0f;

        if (player != null && player.IsDrifting())
        {
            targetTilt = -player.GetMoveInput().x * tiltAmount;
        }

        // 부드럽게 기울기
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSmooth * Time.deltaTime);

        // 👇 여기 핵심
        Vector3 lookTarget = target.position + target.TransformDirection(lookOffset);
        transform.LookAt(lookTarget);

        transform.rotation = Quaternion.Euler(
        transform.eulerAngles.x,
        transform.eulerAngles.y,
        currentTilt
        );

    }
}