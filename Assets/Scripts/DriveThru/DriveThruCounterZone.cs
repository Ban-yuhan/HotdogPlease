using UnityEngine;

public class DriveThruCounterZone : MonoBehaviour
{
    [Header("참조 연결")]
    [SerializeField] private DriveThruManager driveThruManager;
    [SerializeField] private MoneyStackZone moneyZone; // 돈이 생성될 돈 구역
    [SerializeField] private float serveInterval = 0.2f; // 상자 전달 속도 (초)

    private float timer = 0f;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Worker"))
        {
            PlayerStack playerStack = other.GetComponent<PlayerStack>();
            if (playerStack == null) return;

            // 창구에 대기 중인 1등 차 확인
            DriveThruCar currentCar = driveThruManager != null ? driveThruManager.CurrentWaitingCar : null;
            if (currentCar == null || !currentCar.IsWaitingAtWindow) return;

            // 플레이어가 포장 상자를 들고 있는지 확인
            if (playerStack.CurrentItemType == ItemType.Package && playerStack.CurrentCount > 0)
            {
                timer += Time.deltaTime;
                if (timer >= serveInterval)
                {
                    timer = 0f;

                    // 1. 차에게 전달 시도
                    if (!currentCar.IsOrderComplete)
                    {
                        GameObject package = playerStack.PopPackage();
                        if (package != null)
                        {
                            currentCar.ReceivePackage();
                            Destroy(package); // 전달된 상자 파괴/전달 연출
                        }
                    }

                    // 2. 주문이 완료되었으면 돈 배출 후 차량 퇴장
                    if (currentCar.IsOrderComplete)
                    {
                        int earnedMoney = currentCar.TotalPrice; // 개당 1000원
                        if (moneyZone != null)
                        {
                            moneyZone.AddMoney(earnedMoney);
                        }

                        driveThruManager.FinishCurrentCarOrder();
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        timer = 0f;
    }
}