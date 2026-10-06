using System.Collections.Generic;
using UnityEngine;

public class TableManager : MonoBehaviour
{
    public static TableManager Instance { get; private set; }

    [SerializeField] private List<Table> allTables = new List<Table>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 💡 식사 중(IsOccupied)도 아니고, 쓰레기(IsDirty)도 없는 깨끗한 테이블만 반환
    public Table GetAvailableTable()
    {
        foreach (Table table in allTables)
        {
            // IsAvailable = (!IsOccupied && !IsDirty)
            if (table != null && table.IsAvailable)
            {
                return table;
            }
        }

        // 4개 테이블 모두 사용 중이거나 쓰레기가 남은 상태면 null 반환
        return null;
    }
}