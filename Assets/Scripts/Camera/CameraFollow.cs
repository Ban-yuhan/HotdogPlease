using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CameraFollow : MonoBehaviour
{
    [Header("타겟 플레이어")]
    [SerializeField] private Transform targetPlayer;

    [Header("추적 세팅")]
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private Vector3 offset;

    private void Start()
    {
        if (offset == Vector3.zero && targetPlayer != null)
        {
            offset = transform.position - targetPlayer.position;
        }
    }

    private void LateUpdate()
    {
        if (targetPlayer == null) return;

        Vector3 targetPosition = targetPlayer.position + offset;

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
    }
}
