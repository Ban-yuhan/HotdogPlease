using System.Collections.Generic;
using UnityEngine;

public class DriveThruPackagingZone : MonoBehaviour
{
    private enum ZoneMode { None, Pack, Take, Put }

    [Header("포장 상자 세팅")]
    [SerializeField] private Transform packagedStackPoint; // 포장 상자가 쌓일 위치
    [SerializeField] private GameObject packagePrefab;     // 포장된 핫도그 프리팹
    [SerializeField] private float ySpacing = 0.25f;        // 상자 위로 쌓이는 높이 간격
    [SerializeField] private float actionInterval = 0.2f;   // 동작 속도 (초)

    private List<GameObject> stackedPackages = new List<GameObject>();
    private ZoneMode currentMode = ZoneMode.None;
    private float timer = 0f;

    public int PackageCount => stackedPackages.Count;
    public bool HasPackage => stackedPackages.Count > 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Worker"))
        {
            PlayerStack playerStack = other.GetComponent<PlayerStack>();
            if (playerStack == null) return;

            DetermineModeAtEnter(playerStack);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (currentMode == ZoneMode.None) return;

        if (other.CompareTag("Player") || other.CompareTag("Worker"))
        {
            PlayerStack playerStack = other.GetComponent<PlayerStack>();
            if (playerStack == null) return;

            timer += Time.deltaTime;
            if (timer >= actionInterval)
            {
                timer = 0f;
                ExecuteAction(playerStack);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Worker"))
        {
            currentMode = ZoneMode.None;
            timer = 0f;
        }
    }

    private void DetermineModeAtEnter(PlayerStack playerStack)
    {
        if (playerStack.CurrentItemType == ItemType.Hotdog && playerStack.CurrentCount >= 4)
        {
            currentMode = ZoneMode.Pack;
        }
        else if (playerStack.CurrentItemType == ItemType.Package && playerStack.CurrentCount > 0)
        {
            currentMode = ZoneMode.Put;
        }
        else if (playerStack.CurrentItemType == ItemType.None && HasPackage)
        {
            currentMode = ZoneMode.Take;
        }
        else
        {
            currentMode = ZoneMode.None;
        }
    }

    private void ExecuteAction(PlayerStack playerStack)
    {
        switch (currentMode)
        {
            case ZoneMode.Pack:
                if (playerStack.CurrentItemType == ItemType.Hotdog && playerStack.CurrentCount >= 4)
                {
                    int consumedCount = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (playerStack.TryConsumeHotdog())
                        {
                            consumedCount++;
                        }
                    }

                    if (consumedCount == 4 && packagePrefab != null && packagedStackPoint != null)
                    {
                        GameObject package = Instantiate(packagePrefab);
                        AddPackageToZone(package);
                    }
                }
                else
                {
                    currentMode = ZoneMode.None;
                }
                break;

            case ZoneMode.Take:
                if (HasPackage && !playerStack.IsFull)
                {
                    GameObject package = PopPackageFromZone();
                    if (package != null)
                    {
                        if (!playerStack.PushPackage(package))
                        {
                            AddPackageToZone(package);
                            currentMode = ZoneMode.None;
                        }
                    }
                }
                else
                {
                    currentMode = ZoneMode.None;
                }
                break;

            case ZoneMode.Put:
                if (playerStack.CurrentItemType == ItemType.Package && playerStack.CurrentCount > 0)
                {
                    GameObject package = playerStack.PopPackage();
                    if (package != null)
                    {
                        AddPackageToZone(package);
                    }
                }
                else
                {
                    currentMode = ZoneMode.None;
                }
                break;
        }
    }

    private void AddPackageToZone(GameObject package)
    {
        package.transform.SetParent(packagedStackPoint);

        int index = stackedPackages.Count;
        float yPos = index * ySpacing;

        package.transform.localPosition = new Vector3(0f, yPos, 0f);
        package.transform.localRotation = Quaternion.identity;

        stackedPackages.Add(package);
    }

    public GameObject PopPackageFromZone()
    {
        if (stackedPackages.Count == 0) return null;

        int lastIndex = stackedPackages.Count - 1;
        GameObject package = stackedPackages[lastIndex];
        stackedPackages.RemoveAt(lastIndex);

        return package;
    }
}