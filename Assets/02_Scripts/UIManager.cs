using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject mapPanel;

    private void Start()
    {
        menuPanel.SetActive(false);
        mapPanel.SetActive(false);
    }

    public void OpenMenu()
    {
        mapPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        menuPanel.SetActive(false);
    }

    public void OpenMap()
    {
        menuPanel.SetActive(false);
        mapPanel.SetActive(true);
    }

    public void CloseMap()
    {
        mapPanel.SetActive(false);
    }
}