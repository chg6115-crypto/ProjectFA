using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ForestExplorer : MonoBehaviour
{
    public Image backgroundImage;
    public TMP_Text storyText;
    public Sprite entranceBackground;
    public Sprite forestBackground;
    public MapDisplay mapDisplay;

    private ForestNode currentNode;

    public ForestNode CurrentNode => currentNode;

    private void Start()
    {
        if (mapDisplay == null) mapDisplay = GetComponent<MapDisplay>();
        if (mapDisplay == null) mapDisplay = FindFirstObjectByType<MapDisplay>();

        if (mapDisplay == null)
        {
            Debug.LogError("ForestExplorer: Map Display를 찾을 수 없습니다.", this);
            enabled = false;
            return;
        }

        mapDisplay.InitializeMap();
        mapDisplay.SetExplorer(this);

        currentNode = mapDisplay.EntranceConnection;

        if (currentNode == null)
        {
            Debug.LogError("ForestExplorer: StartPoint를 찾을 수 없습니다.", this);
            enabled = false;
            return;
        }

        EnterNode(currentNode);
    }

    public void MoveToNode(ForestNode target)
    {
        if (currentNode == null || target == null)
            return;

        // 현재 방과 실제로 연결된 방만 클릭 이동할 수 있습니다.
        if (!currentNode.connectedNodes.Contains(target))
            return;

        EnterNode(target);
    }

    private void EnterNode(ForestNode target)
    {
        currentNode = target;
        currentNode.isVisited = true;

        DiscoverCurrentArea();
        ShowCurrentNode();
    }

    private void DiscoverCurrentArea()
    {
        currentNode.isDiscovered = true;

        // 현재 방에 도착하면 연결된 다음 후보들만 ?로 공개합니다.
        foreach (ForestNode neighbor in currentNode.connectedNodes)
            neighbor.isDiscovered = true;
    }

    private void ShowCurrentNode()
    {
        if (storyText != null)
            storyText.text = currentNode.nodeName + "\n\n" + currentNode.description;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = currentNode.background != null
                ? currentNode.background
                : forestBackground;
        }

        mapDisplay.ShowMap(currentNode);
    }
}
