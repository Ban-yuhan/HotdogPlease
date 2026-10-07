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

    // 💡 [핵심] 트리거 구역에 들어가는 "순간"의 상태만 체크하여 모드 고정!
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

    // 💡 진입 당시의 손 상태에 맞춰 진입 모드 지정
    private void DetermineModeAtEnter(PlayerStack playerStack)
    {
        // 1. 핫도그 4개 이상 들고 진입 ➔ "만들기만" 모드
        if (playerStack.CurrentItemType == ItemType.Hotdog && playerStack.CurrentCount >= 4)
        {
            currentMode = ZoneMode.Pack;
        }
        // 2. 포장 상자를 들고 진입 ➔ "두기만" 모드
        else if (playerStack.CurrentItemType == ItemType.Package && playerStack.CurrentCount > 0)
        {
            currentMode = ZoneMode.Put;
        }
        // 3. 빈손(None)으로 진입 ➔ "들기만" 모드
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
            // [만들기만]: 핫도그 4개 소모 후 상자 생성 (핫도그 소진 시 중단)
            case ZoneMode.Pack:
                if (playerStack.CurrentItemType == ItemType.Hotdog && playerStack.CurrentCount >= 4)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        GameObject hotdog = playerStack.PopHotdog();
                        if (hotdog != null) Destroy(hotdog);
                    }

                    if (packagePrefab != null && packagedStackPoint != null)
                    {
                        GameObject package = Instantiate(packagePrefab);
                        AddPackageToZone(package);
                    }
                }
                else
                {
                    // 핫도그가 4개 미만이 되면 더 이상 작업 안 하고 멈춤 (상자를 집지 않음)
                    currentMode = ZoneMode.None;
                }
                break;

            // [들기만]: 발판 위에 쌓인 상자를 플레이어 손으로 수거 (가득 차면 중단)
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

            // [두기만]: 플레이어 손의 상자를 발판으로 내려놓음 (손이 비면 중단)
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
                    // 손이 비면 멈춤 (다시 들지 않음)
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