using System.Collections.Generic;
using UnityEngine;

public class Table : MonoBehaviour
{
    [Header("위치 및 에셋 세팅")]
    [SerializeField] private Transform customerPosition; // 손님 앉는/서는 위치
    [SerializeField] private Transform trashSpawnPoint;  // 쓰레기 생성 기준점 (테이블 상판 위)
    [SerializeField] private GameObject trashPrefab;     // 쓰레기 프리팹
    [SerializeField] private MoneyStackZone moneyZone;   // 테이블 전용 돈 구역

    public bool HasTrash => spawnedTrashes.Count > 0;

    public Transform CustomerPosition => customerPosition;
    public MoneyStackZone TableMoneyZone => moneyZone;

    public bool IsOccupied { get; private set; } = false; // 손님이 앉아서 먹는 중인가?
    public bool IsDirty { get; private set; } = false;    // 💡 오타 수정 완료! (쓰레기가 남아있는 상태)

    // 손님이 앉을 수 있는 깨끗하고 비어있는 상태인지 확인
    public bool IsAvailable => !IsOccupied && !IsDirty;

    private List<GameObject> spawnedTrashes = new List<GameObject>();

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
        int trashCount = Random.Range(2, 4); // 2개 또는 3개
        Transform spawnOrigin = trashSpawnPoint != null ? trashSpawnPoint : transform;

        for (int i = 0; i < trashCount; i++)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-0.25f, 0.25f), 0f, Random.Range(-0.25f, 0.25f));
            GameObject trash = Instantiate(trashPrefab, spawnOrigin.position + randomOffset, Quaternion.identity, spawnOrigin);
            spawnedTrashes.Add(trash);
        }
    }

    // 플레이어/청소부가 쓰레기를 치울 때 호출할 함수
    public void ClearTrash()
    {
        foreach (GameObject trash in spawnedTrashes)
        {
            if (trash != null) Destroy(trash);
        }
        spawnedTrashes.Clear();
        IsDirty = false; // 다시 깨끗해져서 이용 가능!
    }


    public List<GameObject> PopAllTrash()
    {
        if (spawnedTrashes.Count == 0) return null;

        // 현재 남아있는 쓰레기 목록 복사
        List<GameObject> trashes = new List<GameObject>(spawnedTrashes);
        spawnedTrashes.Clear();

        // 테이블 상태 즉시 깨끗함으로 리셋
        IsDirty = false;

        return trashes;
    }
}