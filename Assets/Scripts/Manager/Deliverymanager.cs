using System.Collections.Generic;
using UnityEngine;

public class DeliveryManager : MonoBehaviour
{
    [Header("위치 참조")]
    [SerializeField] private Transform riderSpawnPoint;
    [SerializeField] private Transform riderStayPoint;

    [Header("프리팹")]
    [SerializeField] private GameObject deliveryRiderPrefab;

    [Header("스폰 쿨타임 (초)")]
    [SerializeField] private float minInterval = 40f;
    [SerializeField] private float maxInterval = 70f;

    [Header("주문 세팅")]
    [SerializeField] private int minOrder = 20;
    [SerializeField] private int maxOrder = 40;
    [SerializeField] private int pricePerHotdog = 200;

    [Header("시간 제한 세팅 (초)")]
    [SerializeField] private float deliveryTimeLimit = 240f; // 기본 4분 (240초)

    private DeliveryRider currentRider = null;
    private float timer = 0f;
    private float currentCoolTime = 0f;

    public DeliveryRider CurrentRider => currentRider;

    private void Start()
    {
        SetNextCoolTime();

    }

    private void Update()
    {
        if (currentRider == null)
        {
            timer += Time.deltaTime;
            if (timer >= currentCoolTime)
            {
                timer = 0f;
                SpawnDeliveryRider();
            }
        }
    }

    private void SetNextCoolTime()
    {
        currentCoolTime = Random.Range(minInterval, maxInterval);
    }

    private void SpawnDeliveryRider()
    {
        GameObject obj = Instantiate(deliveryRiderPrefab, riderSpawnPoint.position, riderSpawnPoint.rotation);
        currentRider = obj.GetComponent<DeliveryRider>();

        currentRider.InitManager(this);

        List<Vector3> path = new List<Vector3> { riderStayPoint.position };

        currentRider.SetPath(path, () =>
        {
            currentRider.InitDeliveryOrder(minOrder, maxOrder, pricePerHotdog, deliveryTimeLimit);
        });
    }

    // 성공 시 호출
    public void CompleteDelivery(DeliveryRider rider)
    {
        if (rider == null || rider != currentRider) return;

        rider.FinishPickup();

        int totalReward = rider.requestedAmount * pricePerHotdog;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(totalReward);
        }

        LeaveRider(rider);
    }

    // 💡 시간 초과 실패 시 호출 (돈 적립 없이 퇴장)
    public void FailDelivery(DeliveryRider rider)
    {
        if (rider == null || rider != currentRider) return;

        rider.FinishPickup();
        LeaveRider(rider);
    }

    private void LeaveRider(DeliveryRider rider)
    {
        List<Vector3> leavePath = new List<Vector3> { riderSpawnPoint.position };

        rider.SetPath(leavePath, () =>
        {
            Destroy(rider.gameObject);
            currentRider = null;
            SetNextCoolTime(); // 다음 스폰 쿨타임 재설정
        });
    }
}