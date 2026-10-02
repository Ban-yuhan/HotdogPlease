using UnityEngine;

public class Billboard : MonoBehaviour
{
    [Header("위치 오프셋 (X: 화면 우측, Y: 월드 위)")]
    [SerializeField] private Vector3 offset = new Vector3(0.8f, 2.8f, 0f);

    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = FindAnyObjectByType<Camera>();
        }
    }

    private void LateUpdate()
    {
        if (transform.parent == null) return;

        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        // 1. 회전 고정 (카메라 바라보기)
        transform.rotation = mainCam.transform.rotation;

        // 2. 부모(Player) 위치를 가져오되, 부모의 회전값은 완전히 무시하고 배치
        Vector3 parentPos = transform.parent.position;

        // 화면 기준 오른쪽(mainCam.right) + 월드 기준 위쪽(Vector3.up)
        Vector3 targetPos = parentPos
                          + (mainCam.transform.right * offset.x)
                          + (Vector3.up * offset.y);

        transform.position = targetPos;
    }
}