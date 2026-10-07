using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 3D TextMeshPro 사용

public class DriveThruCar : MonoBehaviour
{
    [Header("차량 이동 세팅")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotateSpeed = 8f;
    [SerializeField] private float safeDistance = 4.5f;

    [Header("주문 3D 텍스트 세팅")]
    [SerializeField] private TextMeshPro orderText; // 3D TextMeshPro 컴포넌트

    public bool IsWaitingAtWindow { get; private set; } = false;
    public bool IsOrderComplete => currentDeliveredCount >= requiredOrderCount;
    public int RequiredOrderCount => requiredOrderCount;
    public int TotalPrice => requiredOrderCount * 1000;

    private int requiredOrderCount = 0;
    private int currentDeliveredCount = 0;

    private List<Vector3> currentPath;
    private int currentPathIndex;
    private Action onMoveComplete;
    private DriveThruCar frontCar;

    private void Awake()
    {
        // 게임 시작 시 3D 텍스트 숨기기
        if (orderText != null)
        {
            orderText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        MoveAlongPath();
    }

    public void SetFrontCar(DriveThruCar car)
    {
        frontCar = car;
    }

    public void SetPath(List<Vector3> path, Action onComplete = null)
    {
        currentPath = path;
        currentPathIndex = 0;
        onMoveComplete = onComplete;
        IsWaitingAtWindow = false;

        if (currentPath != null && currentPath.Count > 0)
        {
            Vector3 firstTarget = currentPath[0];
            Vector3 dir = firstTarget - transform.position;
            dir.y = 0f;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }

    private void MoveAlongPath()
    {
        if (currentPath == null || currentPathIndex >= currentPath.Count) return;

        if (frontCar != null)
        {
            float distanceToFrontCar = Vector3.Distance(transform.position, frontCar.transform.position);
            if (distanceToFrontCar < safeDistance) return;
        }

        Vector3 target = currentPath[currentPathIndex];
        target.y = transform.position.y;

        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        Vector3 dir = target - transform.position;
        if (dir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, target) < 0.1f)
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

    public void OnArrivedAtQueue()
    {
        IsWaitingAtWindow = true;
    }

    // 💡 창구 도착 시 1~4개 주문 생성 및 3D 텍스트 켜기
    public void StartOrder()
    {
        IsWaitingAtWindow = true;
        requiredOrderCount = UnityEngine.Random.Range(1, 5); // 1~4개
        currentDeliveredCount = 0;

        UpdateOrderUI();
        if (orderText != null)
        {
            orderText.gameObject.SetActive(true);
        }
    }

    // 💡 상자 전달받았을 때
    public bool ReceivePackage()
    {
        if (IsOrderComplete) return false;

        currentDeliveredCount++;
        UpdateOrderUI();

        if (IsOrderComplete)
        {
            if (orderText != null)
            {
                orderText.gameObject.SetActive(false);
            }
        }

        return true;
    }

    public void ChangeDestination(Vector3 newTargetPos, Action onComplete = null)
    {
        onMoveComplete = onComplete;

        // 1. 아직 도로(웨이포인트)를 따라 이동 중인 경우
        if (currentPath != null && currentPathIndex < currentPath.Count)
        {
            // 기존 웨이포인트 경로들은 그대로 유지하고, 맨 마지막 도착지(대기열 위치)만 갱신
            currentPath[currentPath.Count - 1] = newTargetPos;
        }
        // 2. 이미 도로를 다 지나서 대기열에 정차 중이었던 경우
        else
        {
            // 새 대기 위치로 전진
            SetPath(new List<Vector3> { newTargetPos }, onComplete);
        }
    }

    private void UpdateOrderUI()
    {
        if (orderText != null)
        {
            orderText.text = $"{currentDeliveredCount}/{requiredOrderCount}";
        }
    }
}