using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public Text progressText;
    public GameObject infoPanel;
    public Text infoText;
    public GameObject winText;

    public float infoHideDelay = 7f;
    float hideTimer;

    void Start()
    {
        if (infoPanel) infoPanel.SetActive(false);
        if (winText) winText.SetActive(false);
    }

    void Update()
    {
        if (infoPanel && infoPanel.activeSelf)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f) infoPanel.SetActive(false);
        }
    }

    public void ShowPlanetInfo(string info)
    {
        if (infoText) infoText.text = info;
        if (infoPanel) infoPanel.SetActive(true);
        hideTimer = infoHideDelay;
    }

    public void SetProgress(int count, int total)
    {
        if (progressText) progressText.text = "สำรวจแล้ว: " + count + "/" + total;
    }

    public void ShowWin()
    {
        if (progressText) progressText.text = "สำรวจครบทุกดวงแล้ว! ชนะ!";
        if (winText) winText.SetActive(true);
    }
}
