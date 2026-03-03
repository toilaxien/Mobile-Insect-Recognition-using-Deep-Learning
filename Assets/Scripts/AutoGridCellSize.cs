using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class AutoGridCellSize : MonoBehaviour
{
    public int columnCount = 2;
    public float spacingX = 20f;
    public float cellHeight = 420f;

    void Start()
    {
        GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
        RectTransform rt = GetComponent<RectTransform>();

        float width = rt.rect.width;
        float cellWidth = (width - spacingX * (columnCount - 1)) / columnCount;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
        grid.spacing = new Vector2(spacingX, grid.spacing.y);
    }
}
