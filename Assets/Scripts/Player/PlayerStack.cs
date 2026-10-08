using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 💡 아이템 종류 구분
public enum ItemType
{
    None,
    Hotdog,
    Trash,
    Package
}

public class PlayerStack : MonoBehaviour
{
    [SerializeField] private Transform stackPoint;
    [SerializeField] private int maxStackCount = 4;
    [SerializeField] private TMP_Text MaxText;

    [Header("간격 세팅")]
    [SerializeField] private float ySpacing = 0.3f;

    [Header("프리팹 기준 값")]
    [SerializeField] private GameObject hotdogPrefab;

    private List<GameObject> stackedItems = new List<GameObject>();

    // 💡 현재 들고 있는 아이템 상태
    public ItemType CurrentItemType { get; private set; } = ItemType.None;

    private void Start()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (MaxText == null) return;

        if (IsFull)
        {
            MaxText.gameObject.SetActive(true);
            MaxText.text = "<color=red>MAX</color>";
        }
        else
        {
            MaxText.gameObject.SetActive(false);
        }
    }

    // 💡 [기존 스크립트 호환용] AddHotdog 호출 시 PushHotdog 실행
    public bool AddHotdog(GameObject hotdogObj)
    {
        return PushHotdog(hotdogObj);
    }

    // 💡 핫도그 추가 (쓰레기를 들고 있을 땐 거부)
    public bool PushHotdog(GameObject hotdogObj)
    {
        if (CurrentItemType == ItemType.Trash || IsFull || hotdogObj == null) return false;

        hotdogObj.transform.SetParent(stackPoint);

        int index = stackedItems.Count;
        float yPos = index * ySpacing;

        hotdogObj.transform.localPosition = new Vector3(0f, yPos, 0f);
        if (hotdogPrefab != null)
        {
            hotdogObj.transform.localRotation = hotdogPrefab.transform.rotation;
        }

        stackedItems.Add(hotdogObj);
        CurrentItemType = ItemType.Hotdog; // 핫도그 타입 고정

        UpdateUI();
        return true;
    }

    // 💡 쓰레기 추가 (핫도그를 들고 있을 땐 거부)
    public bool PushTrash(GameObject trashObj)
    {
        // 💡 핫도그를 들고 있거나 null인 경우만 거부 (IsFull 체크 제거로 무제한 획득!)
        if (CurrentItemType == ItemType.Hotdog || trashObj == null) return false;

        trashObj.transform.SetParent(stackPoint);

        // 좁은 범위(-0.15 ~ 0.15) 내에 겹치지 않게 퍼뜨려 배치
        Vector3 randomOffset = new Vector3(
            Random.Range(-0.15f, 0.15f),
            0f,
            Random.Range(-0.15f, 0.15f)
        );

        trashObj.transform.localPosition = randomOffset;
        trashObj.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        stackedItems.Add(trashObj);
        CurrentItemType = ItemType.Trash;

        UpdateUI();
        return true;
    }

    // 💡 핫도그 꺼내기 (손님/배달원에게 전달)
    public GameObject PopHotdog()
    {
        if (CurrentItemType != ItemType.Hotdog || stackedItems.Count == 0) return null;

        int lastIndex = stackedItems.Count - 1;
        GameObject item = stackedItems[lastIndex];
        stackedItems.RemoveAt(lastIndex);

        if (stackedItems.Count == 0) CurrentItemType = ItemType.None;

        UpdateUI();

        // 💡 [핵심] 부모 관계를 즉시 끊어서 씬 상공/스택 포인트 위치 잔상을 방지
        if (item != null)
        {
            item.transform.SetParent(null);
        }

        return item;
    }

    // 💡 쓰레기 꺼내기 (쓰레기통에 버리기)
    public GameObject PopTrash()
    {
        if (CurrentItemType != ItemType.Trash || stackedItems.Count == 0) return null;

        return PopItem();
    }

    // 💡 아이템 제거 공통 로직
    private GameObject PopItem()
    {
        if (stackedItems.Count == 0) return null;

        int lastIndex = stackedItems.Count - 1;
        GameObject itemToPop = stackedItems[lastIndex];
        stackedItems.RemoveAt(lastIndex);

        // 손에 든 것이 전부 없어지면 None 타입으로 리셋
        if (stackedItems.Count == 0)
        {
            CurrentItemType = ItemType.None;
        }

        UpdateUI();
        return itemToPop;
    }

    public bool PushPackage(GameObject packageObj)
    {
        // 핫도그/쓰레기를 들고 있거나, 손이 가득 차면 거부
        if (CurrentItemType == ItemType.Hotdog || CurrentItemType == ItemType.Trash || IsFull || packageObj == null)
            return false;

        packageObj.transform.SetParent(stackPoint);

        int index = stackedItems.Count;
        float yPos = index * ySpacing;

        packageObj.transform.localPosition = new Vector3(0f, yPos, 0f);
        packageObj.transform.localRotation = Quaternion.identity;

        stackedItems.Add(packageObj);
        CurrentItemType = ItemType.Package;

        UpdateUI();
        return true;
    }

    public GameObject PopPackage()
    {
        if (CurrentItemType != ItemType.Package || stackedItems.Count == 0)
            return null;

        int lastIndex = stackedItems.Count - 1;
        GameObject package = stackedItems[lastIndex];
        stackedItems.RemoveAt(lastIndex);

        if (stackedItems.Count == 0)
        {
            CurrentItemType = ItemType.None;
        }

        UpdateUI();
        return package;
    }

    public bool IsFull
    {
        get
        {
            // 💡 핫도그/패키지를 들고 있을 때만 최대 개수(maxStackCount) 제한 적용
            if (CurrentItemType == ItemType.Hotdog || CurrentItemType == ItemType.Package)
            {
                return stackedItems.Count >= maxStackCount;
            }

            // 쓰레기나 맨손 상태일 때는 제한 없음 (항상 false)
            return false;
        }
    }

    public bool TryConsumeHotdog()
    {
        GameObject hotdog = PopHotdog();
        if (hotdog != null)
        {
            Destroy(hotdog);
            return true;
        }
        return false;
    }

    public int CurrentCount => stackedItems.Count;
}