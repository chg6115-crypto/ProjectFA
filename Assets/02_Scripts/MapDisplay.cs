using UnityEngine;
using UnityEngine.UI;

public class MapDisplay : MonoBehaviour
{
    public GameObject mapCellPrefab;
    public Transform mapPanel;

    private GameObject[,] cells = new GameObject[5, 5];
    private bool[,] path = new bool[5, 5];

    private void Start()
    {
        CreateGrid();
        GeneratePath();
        ShowPath();
    }

    private void CreateGrid()
    {
        for (int lane = 4; lane >= 0; lane--)
        {
            for (int depth = 0; depth < 5; depth++)
            {
                GameObject cell = Instantiate(mapCellPrefab, mapPanel);
                cells[depth, lane] = cell;
            }
        }
    }

    private void GeneratePath()
    {
        int lane = Random.Range(0, 5);

        path[0, lane] = true;

        for (int depth = 0; depth < 4; depth++)
        {
            int nextLane = Random.Range(0, 5);

            while (lane != nextLane)
            {
                lane += nextLane > lane ? 1 : -1;
                path[depth, lane] = true;
            }

            path[depth + 1, lane] = true;
        }
    }

    private void ShowPath()
    {
        for (int depth = 0; depth < 5; depth++)
        {
            for (int lane = 0; lane < 5; lane++)
            {
                Image image = cells[depth, lane].GetComponent<Image>();

                image.enabled = path[depth, lane];
            }
        }
    }
}