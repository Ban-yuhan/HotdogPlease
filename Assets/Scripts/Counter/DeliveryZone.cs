using UnityEngine;

public class DeliveryZone : MonoBehaviour
{
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private float giveInterval = 0.15f; // 핫도그 전달 속도

    private float giveTimer = 0f;

    private void OnTriggerStay(Collider other)
    {
        // 1. 플레이어(또는 알바생) 감지
        if (other.CompareTag("Player") || other.CompareTag("Worker"))
        {
            PlayerStack playerStack = other.GetComponent<PlayerStack>();
            Customer currentCustomer = customerManager != null ? customerManager.CurrentCustomer : null;

            // 💡 2. 조건 수정:
            // - 플레이어에게 핫도그가 있고(CurrentCount > 0)
            // - 카운터 구역에 손님이 있으며(IsAtCounter)
            // - "No Table!" 상태가 아니고 주문 수량이 생성되었고(requestedAmount > 0)
            // - 아직 주문을 다 채우지 못했을 때(!IsSatisfied)
            if (playerStack != null &&
                playerStack.CurrentCount > 0 &&
                currentCustomer != null &&
                currentCustomer.IsAtCounter &&
                currentCustomer.requestedAmount > 0 &&
                !currentCustomer.IsSatisfied)
            {
                giveTimer += Time.deltaTime;
                if (giveTimer >= giveInterval)
                {
                    giveTimer = 0f;

                    // 플레이어 스택에서 맨 위 핫도그 1개 뽑기
                    GameObject hotdog = playerStack.PopHotdog();

                    if (hotdog != null)
                    {
                        // 손님에게 핫도그 전달
                        currentCustomer.ReceiveHotdog(hotdog);

                        // 3. 주문이 모두 완료되었는지 확인
                        if (currentCustomer.IsSatisfied)
                        {
                            // 확보된 테이블로 이동 및 식사 실행
                            customerManager.MakeCustomerLeave(currentCustomer);
                        }
                    }
                }
            }
        }
    }
}