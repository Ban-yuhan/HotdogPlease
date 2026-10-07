using UnityEngine;

public class PlayerTrashController : MonoBehaviour
{
    [SerializeField] private PlayerStack playerStack;

    private void Awake()
    {
        if (playerStack == null)
        {
            playerStack = GetComponent<PlayerStack>();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (playerStack == null) return;

        // 1. 테이블 접촉 시: 딜레이 없이 한 번에 싹 수거
        if (other.CompareTag("Table"))
        {
            Table table = other.GetComponentInParent<Table>();
            if (table != null && table.HasTrash)
            {
                table.CollectAllTrash(playerStack);
            }
        }
        // 2. 쓰레기통(TrashBin) 접촉 시: 들고 있는 쓰레기 한 번에 전부 파괴
        else if (other.CompareTag("TrashBin"))
        {
            if (playerStack.CurrentItemType == ItemType.Trash && playerStack.CurrentCount > 0)
            {
                while (playerStack.CurrentCount > 0)
                {
                    GameObject trash = playerStack.PopTrash();
                    if (trash != null)
                    {
                        Destroy(trash);
                    }
                }
            }
        }
    }
}