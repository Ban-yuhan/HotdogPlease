using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Customer : MonoBehaviour
{
    [SerializeField] private TMP_Text orderText; // 머리 위 주문 수량 표시
    [SerializeField] private float moveSpeed = 3f;

    public int requestedAmount { get; private set; }
    public int currentAmount { get; private set; }
    public bool IsSatisfied => requestedAmount > 0 && currentAmount >= requestedAmount;

    private List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private bool isMoving = false;
    private System.Action onReachedDestination;

    public bool IsWaitingAtCounter { get; private set; } // 카운터 도착 대기 상태

    // 💡 [추가] 줄 서기 위치에 완전히 도착했는지 여부
    public bool IsQueueing { get; private set; } = false;

    private void Start()
    {
        if (orderText != null)
        {
            orderText.gameObject.SetActive(false);
        }
    }

    // 주문 초기화 (CustomerZone 도착 시 호출)
    public void InitOrder(int min, int max)
    {
        requestedAmount = Random.Range(min, max + 1);
        currentAmount = 0;

        IsWaitingAtCounter = true;

        UpdateUI();
    }

    // 매니저가 경로를 넘겨줄 때 호출 (최초 스폰 및 입장시)
    public void SetPath(List<Vector3> pathPoints, System.Action onComplete = null)
    {
        IsWaitingAtCounter = false;
        IsQueueing = false; // 💡 새 경로 출발 시 대기열도 미도착 상태로 리셋

        waypoints = pathPoints;
        currentWaypointIndex = 0;

        // 💡 목적지 도착 시 IsQueueing을 true로 변경하도록 콜백 감싸기
        onReachedDestination = () =>
        {
            IsQueueing = true;
            onComplete?.Invoke();
        };

        isMoving = true;
    }

    // 💡 [추가] 줄이 당겨질 때 CustomerManager에서 호출할 함수
    public void UpdateQueueTarget(Vector3 newTargetPos)
    {
        if (IsQueueing)
        {
            // 이미 줄에 도착해서 서 있던 손님: 한 칸 앞 지점으로 이동
            waypoints = new List<Vector3> { newTargetPos };
            currentWaypointIndex = 0;
            isMoving = true;
        }
        else
        {
            // 걸어오는 중인 손님: 기존 경유지(Door 등)는 유지하고 맨 끝 최종 목적지만 새 위치로 교체!
            if (waypoints != null && waypoints.Count > 0)
            {
                waypoints[waypoints.Count - 1] = newTargetPos;
            }
        }
    }

    private void Update()
    {
        if (!isMoving || waypoints == null || waypoints.Count == 0) return;

        Vector3 targetPos = waypoints[currentWaypointIndex];
        targetPos.y = transform.position.y;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        Vector3 moveDir = (targetPos - transform.position).normalized;
        if (moveDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
        }

        if (Vector3.Distance(transform.position, targetPos) < 0.05f)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Count)
            {
                isMoving = false;
                onReachedDestination?.Invoke();
            }
        }
    }

    // 핫도그 받기
    public bool ReceiveHotdog(GameObject hotdog)
    {
        if (IsSatisfied) return false;

        currentAmount++;
        Destroy(hotdog);

        UpdateUI();

        return true;
    }

    private void UpdateUI()
    {
        if (orderText == null) return;

        int remaining = requestedAmount - currentAmount;

        if (requestedAmount > 0 && remaining > 0)
        {
            orderText.gameObject.SetActive(true);
            orderText.text = remaining.ToString();
        }
        else
        {
            orderText.gameObject.SetActive(false);
        }
    }

    public void ShowStatusText(string message)
    {
        if (orderText == null) return;
        orderText.gameObject.SetActive(true);
        orderText.text = message;
    }
}