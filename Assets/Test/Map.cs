using UnityEngine;

public class Map : MonoBehaviour
{
    [Header("地图尺寸")]
    public int mapWidth = 20;
    public int mapHeight = 20;
    public float cellSize = 1f;

    [Header("预制体")]
    public GameObject floorPrefab;
    public GameObject agentPrefab;

    [Header("障碍物外观")]
    public Color wallColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("障碍物")]
    [Range(0f, 0.4f)]
    public float obstacleChance = 0.15f;
    public int randomSeed = 42;

    [Header("AI")]
    public int agentCount = 5;

    [Header("图层")]
    public string obstacleLayerName = "Ground";

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        Random.InitState(randomSeed);

        int obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                bool isBorder = x == 0 || x == mapWidth - 1 || y == 0 || y == mapHeight - 1;
                bool isWall = isBorder || Random.value < obstacleChance;

                Vector3 pos = new Vector3(x * cellSize, y * cellSize, 0f);

                if (isWall)
                {
                    // 障碍物由代码自动生成，带碰撞体和SpriteRenderer
                    var wall = new GameObject("Wall");
                    wall.transform.SetParent(transform);
                    wall.transform.position = pos;
                    wall.transform.localScale = Vector3.one * cellSize;

                    if (obstacleLayer >= 0)
                        wall.layer = obstacleLayer;

                    // 碰撞体
                    wall.AddComponent<BoxCollider2D>();

                    // 外观
                    var sr = wall.AddComponent<SpriteRenderer>();
                    sr.sprite = GetSquareSprite();
                    sr.color = wallColor;
                    sr.sortingOrder = 1;
                }
                else
                {
                    if (floorPrefab != null)
                        Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                }
            }
        }

        SpawnAgents();
    }

    void SpawnAgents()
    {
        if (agentPrefab == null) return;

        int spawned = 0;
        int attempts = 0;

        while (spawned < agentCount && attempts < 1000)
        {
            attempts++;
            int rx = Random.Range(1, mapWidth - 1);
            int ry = Random.Range(1, mapHeight - 1);
            Vector3 pos = new Vector3(rx * cellSize, ry * cellSize, 0f);

            Collider2D hit = Physics2D.OverlapCircle(pos, cellSize * 0.3f);
            if (hit != null) continue;

            Instantiate(agentPrefab, pos, Quaternion.identity);
            spawned++;
        }

        Debug.Log($"[MapGenerator] 生成了 {spawned} 个 Agent");
    }

    Sprite _cachedSprite;
    Sprite GetSquareSprite()
    {
        if (_cachedSprite != null) return _cachedSprite;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _cachedSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _cachedSprite;
    }

#if UNITY_EDITOR
    [ContextMenu("重新生成")]
    void Regenerate() => Generate();
#endif
}