using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RoomEventManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text storyText;
    [SerializeField] private Button[] choiceButtons = new Button[4];

    private ForestNode currentNode;

    private void Start()
    {
        HideAllChoices();
    }

    public void ShowRoomEvent(ForestNode node)
    {
        if (node == null)
            return;

        currentNode = node;
        HideAllChoices();

        switch (node.roomType)
        {
            case ForestRoomType.Start:
                ShowMessage("얕은 숲의 입구다.");
                break;

            case ForestRoomType.Monster:
                ShowMessage("마수가 나타났다.\n\n아직 전투 시스템은 구현되지 않았다.");
                SetChoice(0, "확인", ClearChoices);
                break;

            case ForestRoomType.Camp:
                ShowMessage("숲속에서 모닥불을 발견했다.\n\n잠시 쉬어갈 수 있을 것 같다.");
                SetChoice(0, "휴식한다", Rest);
                SetChoice(1, "그냥 지나간다", ClearChoices);
                break;

            case ForestRoomType.Treasure:
                ShowMessage("낡은 보물상자가 놓여 있다.");
                SetChoice(0, "상자를 연다", OpenTreasure);
                SetChoice(1, "그냥 지나간다", ClearChoices);
                break;

            case ForestRoomType.Event:
                ShowMessage("숲속에서 무언가 이상한 기척이 느껴진다.");
                SetChoice(0, "조사한다", InvestigateEvent);
                SetChoice(1, "그냥 지나간다", ClearChoices);
                break;

            case ForestRoomType.Exit:
                ShowMessage("더 깊은 숲으로 이어지는 길이 보인다.\n\n아직 다음 지역은 구현되지 않았다.");
                SetChoice(0, "확인", ClearChoices);
                break;
        }
    }

    private void OpenTreasure()
    {
        ShowMessage("상자를 열었다.\n\n10 골드를 획득했다.");
        ClearChoices();
    }

    private void Rest()
    {
        ShowMessage("모닥불 옆에서 잠시 휴식했다.\n\n아직 HP 회복 시스템은 구현되지 않았다.");
        ClearChoices();
    }

    private void InvestigateEvent()
    {
        ShowMessage("주변을 조사했다.\n\n아직 이벤트 내용은 구현되지 않았다.");
        ClearChoices();
    }

    private void ShowMessage(string message)
    {
        if (storyText != null)
            storyText.text = message;
    }

    private void SetChoice(int index, string label, UnityAction action)
    {
        if (choiceButtons == null || index < 0 || index >= choiceButtons.Length)
            return;

        Button button = choiceButtons[index];
        if (button == null)
            return;

        button.gameObject.SetActive(true);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);

        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);
        if (buttonText != null)
            buttonText.text = label;
    }

    public void ClearChoices()
    {
        HideAllChoices();
    }

    private void HideAllChoices()
    {
        if (choiceButtons == null)
            return;

        foreach (Button button in choiceButtons)
        {
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
            button.gameObject.SetActive(false);
        }
    }
}
