using UnityEngine;
using ProceduralGeneration.Components;

public class BossManager : MonoBehaviour
{
    [Header("Boss Settings")]
    [SerializeField] private GameObject boss;
    [SerializeField] private DoorTrigger[] doorsToLock; // Các cửa cần khóa (updated to DoorTrigger)
    
    [Header("Trigger")]
    [SerializeField] private BoxCollider2D bossTrigger;
    
    private bool bossFightStarted = false;
    
    void Start()
    {
        if (bossTrigger == null)
            bossTrigger = GetComponent<BoxCollider2D>();
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !bossFightStarted)
        {
            StartBossFight();
        }
    }
    
    void StartBossFight()
    {
        bossFightStarted = true;
        
        // Khóa tất cả các cửa
        foreach (DoorTrigger door in doorsToLock)
        {
            if (door != null)
                door.LockDoor();
        }
        
        // Kích hoạt boss
        if (boss != null)
            boss.SetActive(true);
        
        Debug.Log("Boss Fight Started!");
    }
    
    public void OnBossDefeated()
    {
        // Mở khóa cửa khi boss chết
        foreach (DoorTrigger door in doorsToLock)
        {
            if (door != null)
                door.UnlockDoor();
        }
        
        Debug.Log("Boss Defeated! Doors Unlocked.");
    }
}
