using UnityEngine;
using TMPro;

public class UnlockZone : MonoBehaviour
{
    [Header("해금 설정")]
    [SerializeField] private int maxPrice = 100;        // 필요 금액
    [SerializeField] private int currentPaid = 0;       // 현재까지 지불한 금액
    [SerializeField] private float payInterval = 0.05f;  // 돈 들어가는 속도

    [Header("연결 오브젝트")]
    [SerializeField] private GameObject targetObject;   // 해금 시 나타날 건물/가구 (기본 Active False)
    [SerializeField] private GameObject nextUnlockZone; // 완납 시 새로 등장할 다음 발판
    [SerializeField] private TMP_Text priceText;        // 남은 금액 표시 UI

    private float timer = 0f;

    private void Start()
    {
        if (targetObject != null) targetObject.SetActive(false);
        if (nextUnlockZone != null) nextUnlockZone.SetActive(false);

        UpdateUI();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 💡 GameManager의 현재 소지금(CurrentMoney) 참조
            if (GameManager.Instance != null && GameManager.Instance.CurrentMoney > 0 && currentPaid < maxPrice)
            {
                timer += Time.deltaTime;
                if (timer >= payInterval)
                {
                    timer = 0f;

                    // GameManager에서 1원씩 차감
                    GameManager.Instance.DeductMoney(10);
                    currentPaid += 10;

                    UpdateUI();

                    if (currentPaid >= maxPrice)
                    {
                        Unlock();
                    }
                }
            }
        }
    }

    private void Unlock()
    {
        if (targetObject != null) targetObject.SetActive(true);
        if (nextUnlockZone != null) nextUnlockZone.SetActive(true);

        Destroy(gameObject);
    }

    private void UpdateUI()
    {
        if (priceText != null)
        {
            int remain = maxPrice - currentPaid;
            priceText.text = remain.ToString();
        }
    }
}