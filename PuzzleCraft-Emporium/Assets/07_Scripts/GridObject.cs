using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class GridObject
{
    public GameObject prefab; // The prefab to be placed on the grid
    public int x;             // The x-coordinate on the grid
    public int y;             // The y-coordinate on the grid
    public int rarity;        // The rarity level of the object

    // Optional constructor to initialize the GridObject with specific values
    public GridObject(GameObject prefab, int x, int y, int rarity)
    {
        this.prefab = prefab;
        this.x = x;
        this.y = y;
        this.rarity = rarity;
    }
}