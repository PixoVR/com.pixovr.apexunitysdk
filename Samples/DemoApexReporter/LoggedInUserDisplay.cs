using UnityEngine;
using UnityEngine.UI;
using PixoVR.Apex;

public class LoggedInUserDisplay : MonoBehaviour
{
    public Text FirstName;
    public Text LastName;
    public Text Email;
    public Text UserId;
    public Text OrgName;
    public Text OrgId;
    public Text MinimumPassingScore;

    // Start is called before the first frame update
    void Start()
    {
        ApexSystem.Instance.OnLoginSuccess.AddListener(OnLoginSuccess);
        ApexSystem.Instance.OnLoginFailed.AddListener(OnLoginFailed);
    }

    void OnLoginFailed(FailureResponse response)
    {
        FirstName.text = "N/A";
        LastName.text = "N/A";
        Email.text = "N/A";
        UserId.text = "N/A";
        OrgName.text = "N/A";
        OrgId.text = "N/A";
        MinimumPassingScore.text = "N/A";
    }

    void OnLoginSuccess(LoginResponseContent loginResponse)
    {
        FirstName.text = loginResponse.First;
        LastName.text = loginResponse.Last;
        Email.text = loginResponse.Email;
        UserId.text = loginResponse.ID.ToString();
        OrgName.text = loginResponse.Org.Name;
        OrgId.text = loginResponse.Org.ID.ToString();
        MinimumPassingScore.text = loginResponse.MinimumPassingScore.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
