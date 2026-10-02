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

    [Header("ÇÖµµ±× È¾¿­°£°Ý")]
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

            if (playerStack != null && !playerStack.IsFull)
            {
                giveTimer += Time.deltaTime;
                if (giveTimer >= giveInterval)
                {
                    giveTimer = 0f;

                    GameObject itemToGive = PopHotdogFromGrill();
                    if (itemToGive != null)
                    {
                        playerStack.AddHotdog(itemToGive);
                    }
                }
            }
        }
    }

    private GameObject PopHotdogFromGrill()
    {
        if (grillItems.Count == 0) return null;

        int lastIndex = grillItems.Count - 1;
        GameObject item = grillItems[lastIndex];
        grillItems.RemoveAt(lastIndex);

        UpdateUI();

        return item;
    }
}