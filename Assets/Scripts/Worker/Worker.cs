using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Worker : MonoBehaviour
{
    [Header("직원 능력치")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private int maxCapacity = 2;
    [SerializeField] private float itemSpacing = 0.35f;

    [Header("위치 및 프리팹 참조")]
    [SerializeField] private Transform handStackPoint;
    [SerializeField] private GameObject hotdogPrefab;

    private NavMeshAgent agent;
    private bool isWorking = false;
    private List<GameObject> carriedItems = new List<GameObject>();

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.8f; // 💡 직원 간 여유 거리 확보
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            // 💡 [길막 방지] 직원마다 회피 우선순위를 다르게 부여해 양보 유도 (0~99, 낮은 수치가 높은 우선순위)
            agent.avoidancePriority = Random.Range(20, 80);
        }
    }

    private void OnEnable()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.enabled = false;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
            agent.enabled = true;
            agent.Warp(transform.position);
        }
    }

    private void Update()
    {
        if (!isWorking)
        {
            CheckTask();
        }
    }

    private void CheckTask()
    {
        if (CanFillCounterStack())
        {
            StartCoroutine(FillCounterStackRoutine());
        }
    }

    private bool CanFillCounterStack()
    {
        CounterStackZone stackZone = CounterStackZone.ActiveZone;
        bool hasCounterZone = stackZone != null;
        bool hasHotdogOnGrill = GetAvailableSpawner() != null;

        return hasCounterZone && hasHotdogOnGrill;
    }

    private HotdogSpawner GetAvailableSpawner()
    {
        HotdogSpawner bestSpawner = null;
        float minDistance = float.MaxValue;

        foreach (var spawner in HotdogSpawner.AllSpawners)
        {
            if (spawner != null && spawner.gameObject.activeInHierarchy && spawner.HasHotdog())
            {
                float dist = Vector3.Distance(transform.position, spawner.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestSpawner = spawner;
                }
            }
        }
        return bestSpawner;
    }

    private IEnumerator FillCounterStackRoutine()
    {
        isWorking = true;

        CounterStackZone counterZone = CounterStackZone.ActiveZone;
        if (counterZone == null)
        {
            isWorking = false;
            yield break;
        }

        int maxPickup = maxCapacity;

        // 1. 그릴에서 수거
        while (carriedItems.Count < maxPickup)
        {
            HotdogSpawner spawner = GetAvailableSpawner();
            if (spawner == null) break;

            // 💡 문자열 Find 대신 Spawner의 InteractionPosition 프로퍼티 사용
            Vector3 targetPos = spawner.InteractionPosition;
            yield return MoveToPointRoutine(targetPos);

            while (carriedItems.Count < maxPickup && spawner.HasHotdog())
            {
                GameObject hotdog = spawner.TakeHotdog();
                if (hotdog != null)
                {
                    StackItemToHand(hotdog);
                }
            }
        }

        // 2. 카운터로 이동해 쌓기
        if (carriedItems.Count > 0 && counterZone != null)
        {
            yield return MoveToPointRoutine(counterZone.transform.position);

            for (int i = carriedItems.Count - 1; i >= 0; i--)
            {
                GameObject hotdog = carriedItems[i];
                carriedItems.RemoveAt(i);

                if (hotdog != null)
                {
                    hotdog.transform.SetParent(null);
                    counterZone.PushHotdog(hotdog);
                }

                yield return new WaitForSeconds(0.12f);
            }
        }

        isWorking = false;
    }

    private void StackItemToHand(GameObject item)
    {
        if (item == null) return;

        item.transform.SetParent(handStackPoint != null ? handStackPoint : transform);

        float yOffset = carriedItems.Count * itemSpacing;
        item.transform.localPosition = new Vector3(0f, yOffset, 0f);

        if (hotdogPrefab != null)
        {
            item.transform.localRotation = hotdogPrefab.transform.rotation;
        }

        carriedItems.Add(item);
    }

    // 💡 [길막 해결 핵심] 이동 대기 루프에 속도/시간 체크로 끼임 보정
    private IEnumerator MoveToPointRoutine(Vector3 targetPos)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(targetPos);

            float stuckTimer = 0f;

            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            {
                // 💡 목적지 근처(1m 이내)에서 다른 직원에 막혀 속도가 0에 가까워지면(stuck)
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f && agent.velocity.sqrMagnitude < 0.05f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer >= 0.4f) // 0.4초간 멈춰있으면 목적지에 도착한 것으로 인정하고 루프 탈출
                    {
                        break;
                    }
                }
                else
                {
                    stuckTimer = 0f;
                }

                yield return null;
            }
        }
    }
}