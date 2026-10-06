using System.Collections.Generic;
using UnityEngine;

public class PlayerTrashController : MonoBehaviour
{
    [Header("쓰레기 들기 세팅")]
    [SerializeField] private Transform trashHandZone; // 💡 핫도그 잡는 위치 Transform 지정

    private List<GameObject> carriedTrashes = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        // 1. 테이블에 다가갔을 때 쓰레기 뭉텅이로 가져오기
        Table table = other.GetComponent<Table>();
        if (table != null && table.IsDirty)
        {
            List<GameObject> trashes = table.PopAllTrash();
            if (trashes != null && trashes.Count > 0)
            {
                PickUpTrashBundle(trashes);
            }
            return;
        }

        // 2. 쓰레기통에 다가갔을 때 다 집어넣기
        TrashBin trashBin = other.GetComponent<TrashBin>();
        if (trashBin != null && carriedTrashes.Count > 0)
        {
            ClearAllCarriedTrash();
        }
    }

    // 쓰레기 뭉텅이 집기
    private void PickUpTrashBundle(List<GameObject> trashes)
    {
        foreach (GameObject trash in trashes)
        {
            if (trash == null) continue;

            // 부모를 핫도그 위치(trashHandZone)로 설정
            trash.transform.SetParent(trashHandZone);

            // 위로 쌓지 않고 한 위치에 뭉쳐서 겹쳐 들도록 설정
            trash.transform.localPosition = Vector3.zero;
            trash.transform.localRotation = Quaternion.identity;

            carriedTrashes.Add(trash);
        }
    }

    // 쓰레기통에 다 버리기
    private void ClearAllCarriedTrash()
    {
        foreach (GameObject trash in carriedTrashes)
        {
            if (trash != null)
            {
                Destroy(trash);
            }
        }
        carriedTrashes.Clear();
    }
}