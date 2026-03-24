using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginSelectPanel : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button guestButton;
    [SerializeField] private Button googleButton;

    [Header("상태 텍스트")]
    [SerializeField] private TextMeshProUGUI statusText;

    private void OnEnable()
    {
        guestButton.onClick.AddListener(OnGuestClicked);
        googleButton.onClick.AddListener(OnGoogleClicked);

    }

    public void OnDisable()
    {
        guestButton.onClick.RemoveListener(OnGuestClicked);
        googleButton.onClick.RemoveListener(OnGoogleClicked);
    }

    private async void OnGuestClicked()
    {
        SetButtonsInteractable(false);
        SetStatus("로그인 중...");

        PlayerPrefs.SetString("LoginMethod", "guest");
        PlayerPrefs.Save();

        await AuthManager.Instance.LoginAsGuest();

        gameObject.SetActive(false);
    }

    private void OnGoogleClicked()
    {
        SetButtonsInteractable(false);
        SetStatus("구글 로그인 중...");

        PlayerPrefs.SetString("LoginMethod", "google");
        PlayerPrefs.Save();

        AuthManager.Instance.LoginWithGoogle();
    }

    private void SetButtonsInteractable(bool value)
    {
        guestButton.interactable = value;
        googleButton.interactable = value;
    }

    private void SetStatus(string textStatus)
    {
        if (statusText != null)
        {
            statusText.text = textStatus;
        }
    }
}
