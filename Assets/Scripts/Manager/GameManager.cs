using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("재화 UI")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [SerializeField] private int currentMoney = 200;

    public int CurrentMoney => currentMoney;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

    }

    private void Start()
    {
        UpdateMoneyUI();
    }

    // 돈 획득
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateMoneyUI();
    }

    // 돈 소비 (업그레이드, 구역 해금 등)
    public bool TryUseMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            UpdateMoneyUI();
            return true; // 구매 성공
        }
        return false; // 돈 부족
    }

    private void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"$ {currentMoney:N0}"; // 1,000 단위 쉼표 표기
        }
    }

    public bool DeductMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;

            UpdateMoneyUI();

            return true;
        }
        return false;
    }
}