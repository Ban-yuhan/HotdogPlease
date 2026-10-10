using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Customer : MonoBehaviour
{
    [SerializeField] private TMP_Text orderText; // 머리 위 텍스트 UI
    [SerializeField] private float moveSpeed = 3f;

    public bool HasArrived => currentPath == null;

    public int requestedAmount { get; private set; } = 0;
    public int currentAmount { get; private set; } = 0;
    public bool IsSatisfied => currentAmount >= requestedAmount && requestedAmount > 0;

    public bool IsAtCounter { get; private set; } = false; // 카운터 맨 앞(CustomerZone) 도착 여부

    public Table AssignedTable { get; set; } // 💡 나에게 할당된 테이블 기억

    private Action onMoveComplete;
    private List<Vector3> currentPath;
    private int currentPathIndex;

    private void Awake()
    {
        if (orderText != null)
        {
            orderText.gameObject.SetActive(false);
        }
        requestedAmount = 0;
        currentAmount = 0;
    }

    private void Update()
    {
        MoveAlongPath();
    }

    // 경로 이동 설정
    public void SetPath(List<Vector3> path, Action onComplete = null)
    {
        currentPath = path;
        currentPathIndex = 0;
        onMoveComplete = onComplete;
    }

    private void MoveAlongPath()
    {
        if (currentPath == null || currentPathIndex >= currentPath.Count) return;

        Vector3 target = currentPath[currentPathIndex];
        target.y = transform.position.y;

        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        Vector3 dir = target - transform.position;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            currentPathIndex++;
            if (currentPathIndex >= currentPath.Count)
            {
                currentPath = null;
                onMoveComplete?.Invoke();
                onMoveComplete = null;
            }
        }
    }

    public void UpdateQueueTarget(Vector3 targetPos)
    {
        if (currentPath != null && currentPath.Count > 0 && currentPathIndex < currentPath.Count)
        {
            currentPath[currentPath.Count - 1] = targetPos;
        }
        else
        {
            SetPath(new List<Vector3> { targetPos });
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CustomerZone>() != null)
        {
            IsAtCounter = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<CustomerZone>() != null)
        {
            IsAtCounter = false;
        }
    }

    public void InitOrder(int min, int max)
    {
        requestedAmount = UnityEngine.Random.Range(min, max + 1);
        currentAmount = 0;

        if (orderText != null)
        {
            orderText.gameObject.SetActive(true);
            orderText.text = requestedAmount.ToString();
        }
    }

    public void ShowStatusText(string message)
    {
        if (orderText == null) return;
        orderText.gameObject.SetActive(true);
        orderText.text = message;
    }

    // 핫도그 전달받는 함수
    public bool ReceiveHotdog(GameObject hotdog)
    {
        if (requestedAmount == 0 || IsSatisfied) return false;

        currentAmount++;
        Destroy(hotdog);

        if (orderText != null)
        {
            int remain = requestedAmount - currentAmount;

            if (remain > 0)
            {
                orderText.text = remain.ToString();
            }
            else
            {
                orderText.gameObject.SetActive(false);

                if (CustomerManager.Instance != null)
                {
                    CustomerManager.Instance.MakeCustomerLeave(this);
                }
            }
        }

        return true;
    }

    public void ClearStatusText()
    {
        if (orderText != null)
        {
            orderText.gameObject.SetActive(false);
        }
    }
}