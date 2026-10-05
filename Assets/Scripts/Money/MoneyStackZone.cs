using System.Collections.Generic;
using UnityEngine;

public class MoneyStackZone : MonoBehaviour
{
    [Header("돈 뭉치 프리팹 & 위치")]
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private Transform spawnPoint; // 돈이 쌓이기 시작할 기준점

    [Header("6x3 격자 간격 세팅")]
    [SerializeField] private int gridX = 6;            // 가로 6개
    [SerializeField] private int gridZ = 3;            // 세로 3개
    [SerializeField] private float xSpacing = 0.4f;    // 가로 간격
    [SerializeField] private float zSpacing = 0.3f;    // 세로 간격
    [SerializeField] private float ySpacing = 0.15f;   // 위로 쌓이는 높이 간격

    [Header("수집 속도")]
    [SerializeField] private float collectInterval = 0.05f; // 돈 수집 속도

    private int currentZoneMoney = 0;
    private List<GameObject> activeMoneyList = new List<GameObject>();
    private float collectTimer = 0f;

    // 외부(손님/테이블)에서 돈 추가 시 호출
    public void AddMoney(int amount)
    {
        currentZoneMoney += amount;
        UpdateMoneyVisuals();
    }

    private void UpdateMoneyVisuals()
    {
        // 1. 1000원 단위 층수 계산 (0원 = 0층, 100~900원 = 1층, 1000~1900원 = 2층 ...)
        int tierCount = (currentZoneMoney > 0) ? ((currentZoneMoney - 1) / 1000) + 1 : 0;

        // 2. 1층당 들어가는 돈 뭉치 총 개수 (6 * 3 = 18개)
        int itemsPerLayer = gridX * gridZ;

        // 3. 생성해야 할 총 돈 뭉치 목표 개수 (층수 * 18개)
        int targetTotalBundles = tierCount * itemsPerLayer;

        // 부족한 만큼 돈 뭉치 생성 및 6x3 격자 배치
        while (activeMoneyList.Count < targetTotalBundles)
        {
            int index = activeMoneyList.Count;

            int layer = index / itemsPerLayer; // Y축 층수 (0층, 1층, 2층...)
            int rem = index % itemsPerLayer;   // 해당 층에서의 순번
            int x = rem % gridX;               // X축 (가로 0~5)
            int z = rem / gridX;               // Z축 (세로 0~2)

            Vector3 localPos = new Vector3(x * xSpacing, layer * ySpacing, z * zSpacing);

            GameObject moneyObj = Instantiate(moneyPrefab, spawnPoint);
            moneyObj.transform.localPosition = localPos;
            moneyObj.transform.localRotation = Quaternion.identity;

            activeMoneyList.Add(moneyObj);
        }

        // 돈이 줄어들어서 층수가 내려가면 상단 층 통째로 삭제
        while (activeMoneyList.Count > targetTotalBundles)
        {
            int lastIndex = activeMoneyList.Count - 1;
            Destroy(activeMoneyList[lastIndex]);
            activeMoneyList.RemoveAt(lastIndex);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (currentZoneMoney <= 0) return;

        if (other.CompareTag("Player") || other.CompareTag("Worker"))
        {
            collectTimer += Time.deltaTime;
            if (collectTimer >= collectInterval)
            {
                collectTimer = 0f;

                int collectAmount = Mathf.Min(100, currentZoneMoney);
                currentZoneMoney -= collectAmount;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddMoney(collectAmount);
                }

                UpdateMoneyVisuals();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Worker"))
        {
            collectTimer = 0f;
        }
    }
}