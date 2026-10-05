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

            // 2. 조건 체크: 플레이어에게 핫도그가 있고, 카운터에 손님이 서있으며, 주문을 아직 다 못 채웠을 때.손님이 '카운터에 완전히 도착해서 대기 중(IsWaitingAtCounter)'일 때만 전달
            if (playerStack != null && playerStack.CurrentCount > 0 && currentCustomer != null && currentCustomer.IsWaitingAtCounter && !currentCustomer.IsSatisfied)
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
                            // 역순 경로(DoorPosition -> SpawnPoint)로 돌아가게 하고 삭제
                            customerManager.MakeCustomerLeave(currentCustomer);
                        }
                    }
                }
            }
        }
    }
}