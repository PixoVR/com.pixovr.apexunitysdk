using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PixoVR.Apex;

public class LoginButtonScript : MonoBehaviour
{
    public InputField UsernameField;
    public InputField PasswordField;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoginClicked()
    {
        ApexSystem.Login(UsernameField.text, PasswordField.text);
    }

    public void GetUserClicked()
    {
        ApexSystem.GetCurrentUser();
    }
}
