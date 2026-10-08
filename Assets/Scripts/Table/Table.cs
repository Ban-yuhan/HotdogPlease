using System.Collections.Generic;
using UnityEngine;

public class Table : MonoBehaviour
{
    [Header("위치 및 에셋 세팅")]
    [SerializeField] private Transform customerPosition; // 손님 앉는/서는 위치
    [SerializeField] private Transform trashSpawnPoint;  // 쓰레기 생성 기준점 (테이블 상판 위)
    [SerializeField] private GameObject trashPrefab;     // 쓰레기 프리팹
    [SerializeField] private MoneyStackZone moneyZone;   // 테이블 전용 돈 구역

    [Header("쓰레기 수거 간격")]
    [SerializeField] private float cleanInterval = 0.15f;

    public bool HasTrash => spawnedTrashes.Count > 0;
    public Transform CustomerPosition => customerPosition;
    public MoneyStackZone TableMoneyZone => moneyZone;

    public bool IsOccupied { get; private set; } = false; // 손님이 앉아서 먹는 중인가?
    public bool IsReserved { get; private set; } = false; // 💡 예약 상태 추가
    public bool IsDirty { get; private set; } = false;    // 쓰레기가 남아있는 상태인가?

    // 손님이 앉을 수 있는 깨끗하고 비어있는 상태인지 확인
    public bool IsAvailable => !IsOccupied && !IsReserved && !HasTrash;

    private List<GameObject> spawnedTrashes = new List<GameObject>();
    private float cleanTimer = 0f;

    public void Reserve()
    {
        IsReserved = false;
        IsOccupied = true;
    }

    public bool Occupy()
    {
        if (!IsAvailable) return false;
        IsOccupied = true;
        return true;
    }

    // 식사 완료 시 돈 배출 & 쓰레기 2~3개 생성
    public void FinishEating(int earnedMoney)
    {
        IsOccupied = false;
        IsDirty = true; // 쓰레기가 남아있어 이용 불가 상태로 변경

        // 1. 돈 배출
        if (moneyZone != null)
        {
            moneyZone.AddMoney(earnedMoney);
        }

        // 2. 쓰레기 2~3개 랜덤 생성
        int trashCount = Random.Range(2, 4);
        Transform spawnOrigin = trashSpawnPoint != null ? trashSpawnPoint : transform;

        for (int i = 0; i < trashCount; i++)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-0.25f, 0.25f), 0f, Random.Range(-0.25f, 0.25f));
            GameObject trash = Instantiate(trashPrefab, spawnOrigin.position + randomOffset, Quaternion.identity, spawnOrigin);
            spawnedTrashes.Add(trash);
        }
    }

    // 💡 플레이어가 테이블 콜라이더 영역 안에 들어와 있을 때 자동 수거
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && HasTrash)
        {
            PlayerStack playerStack = other.GetComponent<PlayerStack>();
            if (playerStack != null)
            {
                cleanTimer += Time.deltaTime;
                if (cleanTimer >= cleanInterval)
                {
                    cleanTimer = 0f;
                    CollectAllTrash(playerStack);
                }
            }
        }
    }

    // 💡 쓰레기를 1개씩 손으로 옮기는 핵심 로직
    public void CollectAllTrash(PlayerStack playerStack)
    {
        if (spawnedTrashes.Count == 0) return;

        // 역순으로 순회하며 플레이어 손에 한 번에 담음
        for (int i = spawnedTrashes.Count - 1; i >= 0; i--)
        {
            GameObject trash = spawnedTrashes[i];

            // 플레이어 스택에 담기 성공 시 테이블 리스트에서 제거
            if (playerStack.PushTrash(trash))
            {
                spawnedTrashes.RemoveAt(i);
            }
            else
            {
                // 플레이어 가방/손이 MAX에 도달하면 중단
                break;
            }
        }

        // 쓰레기를 모두 가져갔으면 깨끗한 상태로 변경
        if (spawnedTrashes.Count == 0)
        {
            IsDirty = false;
        }
    }

    // 외부(개발용/청소부 등)에서 무조건 강제로 청소할 때 호출할 함수
    public void ClearTrash()
    {
        foreach (GameObject trash in spawnedTrashes)
        {
            if (trash != null) Destroy(trash);
        }
        spawnedTrashes.Clear();
        IsDirty = false;
    }

    public void ClearTable()
    {
        IsOccupied = false;
        IsReserved = false;
    }
}