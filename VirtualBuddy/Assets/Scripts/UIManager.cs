using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject StartPanel;
    public GameObject LoginPanel;
    public GameObject SetNamePanel;
    public GameObject BuddyChoicePanel;
    public GameObject PersonalityChoicePanel;

    public void ShowPanel(string panelName)
    {
        StartPanel.SetActive(false);
        LoginPanel.SetActive(false);
        SetNamePanel.SetActive(false);
        BuddyChoicePanel.SetActive(false);
        PersonalityChoicePanel.SetActive(false);

        switch (panelName)
        {
            case "Start":
                StartPanel.SetActive(true); break;
            case "Login":
                LoginPanel.SetActive(true); break;
            case "SetName":
                SetNamePanel.SetActive(true); break;
            case "BuddyChoice":
                BuddyChoicePanel.SetActive(true); break;
            case "PersonalityChoice":
                PersonalityChoicePanel.SetActive(true); break;
        }
    }

    void Start()
    {
        ShowPanel("Start"); // ƒ¨»œΩ¯»ÎStart“≥
    }
}
