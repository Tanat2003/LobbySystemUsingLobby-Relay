using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditPassword : MonoBehaviour
{
    public static EditPassword Instance { get; private set; }
    public event EventHandler OnPasswordChange;
    [SerializeField] private TextMeshProUGUI passwordText;

    private string password = "EnterPasswordHere";
    private void Awake()
    {
        Instance = this;

        GetComponent<Button>().onClick.AddListener(() =>
        {
            
            UI_InputWindow.Show_Static("Password", password, "abcdefghijklmnopqrstuvxywzABCDEFGHIJKLMNOPQRSTUVXYWZ., -123456789", 10,
            () =>
            {
                // Cancel
            },
                (string newpassword) =>
                {
                    password = newpassword;

                    passwordText.text = password;

                    OnPasswordChange?.Invoke(this, EventArgs.Empty);
                });
        });

        passwordText.text = password;
    }

    private void Start()
    {
        OnPasswordChange += EditPassword_OnPasswordChange;
        
    }

    private void EditPassword_OnPasswordChange(object sender, EventArgs e)
    {
       
        Debug.Log("?? UpdatePasswordLobby called with: " + password);
        LobbyManager.Instance.UpdatePasswordLobby(GetPasswordOfLobby());
    }
    public string GetPasswordOfLobby() => password;

}
