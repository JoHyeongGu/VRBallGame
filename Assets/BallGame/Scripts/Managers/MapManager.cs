using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("<color=yellow>Target Settings</color>")]
    [SerializeField] private Transform targetPlayer;
    [SerializeField] private Transform[] mapTiles;

    [Header("<color=yellow>Grid Settings</color>")]
    [SerializeField] private float tileSize = 50f;
    [SerializeField] private float updateInterval = 0.5f;

    private Vector3 currentCenterPos;
    private float nextUpdateTime;
    private float squaredTileHalf;
    private float tileOneAndHalf;
    private float tileThree;

    private void Awake()
    {
        squaredTileHalf = (tileSize * 0.5f) * (tileSize * 0.5f);
        tileOneAndHalf = tileSize * 1.5f;
        tileThree = tileSize * 3f;
    }

    private void Start()
    {
        if (targetPlayer != null)
        {
            currentCenterPos = targetPlayer.position;
        }
    }

    private void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            CheckAndRepositionTiles();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    private void CheckAndRepositionTiles()
    {
        if (targetPlayer == null || mapTiles == null || mapTiles.Length == 0) return;

        Vector3 playerPos = targetPlayer.position;
        Vector3 diff = playerPos - currentCenterPos;

        diff.y = 0f;

        if (diff.sqrMagnitude > squaredTileHalf)
        {
            UpdateTilePositions(playerPos);
            currentCenterPos = playerPos;
        }
    }

    private void UpdateTilePositions(Vector3 playerPos)
    {
        for (int i = 0; i < mapTiles.Length; i++)
        {
            Vector3 tilePos = mapTiles[i].position;
            float diffX = playerPos.x - tilePos.x;
            float diffZ = playerPos.z - tilePos.z;

            if (Mathf.Abs(diffX) > tileOneAndHalf)
            {
                tilePos.x += Mathf.Sign(diffX) * tileThree;
            }

            if (Mathf.Abs(diffZ) > tileOneAndHalf)
            {
                tilePos.z += Mathf.Sign(diffZ) * tileThree;
            }

            if (tilePos != mapTiles[i].position)
            {
                mapTiles[i].position = tilePos;
            }
        }
    }
}