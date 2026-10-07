using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DeliveryRider : MonoBehaviour
{
    [SerializeField] private TMP_Text orderText;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float giveInterval = 0.15f;

    public int requestedAmount { get; private set; } = 0;
    public int currentAmount { get; private set; } = 0;
    public bool IsSatisfied => currentAmount >= requestedAmount && requestedAmount > 0;
    public bool IsWaitingAtPickup { get; private set; } = false;

    private List<Vector3> currentPath;
    private int currentPathIndex;
    private Action onMoveComplete;

    private DeliveryManager deliveryManager;
    private float giveTimer = 0f;

    // 💡 시간 제한 관련 변수
    private float currentTimer = 0f;
    private int totalPrice = 0;
    private bool isTimedOut = false;

    private void Awake()
    {
        if (orderText != null)
        {
            orderText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        MoveAlongPath();
        HandleTimeLimit();
    }

    public void InitManager(DeliveryManager manager)
    {
        deliveryManager = manager;
    }

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

    // 💡 시간 제한(timeLimit) 인자 추가
    public void InitDeliveryOrder(int minOrder, int maxOrder, int pricePerHotdog, float timeLimit)
    {
        IsWaitingAtPickup = true;
        requestedAmount = UnityEngine.Random.Range(minOrder, maxOrder + 1);
        currentAmount = 0;
        totalPrice = requestedAmount * pricePerHotdog;
        currentTimer = timeLimit;
        isTimedOut = false;

        UpdateOrderText();
    }

    // 💡 매 프레임 타이머 감소 및 시간 초과 검사
    private void HandleTimeLimit()
    {
        if (!IsWaitingAtPickup || IsSatisfied || isTimedOut) return;

        currentTimer -= Time.deltaTime;

        if (currentTimer <= 0f)
        {
            currentTimer = 0f;
            isTimedOut = true;

            if (deliveryManager != null)
            {
                deliveryManager.FailDelivery(this);
            }
            return;
        }

        UpdateOrderText();
    }

    private void OnTriggerStay(Collider other)
    {
        if (isTimedOut) return;

        if (other.CompareTag("Player"))
        {
            PlayerStack playerStack = other.GetComponent<PlayerStack>();

            if (playerStack != null && playerStack.CurrentCount > 0 && IsWaitingAtPickup && !IsSatisfied)
            {
                giveTimer += Time.deltaTime;
                if (giveTimer >= giveInterval)
                {
                    giveTimer = 0f;

                    GameObject hotdog = playerStack.PopHotdog();
                    if (hotdog != null)
                    {
                        ReceiveHotdog(hotdog);

                        if (IsSatisfied && deliveryManager != null)
                        {
                            deliveryManager.CompleteDelivery(this);
                        }
                    }
                }
            }
        }
    }

    public bool ReceiveHotdog(GameObject hotdog)
    {
        if (requestedAmount == 0 || IsSatisfied || isTimedOut) return false;

        currentAmount++;
        Destroy(hotdog);

        if (orderText != null)
        {
            if (!IsSatisfied)
            {
                UpdateOrderText();
            }
            else
            {
                orderText.gameObject.SetActive(false);
            }
        }

        return true;
    }

    // 💡 UI 표기: "$ 6,000 (0/30) [04:00]" 형태
    private void UpdateOrderText()
    {
        if (orderText != null)
        {
            orderText.gameObject.SetActive(true);
            int minutes = Mathf.FloorToInt(currentTimer / 60f);
            int seconds = Mathf.FloorToInt(currentTimer % 60f);

            orderText.text = $"{currentAmount}/{requestedAmount} $ {totalPrice:N0}   [<color=red>{minutes:D2}:{seconds:D2}</color>]";
        }
    }

    public void FinishPickup()
    {
        IsWaitingAtPickup = false;
        if (orderText != null)
        {
            orderText.gameObject.SetActive(false);
        }
    }
}