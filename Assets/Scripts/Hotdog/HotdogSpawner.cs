using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HotdogSpawner : MonoBehaviour
{
    [SerializeField] private TMP_Text countText;
    [SerializeField] private int maxCount = 4;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float giveInterval = 0.15f;

    [SerializeField] private GameObject HotdogPrefab;
    [SerializeField] private Transform GrillPoint;

    [Header("핫도그 횡열간격")]
    [SerializeField] private float xSpacing = 0.4f;
    [SerializeField] private float ySpacing = 0.2f;

    private List<GameObject> grillItems = new List<GameObject>();

    private float timer = 0f;
    private float giveTimer = 0f;

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        if (grillItems.Count < maxCount)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                AddHotdog();
                timer = 0f;
                UpdateUI();
            }
        }
    }

    private void UpdateUI()
    {
        if (countText == null) return;

        if (grillItems.Count >= maxCount)
        {
            countText.gameObject.SetActive(true);
            countText.text = "<color=red>FULL</color>";
        }
        else
        {
            countText.gameObject.SetActive(false);
        }
    }

    void AddHotdog()
    {
        GameObject newHotdog = Instantiate(HotdogPrefab);
        newHotdog.transform.SetParent(GrillPoint);

        int index = grillItems.Count;

        int lane = index % 2;
        int level = index / 2;

        float xPos = (lane == 0) ? -xSpacing / 2f : xSpacing / 2f;
        float yPos = level * ySpacing;
        float zPos = 0f;

        newHotdog.transform.localPosition = new Vector3(xPos, yPos, zPos);
        newHotdog.transform.localRotation = HotdogPrefab.transform.rotation;

        grillItems.Add(newHotdog);
    }

    private void OnTriggerStay(Collider other)
    {
        if ((other.CompareTag("Player") || other.CompareTag("Worker")) && grillItems.Count > 0)
        {
            PlayerStack playerStack = other.GetComponent<PlayerStack>();

            // 💡 핫도그를 들 수 있는 상태(빈손이거나 이미 핫도그를 들고 있음)일 때만 전달 시도
            if (playerStack != null && !playerStack.IsFull &&
               (playerStack.CurrentItemType == ItemType.None || playerStack.CurrentItemType == ItemType.Hotdog))
            {
                giveTimer += Time.deltaTime;
                if (giveTimer >= giveInterval)
                {
                    giveTimer = 0f;

                    GameObject itemToGive = PopHotdogFromGrill();
                    if (itemToGive != null)
                    {
                        // 💡 PushHotdog(또는 AddHotdog) 호출 결과를 bool로 확인
                        bool success = playerStack.PushHotdog(itemToGive);

                        // 💡 손에 들기 실패 시 그릴 리스트 및 부모 위치로 즉시 복구하여 프리팹 찌꺼기 방지
                        if (!success)
                        {
                            grillItems.Add(itemToGive);
                            itemToGive.transform.SetParent(GrillPoint);
                            UpdateUI();
                        }
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 💡 플레이어나 워커가 발판을 나갈 때 전달 타이머 즉시 초기화
        if (other.CompareTag("Player") || other.CompareTag("Worker"))
        {
            giveTimer = 0f;
        }
    }

    private GameObject PopHotdogFromGrill()
    {
        if (grillItems.Count == 0) return null;

        // 💡 뒤에서부터 탐색하며 파괴된 Null 오브젝트는 자동으로 지우고 정상 핫도그만 반환
        for (int i = grillItems.Count - 1; i >= 0; i--)
        {
            GameObject item = grillItems[i];
            grillItems.RemoveAt(i);

            if (item != null)
            {
                UpdateUI();
                return item;
            }
        }

        UpdateUI();
        return null;
    }
}