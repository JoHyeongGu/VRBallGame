using UnityEngine;
using UnityEngine.Pool;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private Exp expPrefab;
    [SerializeField] private Transform player;

    [SerializeField] private float spawnDistance = 50f;
    [SerializeField] private float spawnInterval = 1f;

    private IObjectPool<Enemy> enemyPool;
    private IObjectPool<Exp> expPool;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        enemyPool = new ObjectPool<Enemy>(
            createFunc: () => Instantiate(enemyPrefab),
            actionOnGet: (e) => e.gameObject.SetActive(true),
            actionOnRelease: (e) => e.gameObject.SetActive(false),
            actionOnDestroy: (e) => Destroy(e.gameObject),
            defaultCapacity: 50, maxSize: 300);

        expPool = new ObjectPool<Exp>(
            createFunc: () => Instantiate(expPrefab),
            actionOnGet: (g) => g.gameObject.SetActive(true),
            actionOnRelease: (g) => g.gameObject.SetActive(false),
            actionOnDestroy: (g) => Destroy(g.gameObject),
            defaultCapacity: 100, maxSize: 500);
    }

    private void Start()
    {
        InvokeRepeating(nameof(SpawnEnemy), 1f, spawnInterval);
    }

    private void SpawnEnemy()
    {
        if (player == null) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized * spawnDistance;
        Vector3 spawnPos = player.position + new Vector3(randomDir.x, 0, randomDir.y);

        Enemy enemy = enemyPool.Get();
        enemy.transform.position = spawnPos;
        enemy.Init(player, enemyPool);
    }

    public void SpawnExpGem(Vector3 position)
    {
        Exp gem = expPool.Get();
        gem.Init(position, expPool);
    }
}