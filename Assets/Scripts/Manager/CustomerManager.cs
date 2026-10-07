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
    [SerializeField] private float minSpawnInterval = 3f; // 최소 스폰 대기시간
    [SerializeField] private float maxSpawnInterval = 7f; // 최대 스폰 대기시간
    [SerializeField] private int minOrder = 1;
    [SerializeField] private int maxOrder = 3;

    private float spawnTimer = 0f;
    private float currentSpawnInterval = 0f;

    [Header("퇴장 경로 포인트들 (Pos 1 -> Pos 2 -> Pos 3)")]
    [SerializeField] private Transform exitPos1;
    [SerializeField] private Transform exitPos2;
    [SerializeField] private Transform exitPos3;

    [Header("돈 구역 참조")]
    [SerializeField] private MoneyStackZone moneyZone;

    private List<Customer> customerQueue = new List<Customer>();

    // DeliveryZone에서 건네줄 '현재 맨 앞 손님'
    public Customer CurrentCustomer => (customerQueue.Count > 0) ? customerQueue[0] : null;

    private void Start()
    {
        SetNextSpawnInterval();
    }

    private void Update()
    {
        // 최대 대기열 인원 미만일 때만 타이머 진행
        if (customerQueue.Count < maxQueueCount)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= currentSpawnInterval)
            {
                spawnTimer = 0f;
                SpawnCustomer();
                SetNextSpawnInterval(); // 💡 다음 스폰 시간 랜덤 재설정
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
                StartCoroutine(ProcessCustomerOrderRoutine(customer));
            }
        });
    }

    // CustomerZone 콜라이더 감지(IsAtCounter) 시 오더 처리 코루틴
    private IEnumerator ProcessCustomerOrderRoutine(Customer customer)
    {
        while (customerQueue.Count > 0 && customerQueue[0] == customer && customer.requestedAmount == 0)
        {
            // CustomerZone 콜라이더 영역 안으로 들어온 경우에만 검사
            if (customer.IsAtCounter)
            {
                Table availableTable = TableManager.Instance != null ? TableManager.Instance.GetAvailableTable() : null;

                if (availableTable != null)
                {
                    // 빈 테이블 존재 ➔ 오더(주문) 생성
                    customer.InitOrder(minOrder, maxOrder);
                    yield break;
                }
                else
                {
                    // 빈 테이블 없음 ➔ "No Table!" 표시하며 카운터 대기
                    customer.ShowStatusText("No Table!");
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    // 1. 카운터에서 핫도그 받기가 완료되었을 때 호출
    public void MakeCustomerLeave(Customer customer)
    {
        if (customer == null || !customerQueue.Contains(customer)) return;

        // 💡 [카운터 돈 적립] 주문 수량 * 100원을 카운터 돈 구역에 생성
        int counterMoney = customer.requestedAmount * 100;
        if (moneyZone != null)
        {
            moneyZone.AddMoney(counterMoney);
        }

        // 대기열 목록에서 제거 및 뒤 손님 앞으로 이동
        customerQueue.Remove(customer);
        UpdateQueuePositions();

        // 식사 진행 코루틴 시작
        StartCoroutine(GoToTableAndEatRoutine(customer));
    }

    private IEnumerator GoToTableAndEatRoutine(Customer customer)
    {
        Table availableTable = null;

        while (availableTable == null)
        {
            if (TableManager.Instance != null)
            {
                availableTable = TableManager.Instance.GetAvailableTable();
            }

            if (availableTable == null)
            {
                customer.ShowStatusText("No Table!");
                yield return new WaitForSeconds(0.5f);
            }
        }

        availableTable.Occupy();

        List<Vector3> tablePath = new List<Vector3> { availableTable.CustomerPosition.position };
        bool reachedTable = false;
        customer.SetPath(tablePath, () => { reachedTable = true; });

        yield return new WaitUntil(() => reachedTable);

        // 💡 "Eating..." 텍스트를 띄우지 않고 4초간 식사 진행
        yield return new WaitForSeconds(4.0f);

        int tipMoney = (customer.requestedAmount * 100) / 2;
        availableTable.FinishEating(tipMoney);

        SendCustomerToExit(customer);
    }

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

            // 💡 경로의 마지막 목적지만 새로운 대기열 위치로 업데이트
            c.UpdateQueueTarget(newQueuePos);

            // 0번(카운터 맨 앞) 자리가 되었고 아직 주문을 시작하지 않은 경우
            if (i == 0 && c.requestedAmount == 0)
            {
                StartCoroutine(ProcessCustomerOrderRoutine(c));
            }
        }
    }

    private void SetNextSpawnInterval()
    {
        currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}