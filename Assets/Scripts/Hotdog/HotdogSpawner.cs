using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HotdogSpawner : MonoBehaviour
{
    // 💡 [정적 레지스트리] 싱글톤 오남용 없이 씬 내 스포너들을 안전하게 관리
    public static List<HotdogSpawner> AllSpawners { get; private set; } = new List<HotdogSpawner>();

    [SerializeField] private Transform interactionPoint; // 💡 인스펙터에서 직접 할당할 상호작용 위치

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

    public Vector3 InteractionPosition => interactionPoint != null ? interactionPoint.position : transform.position;

    private void OnEnable()
    {
        if (!AllSpawners.Contains(this))
        {
            AllSpawners.Add(this);
        }
    }

    private void OnDisable()
    {
        AllSpawners.Remove(this);
    }

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

    private void AddHotdog()
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
                        bool success = playerStack.PushHotdog(itemToGive);

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
        if (other.CompareTag("Player") || other.CompareTag("Worker"))
        {
            giveTimer = 0f;
        }
    }

    private GameObject PopHotdogFromGrill()
    {
        if (grillItems.Count == 0) return null;

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

    // 💡 [직원 연동용 인터페이스]
    public bool HasHotdog()
    {
        return grillItems != null && grillItems.Count > 0;
    }

    public GameObject TakeHotdog()
    {
        return PopHotdogFromGrill();
    }
}