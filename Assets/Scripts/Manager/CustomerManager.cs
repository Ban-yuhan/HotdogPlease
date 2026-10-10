using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    [Header("위치 포인트들")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform doorPosition;        // 입장용 문
    [SerializeField] private Transform exitDoorPosition;    // 퇴장용 문
    [SerializeField] private Transform customerZone;        // 카운터 맨 앞 자리

    [Header("줄서기 세팅")]
    [SerializeField] private int maxQueueCount = 6;         // 최대 대기 인원
    [SerializeField] private float queueSpacing = 1.2f;      // 손님 간 줄 간격
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

    public Customer CurrentCustomer => (customerQueue.Count > 0) ? customerQueue[0] : null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetNextSpawnInterval();
    }

    private void Update()
    {
        if (customerZone == null || !customerZone.gameObject.activeInHierarchy) return;

        if (customerQueue.Count < maxQueueCount)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= currentSpawnInterval)
            {
                spawnTimer = 0f;
                SpawnCustomer();
                SetNextSpawnInterval();
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

    // CustomerZone 주문 처리 루틴
    private IEnumerator ProcessCustomerOrderRoutine(Customer customer)
    {
        while (customerQueue.Count > 0 && customerQueue[0] == customer && customer.requestedAmount == 0)
        {
            Table availableTable = TableManager.Instance != null ? TableManager.Instance.GetAvailableTable() : null;

            if (availableTable != null)
            {
                availableTable.Reserve();
                customer.AssignedTable = availableTable;

                customer.InitOrder(minOrder, maxOrder);
                yield break;
            }
            else
            {
                // 💡 유니티 Find API를 쓰지 않고 TableManager에게 활성화된 테이블 유무를 직접 확인!
                bool hasActiveTable = TableManager.Instance != null && TableManager.Instance.HasAnyActiveTable();

                // 씬에 해금된 테이블이 1개도 없다면 -> 테이크아웃 전용 주문 생성
                if (!hasActiveTable)
                {
                    customer.AssignedTable = null;
                    customer.InitOrder(minOrder, maxOrder);
                    yield break;
                }

                // 테이블은 있으나 만석인 경우에만 "No Table!" 표시
                if (customer.HasArrived)
                {
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

        int counterMoney = customer.requestedAmount * 100;
        if (moneyZone != null)
        {
            moneyZone.AddMoney(counterMoney);
        }

        customerQueue.Remove(customer);
        UpdateQueuePositions();

        // 💡 테이블이 지정되어 있으면 식사하러 가고, 없으면(테이크아웃) 즉시 퇴장
        if (customer.AssignedTable != null)
        {
            StartCoroutine(GoToTableAndEatRoutine(customer));
        }
        else
        {
            customer.ClearStatusText();
            SendCustomerToExit(customer);
        }
    }

    private IEnumerator GoToTableAndEatRoutine(Customer customer)
    {
        Table availableTable = customer.AssignedTable;

        while (availableTable == null)
        {
            if (TableManager.Instance != null)
            {
                availableTable = TableManager.Instance.GetAvailableTable();
                if (availableTable != null)
                {
                    availableTable.Reserve();
                    customer.AssignedTable = availableTable;
                }
            }

            if (availableTable == null)
            {
                customer.ShowStatusText("No Table!");
                yield return new WaitForSeconds(0.5f);
            }
        }

        customer.ClearStatusText();
        availableTable.Occupy();

        Vector3 startPos = customer.transform.position;
        Vector3 targetPos = availableTable.CustomerPosition.position;

        NavMeshPath navPath = new NavMeshPath();
        List<Vector3> tablePath = new List<Vector3>();

        if (NavMesh.CalculatePath(startPos, targetPos, NavMesh.AllAreas, navPath))
        {
            foreach (Vector3 corner in navPath.corners)
            {
                tablePath.Add(corner);
            }
        }
        else
        {
            tablePath.Add(targetPos);
        }

        bool reachedTable = false;
        customer.SetPath(tablePath, () => { reachedTable = true; });

        yield return new WaitUntil(() => reachedTable);

        yield return new WaitForSeconds(4.0f);

        int tipMoney = (customer.requestedAmount * 100) / 2;
        availableTable.FinishEating(tipMoney);

        SendCustomerToExit(customer);
    }

    public void SendCustomerToExit(Customer customer)
    {
        Vector3 startPos = customer.transform.position;
        Vector3 firstExitTarget = exitPos1.position;

        NavMeshPath navPath = new NavMeshPath();
        List<Vector3> leavePath = new List<Vector3>();

        if (NavMesh.CalculatePath(startPos, firstExitTarget, NavMesh.AllAreas, navPath))
        {
            foreach (Vector3 corner in navPath.corners)
            {
                leavePath.Add(corner);
            }
        }
        else
        {
            leavePath.Add(firstExitTarget);
        }

        if (exitPos2 != null) leavePath.Add(exitPos2.position);
        if (exitPos3 != null) leavePath.Add(exitPos3.position);

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