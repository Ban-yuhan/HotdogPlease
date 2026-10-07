using System.Collections.Generic;
using UnityEngine;

public class CounterStackZone : MonoBehaviour
{
    private enum ZoneMode { None, Put, Take }

    [Header("카운터 쌓기 세팅")]
    [SerializeField] private Transform stackPosition;     // 핫도그가 쌓일 위치 (StackPosition)
    [SerializeField] private float ySpacing = 0.2f;       // 높이 간격
    [SerializeField] private float transferInterval = 0.15f; // 주고받는 속도

    [Header("프리팹 세팅")]
    [SerializeField] private GameObject hotdogPrefab;

    private List<GameObject> stackedItems = new List<GameObject>();
    private ZoneMode currentMode = ZoneMode.None;
    private float timer = 0f;

    // 영역에 처음 진입할 때 모드 결정!
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Worker"))
        {
            PlayerStack playerStack = other.GetComponent<PlayerStack>();
            if (playerStack == null) return;

            // 1. 핫도그를 들고 들어오면 -> 내려놓기(Put) 모드
            if (playerStack.CurrentItemType == ItemType.Hotdog && playerStack.CurrentCount > 0)
            {
                currentMode = ZoneMode.Put;
            }
            // 2. 맨손(None)으로 들어오면 -> 집어들기(Take) 모드
            else if (playerStack.CurrentItemType == ItemType.None)
            {
                currentMode = ZoneMode.Take;
            }
            // 3. 쓰레기를 들고 오면 아무 작업도 하지 않음
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

                // [Put 모드] 플레이어 -> 카운터로 쌓기
                if (currentMode == ZoneMode.Put)
                {
                    // 핫도그를 들고 있고 1개 이상일 때만 진행
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
                        // 손이 비거나 핫도그가 없으면 모드 중단 (다시 집어 들지 않도록)
                        currentMode = ZoneMode.None;
                    }
                }
                // [Take 모드] 카운터 -> 플레이어로 가져오기
                else if (currentMode == ZoneMode.Take)
                {
                    if (!playerStack.IsFull && stackedItems.Count > 0)
                    {
                        GameObject hotdog = PopHotdogFromCounter();
                        if (hotdog != null)
                        {
                            // 💡 AddHotdog 실패 시 핫도그 증발 방지를 위해 카운터에 복구
                            if (!playerStack.AddHotdog(hotdog))
                            {
                                AddHotdogToCounter(hotdog);
                                currentMode = ZoneMode.None;
                            }
                        }
                    }
                    else
                    {
                        // 가득 차거나 카운터가 비면 동작 중단
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

    // 카운터에 핫도그 쌓기
    private void AddHotdogToCounter(GameObject hotdog)
    {
        hotdog.transform.SetParent(stackPosition);

        int index = stackedItems.Count;
        float yPos = index * ySpacing;

        hotdog.transform.localPosition = new Vector3(0f, yPos, 0f);

        if (hotdogPrefab != null)
        {
            hotdog.transform.localRotation = hotdogPrefab.transform.localRotation;
        }

        stackedItems.Add(hotdog);
    }

    // 카운터에서 맨 위 핫도그 꺼내기
    private GameObject PopHotdogFromCounter()
    {
        if (stackedItems.Count == 0) return null;

        int lastIndex = stackedItems.Count - 1;
        GameObject item = stackedItems[lastIndex];
        stackedItems.RemoveAt(lastIndex);

        return item;
    }
}