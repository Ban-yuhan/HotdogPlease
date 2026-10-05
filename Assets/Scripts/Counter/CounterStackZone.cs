using System.Collections.Generic;
using UnityEngine;

public class CounterStackZone : MonoBehaviour
{
    private enum ZoneMode { None, Put, Take }

    [Header("카운터 쌓기 세팅")]
    [SerializeField] private Transform stackPosition;     // 핫도그가 쌓일 위치 (StackPosition)
    [SerializeField] private float ySpacing = 0.2f;       // 높이 간격
    [SerializeField] private float transferInterval = 0.15f; // 주고받는 속도

    //[Header("핫도그 누워있는 각도 조절")]
    //[SerializeField] private Vector3 hotdogRotation = new Vector3(0f, 0f, 90f);

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
            if (playerStack.CurrentCount > 0)
            {
                currentMode = ZoneMode.Put;
            }
            // 2. 맨손으로 들어오면 -> 집어들기(Take) 모드
            else
            {
                currentMode = ZoneMode.Take;
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
                    if (playerStack.CurrentCount > 0)
                    {
                        GameObject hotdog = playerStack.PopHotdog();
                        if (hotdog != null)
                        {
                            AddHotdogToCounter(hotdog);
                        }
                    }
                    else
                    {
                        // 손이 비면 아무것도 안 함 (다시 집어 들지 않도록 모드 중단)
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
                            playerStack.AddHotdog(hotdog);
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

        // 2. 위로 높이 쌓기
        hotdog.transform.localPosition = new Vector3(0f, yPos, 0f);

        // 3. 회전은 rotation이 아닌 'localRotation'으로 프리팹 원본값 복사
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