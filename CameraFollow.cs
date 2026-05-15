using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject Player;
    public Transform target;// 플레이어
    public Vector3 offset = new Vector3(1, 2, -4);
    public Vector3 lookOffset = new Vector3(0, 1.5f, 2f); 
    public float tiltAmount = 10f;
    public float tiltSmooth = 5f;
    private float currentTilt = 0f;
    public float smoothSpeed = 5f;
    public float rotationSmooth = 5f;
    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        Vector3 desiredPosition = target.position + target.TransformDirection(offset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f/smoothSpeed);
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
        Quaternion targetRot = Quaternion.LookRotation(lookTarget - transform.position);

        targetRot *= Quaternion.Euler(0, 0, currentTilt);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationSmooth * Time.deltaTime
        );
    }
}