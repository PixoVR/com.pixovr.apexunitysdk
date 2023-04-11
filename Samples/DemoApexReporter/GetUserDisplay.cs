using UnityEngine;
using UnityEngine.UI;
using PixoVR.Apex;
public class GetUserDisplay : MonoBehaviour
{
    public Text FirstName;
    public Text LastName;
    public Text Email;
    public Text UserId;
    public Button GetUserButton;

    // Start is called before the first frame update
    void Start()
    {
        ApexSystem.Instance.OnGetUserSuccess.AddListener(OnGetUserSuccess);
        ApexSystem.Instance.OnGetUserFailed.AddListener(OnGetUserFailed);
        ApexSystem.Instance.OnLoginSuccess.AddListener(OnLoginSuccess);
        ApexSystem.Instance.OnLoginFailed.AddListener(OnLoginFailed);
        GetUserButton.interactable = false;
    }

    void OnLoginSuccess(LoginResponseContent loginResponse)
    {
        GetUserButton.interactable = true;
    }

    void OnLoginFailed(FailureResponse failedLoginResponse)
    {
        GetUserButton.interactable = false;
    }

    void OnGetUserSuccess(GetUserResponseContent getUserResponse)
    {
        FirstName.text = getUserResponse.First;
        LastName.text = getUserResponse.Last;
        Email.text = getUserResponse.Email;
        UserId.text = getUserResponse.ID.ToString();
    }

    void OnGetUserFailed(FailureResponse failedGetUserResponse)
    {
        FirstName.text = "N/A";
        LastName.text = "N/A";
        Email.text = "N/A";
        UserId.text = "N/A";
    }

    // Update is called once per frame
    void Update()
    {

    }
}
