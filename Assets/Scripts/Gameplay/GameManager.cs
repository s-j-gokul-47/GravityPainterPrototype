using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public int width = 5;
    public int length = 10;
    public float tileSpacing = 1.2f;

    void Start()
    {
        CoinManager.ResetSessionCoins();
        //GenerateLevel();
    }

    void GenerateLevel()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                Vector3 pos = new Vector3(x * tileSpacing, 0f, z * tileSpacing);
                Instantiate(tilePrefab, pos, Quaternion.identity);
            }
        }
    }
}