using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class UIManager : SingletonMonobehaviour<UIManager> {
    public GameObject startMenuGO;
    public TMP_InputField usernameInputField;



    public void ConnectToServer() {
        startMenuGO.SetActive(false);
        usernameInputField.interactable = false;

        Client.Instance.ConnectToServer();
    }
}
