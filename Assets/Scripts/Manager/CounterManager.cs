using System.Collections.Generic;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    [Header("위치 포인트들")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform doorPosition;        // 입장용 문
    [SerializeField] private Transform exitDoorPosition;    // 퇴장용 문
    [SerializeField] private Transform customerZone;        // 카운터 맨 앞 자리

    [Header("줄서기 세팅")]
    [SerializeField] private int maxQueueCount = 6;         // 최대 대기 인원
    [SerializeField] private float queueSpacing = 1.2f;     // 손님 간 줄 간격
    [SerializeField] private Vector3 queueDirection = Vector3.back; // 줄 서는 방향

    [Header("스폰 세팅")]
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int minOrder = 1;
    [SerializeField] private int maxOrder = 3;

    [Header("퇴장 경로 포인트들 (Pos 1 -> Pos 2 -> Pos 3)")]
    [SerializeField] private Transform exitPos1;
    [SerializeField] private Transform exitPos2;
    [SerializeField] private Transform exitPos3;

    [Header("돈 구역 참조")]
    [SerializeField] private MoneyStackZone moneyZone;

    private List<Customer> customerQueue = new List<Customer>();
    private float timer = 0f;

    // DeliveryZone에서 건네줄 '현재 맨 앞 손님'
    public Customer CurrentCustomer => (customerQueue.Count > 0) ? customerQueue[0] : null;

    private void Update()
    {
        // 줄 서 있는 손님이 최대 인원(6명)보다 적으면 계속 스폰
        if (customerQueue.Count < maxQueueCount)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                timer = 0f;
                SpawnCustomer();
            }
        }
    }

    // 💡 [추가] 줄 번호(index)에 따른 좌표 계산 헬퍼 함수
    private Vector3 GetQueuePosition(int index)
    {
        return customerZone.position + (queueDirection.normalized * index * queueSpacing);
    }

    private void SpawnCustomer()
    {
        GameObject obj = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        Customer customer = obj.GetComponent<Customer>();

        int queueIndex = customerQueue.Count;
        customerQueue.Add(customer);

        Vector3 targetQueuePos = GetQueuePosition(queueIndex);

        // 입장 경로: SpawnPoint -> DoorPosition -> 내 줄서기 위치
        List<Vector3> enterPath = new List<Vector3>
        {
            doorPosition.position,
            targetQueuePos
        };

        customer.SetPath(enterPath, () =>
        {
            // 카운터 맨 앞(0번)에 도착한 손님만 주문 생성
            if (customerQueue.Count > 0 && customerQueue[0] == customer)
            {
                customer.InitOrder(minOrder, maxOrder);
            }
        });
    }

    // 손님이 핫도그 다 받고 퇴장할 때
    public void MakeCustomerLeave(Customer customer)
    {
        if (customer == null || !customerQueue.Contains(customer)) return;

        if (moneyZone != null)
        {
            int earnedMoney = customer.requestedAmount * 100;
            moneyZone.AddMoney(earnedMoney);
        }

        // 1. 대기열(줄서기) 리스트에서 제외
        customerQueue.Remove(customer);

        // 2. 퇴장 경로 리스트 만들기 (Pos 1 -> Pos 2 -> Pos 3 순서)
        List<Vector3> leavePath = new List<Vector3>
        {
            exitPos1.position,
            exitPos2.position,
            exitPos3.position
        };

        // 3. 손님에게 퇴장 경로 전달 & 최종 목적지(Pos 3)에 도착하면 오브젝트 삭제
        customer.SetPath(leavePath, () =>
        {
            Destroy(customer.gameObject);
        });

        // 4. 뒤에 서 있던 손님들 한 칸씩 앞으로 이동
        UpdateQueuePositions();
    }

    // 남은 손님들을 한 칸씩 앞으로 당겨주는 함수
    private void UpdateQueuePositions()
    {
        for (int i = 0; i < customerQueue.Count; i++)
        {
            Customer c = customerQueue[i];
            Vector3 newQueuePos = GetQueuePosition(i);

            // 💡 [핵심 수정] c.SetPath(...) 대신 UpdateQueueTarget 호출!
            // - 걸어오던 중인 손님: 기존 경유지(Door)를 거친 후 새로 당겨진 위치로 이동
            // - 이미 줄에 서 있던 손님: 새 위치(한 칸 앞)로 스무스하게 이동
            c.UpdateQueueTarget(newQueuePos);
        }
    }
}