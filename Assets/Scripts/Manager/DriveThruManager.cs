using System.Collections.Generic;
using UnityEngine;

public class DriveThruManager : MonoBehaviour
{
    [Header("차량 프리팹")]
    [SerializeField] private GameObject carPrefab;

    [Header("위치 지정")]
    [SerializeField] private Transform spawnPoint;          // 차량 스폰 위치
    [SerializeField] private Transform[] approachWaypoints; // 접근 도로 웨이포인트들
    [SerializeField] private Transform orderWindowPoint;    // 창구 위치
    [SerializeField] private Transform exitPoint;           // 출구 위치

    [Header("대기열 세팅")]
    [SerializeField] private float carSpacing = 4.5f;        // 차량 정차 간격
    [SerializeField] private int maxQueueCount = 3;         // 최대 대기 차량 수
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 10f;

    private List<DriveThruCar> carQueue = new List<DriveThruCar>();
    private float spawnTimer = 0f;
    private float currentSpawnInterval = 0f;

    public DriveThruCar CurrentWaitingCar => (carQueue.Count > 0) ? carQueue[0] : null;

    private void Start()
    {
        SetNextSpawnInterval();
    }

    private void Update()
    {
        if (orderWindowPoint == null || !orderWindowPoint.gameObject.activeInHierarchy) return;

        HandleCarSpawning();
    }

    private void HandleCarSpawning()
    {
        if (carQueue.Count < maxQueueCount)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= currentSpawnInterval)
            {
                spawnTimer = 0f;
                SpawnCar();
                SetNextSpawnInterval();
            }
        }
    }

    private void SetNextSpawnInterval()
    {
        currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void SpawnCar()
    {
        if (carPrefab == null || spawnPoint == null || orderWindowPoint == null) return;

        GameObject carObj = Instantiate(carPrefab, spawnPoint.position, spawnPoint.rotation);
        DriveThruCar newCar = carObj.GetComponent<DriveThruCar>();

        if (newCar == null) return;

        if (carQueue.Count > 0)
        {
            DriveThruCar frontCar = carQueue[carQueue.Count - 1];
            newCar.SetFrontCar(frontCar);
        }

        carQueue.Add(newCar);

        List<Vector3> path = new List<Vector3>();

        if (approachWaypoints != null)
        {
            foreach (var wp in approachWaypoints)
            {
                if (wp != null) path.Add(wp.position);
            }
        }

        int queueIndex = carQueue.Count - 1;
        path.Add(CalculateQueuePosition(queueIndex));

        newCar.SetPath(path, () =>
        {
            // 💡 대기열 0번(창구 바로 앞)에 도착한 차라면 주문 시작!
            if (carQueue.Count > 0 && carQueue[0] == newCar)
            {
                newCar.StartOrder();
            }
            else
            {
                newCar.OnArrivedAtQueue();
            }
        });
    }

    // 💡 [핵심 2] 1등 차가 주문을 완료하고 퇴장할 때 호출하는 함수
    public void FinishCurrentCarOrder()
    {
        if (carQueue.Count == 0) return;

        DriveThruCar leavingCar = carQueue[0];
        carQueue.RemoveAt(0); // 대기열 0번 제거

        // 1. 떠나는 차는 출구(Exit)로 이동 후 파괴
        List<Vector3> exitPath = new List<Vector3> { exitPoint.position };
        leavingCar.SetFrontCar(null); // 앞차 참조 해제
        leavingCar.SetPath(exitPath, () =>
        {
            Destroy(leavingCar.gameObject);
        });

        // 2. 남아있는 뒷차들의 정차 위치를 한 칸씩 앞으로 재설정하고 전진시킴
        RebalanceCarQueue();
    }

    // 뒷차들의 정차 위치 전진 업데이트
    private void RebalanceCarQueue()
    {
        for (int i = 0; i < carQueue.Count; i++)
        {
            DriveThruCar car = carQueue[i];

            // 0번 차는 앞차가 없고, 뒷차들은 자기 바로 앞차를 참조
            if (i == 0)
            {
                car.SetFrontCar(null);
            }
            else
            {
                car.SetFrontCar(carQueue[i - 1]);
            }

            Vector3 newTargetPos = CalculateQueuePosition(i);
            int index = i;

            // 💡 SetPath 대신 ChangeDestination을 호출하여 도로 경로 이탈 방지
            car.ChangeDestination(newTargetPos, () =>
            {
                if (index == 0)
                {
                    car.StartOrder(); // 1순번 도착 시 주문 개시
                }
                else
                {
                    car.OnArrivedAtQueue();
                }
            });
        }
    }

    private Vector3 CalculateQueuePosition(int index)
    {
        Vector3 forwardDir = orderWindowPoint.forward;
        return orderWindowPoint.position - (forwardDir * (index * carSpacing));
    }
}