using System.Collections;
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

        List<Vector3> enterPath = new List<Vector3>
        {
            doorPosition.position,
            targetQueuePos
        };

        customer.SetPath(enterPath, () =>
        {
            if (customerQueue.Count > 0 && customerQueue[0] == customer)
            {
                customer.InitOrder(minOrder, maxOrder);
            }
        });
    }

    // 손님이 핫도그를 다 받았을 때 호출
    public void MakeCustomerLeave(Customer customer)
    {
        if (customer == null || !customerQueue.Contains(customer)) return;

        // 1. 카운터 대기열 목록에서 제거 및 뒤 손님들 앞으로 당기기
        customerQueue.Remove(customer);
        UpdateQueuePositions();

        // 2. 테이블 이동 및 식사 프로세스 실행 (빈 테이블 탐색 코루틴)
        StartCoroutine(ProcessCustomerTableOrWait(customer));
    }

    // 빈 테이블 탐색 -> 이동 -> 4초 식사 -> 쓰레기 스폰 -> 퇴장 처리 코루틴
    private IEnumerator ProcessCustomerTableOrWait(Customer customer)
    {
        Table availableTable = null;

        // 빈 테이블이 나올 때까지 반복 체크
        while (availableTable == null)
        {
            if (TableManager.Instance != null)
            {
                availableTable = TableManager.Instance.GetAvailableTable();
            }

            if (availableTable == null)
            {
                // 빈 테이블이 없으면 머리 위에 "No Table!" 표시 후 1초 대기
                customer.ShowStatusText("No Table!");
                yield return new WaitForSeconds(1.0f);
            }
        }

        // 빈 테이블 확보
        availableTable.Occupy();

        // 테이블 자릿수로 이동
        List<Vector3> tablePath = new List<Vector3> { availableTable.CustomerPosition.position };

        bool reachedTable = false;
        customer.SetPath(tablePath, () => { reachedTable = true; });

        yield return new WaitUntil(() => reachedTable);

        // 4초 동안 식사 진행
        customer.ShowStatusText("Eating...");
        yield return new WaitForSeconds(4.0f);

        // 식사 완료: 돈 배출 + 쓰레기 2~3개 스폰
        int earnedMoney = customer.requestedAmount * 100;
        availableTable.FinishEating(earnedMoney);

        // 퇴장 경로 이동 후 오브젝트 삭제
        SendCustomerToExit(customer);
    }

    // 퇴장 경로 (Pos 1 -> Pos 2 -> Pos 3)
    private void SendCustomerToExit(Customer customer)
    {
        List<Vector3> leavePath = new List<Vector3>
        {
            exitPos1.position,
            exitPos2.position,
            exitPos3.position
        };

        customer.SetPath(leavePath, () =>
        {
            Destroy(customer.gameObject);
        });
    }

    private void UpdateQueuePositions()
    {
        for (int i = 0; i < customerQueue.Count; i++)
        {
            Customer c = customerQueue[i];
            Vector3 newQueuePos = GetQueuePosition(i);
            c.UpdateQueueTarget(newQueuePos);
        }
    }
}