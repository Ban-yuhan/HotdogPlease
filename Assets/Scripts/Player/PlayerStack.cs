using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerStack : MonoBehaviour
{
    [SerializeField] private Transform stackPoint;      
    [SerializeField] private int maxStackCount = 4;

    [SerializeField] private TMP_Text MaxText;

    [Header("간격 세팅")]
    [SerializeField] private float ySpacing = 0.3f;

    private List<GameObject> stackedItems = new List<GameObject>();


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


    public bool AddHotdog(GameObject hotdogObj)
    {
        if (IsFull || hotdogObj == null) return false;

        hotdogObj.transform.SetParent(stackPoint);

        int index = stackedItems.Count;
        float yPos = index * ySpacing;

        hotdogObj.transform.localPosition = new Vector3(0f, yPos, 0f);
        hotdogObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // 4. 리스트에 저장
        stackedItems.Add(hotdogObj);

        UpdateUI();
        return true;
    }

    public GameObject PopHotdog()
    {
        if (stackedItems.Count == 0) return null;

        int lastIndex = stackedItems.Count - 1;
        GameObject itemToPop = stackedItems[lastIndex];
        stackedItems.RemoveAt(lastIndex);

        return itemToPop;
    }

    public bool IsFull => stackedItems.Count >= maxStackCount;
    public int CurrentCount => stackedItems.Count;
}