using System.Collections.Generic;
using UnityEngine;

public class DriveThruManager : MonoBehaviour
{
    public static DriveThruManager Instance { get; private set; }

    [Header("포장 구역 참조")]
    [SerializeField] private DriveThruPackagingZone packagingZone; // 💡 싱글톤 오남용 없이 관리자가 중계

    [Header("차량 프리팹")]
    [SerializeField] private GameObject carPrefab;

    [Header("위치 지정")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform[] approachWaypoints;
    [SerializeField] private Transform orderWindowPoint;
    [SerializeField] private Transform exitPoint;

    [Header("대기열 세팅")]
    [SerializeField] private float carSpacing = 4.5f;
    [SerializeField] private int maxQueueCount = 3;
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 10f;

    private List<DriveThruCar> carQueue = new List<DriveThruCar>();
    private float spawnTimer = 0f;
    private float currentSpawnInterval = 0f;

    public DriveThruCar CurrentWaitingCar => (carQueue.Count > 0) ? carQueue[0] : null;
    public DriveThruPackagingZone PackagingZone => packagingZone; // 💡 프로퍼티 노출

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

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

    public void FinishCurrentCarOrder()
    {
        if (carQueue.Count == 0) return;

        DriveThruCar leavingCar = carQueue[0];
        carQueue.RemoveAt(0);

        List<Vector3> exitPath = new List<Vector3> { exitPoint.position };
        leavingCar.SetFrontCar(null);
        leavingCar.SetPath(exitPath, () =>
        {
            Destroy(leavingCar.gameObject);
        });

        RebalanceCarQueue();
    }

    private void RebalanceCarQueue()
    {
        for (int i = 0; i < carQueue.Count; i++)
        {
            DriveThruCar car = carQueue[i];

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

            car.ChangeDestination(newTargetPos, () =>
            {
                if (index == 0)
                {
                    car.StartOrder();
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

    public bool HasCarWaiting()
    {
        return CurrentWaitingCar != null && CurrentWaitingCar.IsWaitingAtWindow;
    }
}