using UnityEngine;
using UnityEngine.UI;
using PixoVR.Apex;

public class RequestAuthPin : MonoBehaviour
{
    public Text AuthCodeText;

    // Start is called before the first frame update
    void Start()
    {
        ApexSystem.Instance.OnAuthorizationCodeReceived.AddListener(DisplayAuthCode);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RequestAuthorizationPin()
    {
        ApexSystem.RequestAuthorizationCode();
    }

    public void DisplayAuthCode(string authCode)
    {
        if(AuthCodeText)
        {
            AuthCodeText.text = authCode;
        }
    }
}
