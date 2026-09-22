using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ForestExplorer : MonoBehaviour
{
    public Image backgroundImage;
    public TMP_Text storyText;
    public Button[] choiceButtons;

    public Sprite entranceBackground;
    public Sprite forestBackground;

    private ForestNode currentNode;

    private void Start()
    {
        CreateTestMap();
        ShowCurrentNode();
    }

    private void CreateTestMap()
    {
        ForestNode entrance = CreateNode(
            "숲 입구",
            "마수의 숲 입구에 도착했다.",
            entranceBackground);

        ForestNode shallowForest = CreateNode(
            "얕은 숲",
            "숲 안쪽에서 세 갈래 길을 발견했다.",
            forestBackground);

        ForestNode oldPath = CreateNode(
            "오래된 숲길",
            "오래된 흔적이 남아 있는 좁은 숲길이다.",
            forestBackground);

        ForestNode stream = CreateNode(
            "시냇가",
            "맑은 물이 흐르는 작은 시냇가에 도착했다.",
            forestBackground);

        ForestNode rockyHill = CreateNode(
            "바위 언덕",
            "커다란 바위가 드문드문 솟아 있는 언덕이다.",
            forestBackground);

        entrance.connectedNodes.Add(shallowForest);

        shallowForest.connectedNodes.Add(oldPath);
        shallowForest.connectedNodes.Add(stream);
        shallowForest.connectedNodes.Add(rockyHill);

        currentNode = entrance;
    }

    private ForestNode CreateNode(string name, string description, Sprite background)
    {
        ForestNode node = new ForestNode();
        node.nodeName = name;
        node.description = description;
        node.background = background;

        return node;
    }

    private void ShowCurrentNode()
    {
        backgroundImage.sprite = currentNode.background;
        storyText.text = currentNode.description;

        UpdateChoiceButtons();
    }

    private void UpdateChoiceButtons()
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].onClick.RemoveAllListeners();

            if (i < currentNode.connectedNodes.Count)
            {
                ForestNode nextNode = currentNode.connectedNodes[i];

                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].GetComponentInChildren<TMP_Text>().text =
                    nextNode.nodeName + "(으)로 이동";

                choiceButtons[i].onClick.AddListener(() => MoveToNode(nextNode));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void MoveToNode(ForestNode nextNode)
    {
        currentNode = nextNode;
        ShowCurrentNode();
    }
}