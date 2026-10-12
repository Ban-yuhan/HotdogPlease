using System.Collections.Generic;
using UnityEngine;

public class CounterStackZone : MonoBehaviour
{
    public static CounterStackZone ActiveZone { get; private set; }

    private enum ZoneMode { None, Put, Take }

    [Header("카운터 쌓기 세팅")]
    [SerializeField] private Transform stackPosition;
    [SerializeField] private float ySpacing = 0.2f;
    [SerializeField] private float transferInterval = 0.15f;

    [Header("프리팹 세팅")]
    [SerializeField] private GameObject hotdogPrefab;

    private List<GameObject> stackedItems = new List<GameObject>();
    private ZoneMode currentMode = ZoneMode.None;
    private float timer = 0f;

    // 💡 한도가 없으므로 IsFull은 항상 false, CurrentCount만 제공
    public bool IsFull => false;
    public int CurrentCount => stackedItems.Count;

    private void OnEnable()
    {
        ActiveZone = this;
    }

    private void OnDisable()
    {
        if (ActiveZone == this) ActiveZone = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Worker"))
        {
            PlayerStack playerStack = other.GetComponent<PlayerStack>();
            if (playerStack == null) return;

            if (playerStack.CurrentItemType == ItemType.Hotdog && playerStack.CurrentCount > 0)
            {
                currentMode = ZoneMode.Put;
            }
            else if (playerStack.CurrentItemType == ItemType.None)
            {
                currentMode = ZoneMode.Take;
            }
            else
            {
                currentMode = ZoneMode.None;
            }
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
            if (timer >= transferInterval)
            {
                timer = 0f;

                if (currentMode == ZoneMode.Put)
                {
                    if (playerStack.CurrentItemType == ItemType.Hotdog && playerStack.CurrentCount > 0)
                    {
                        GameObject hotdog = playerStack.PopHotdog();
                        if (hotdog != null)
                        {
                            AddHotdogToCounter(hotdog);
                        }
                    }
                    else
                    {
                        currentMode = ZoneMode.None;
                    }
                }
                else if (currentMode == ZoneMode.Take)
                {
                    if (!playerStack.IsFull && stackedItems.Count > 0)
                    {
                        GameObject hotdog = PopHotdogFromCounter();
                        if (hotdog != null)
                        {
                            if (!playerStack.AddHotdog(hotdog))
                            {
                                AddHotdogToCounter(hotdog);
                                currentMode = ZoneMode.None;
                            }
                        }
                    }
                    else
                    {
                        currentMode = ZoneMode.None;
                    }
                }
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

    // 💡 직원 연동용: 한도 없이 무조건 카운터에 쌓기
    public bool PushHotdog(GameObject hotdog)
    {
        AddHotdogToCounter(hotdog);
        return true;
    }

    public GameObject PopHotdog()
    {
        return PopHotdogFromCounter();
    }

    private void AddHotdogToCounter(GameObject hotdog)
    {
        hotdog.transform.SetParent(stackPosition != null ? stackPosition : transform);

        int index = stackedItems.Count;
        float yPos = index * ySpacing;

        hotdog.transform.localPosition = new Vector3(0f, yPos, 0f);

        if (hotdogPrefab != null)
        {
            hotdog.transform.localRotation = hotdogPrefab.transform.localRotation;
        }

        stackedItems.Add(hotdog);
    }

    private GameObject PopHotdogFromCounter()
    {
        if (stackedItems.Count == 0) return null;

        int lastIndex = stackedItems.Count - 1;
        GameObject item = stackedItems[lastIndex];
        stackedItems.RemoveAt(lastIndex);

        return item;
    }
}