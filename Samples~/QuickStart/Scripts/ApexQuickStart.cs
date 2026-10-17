using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using PixoVR.Apex.Analytics;
using UnityEngine;

namespace PixoVR.Apex.Samples
{
	/// <summary>
	/// A panel that drives every ApexSystem call, for trying the SDK against a real environment.
	/// Put it on a GameObject next to an ApexSystem (the QuickStart scene has both), press Play,
	/// and use the buttons; each result is logged in the panel and the console.
	/// </summary>
	public class ApexQuickStart : MonoBehaviour
	{
		private const int kMaximumLogLines = 14;
		private const float kPanelWidth = 460f;

		[SerializeField, Tooltip("Environment to use on start. Stage is the safest place to try things.")]
		private EPlatformServer m_platformServer = EPlatformServer.NA_STAGE;

		[SerializeField, Tooltip("0 keeps the module ID from Project Settings > PixoVR.")]
		private int m_moduleId = 13;

		[SerializeField]
		private string m_moduleVersion = "1.0.0";

		[SerializeField]
		private string m_username = string.Empty;

		[SerializeField]
		private string m_scenarioId = "QuickStart";

		[SerializeField, Tooltip("Platform name used to filter the module list, e.g. android or windows.")]
		private string m_modulePlatform = "android";

		[SerializeField, Tooltip("Modules per page for the paged module list.")]
		private int m_modulePageSize = 5;

		private static readonly string[] kSortOrderLabels = { "Platform", "Name", "ID", "Newest" };

		private readonly List<string> m_logLines = new List<string>();
		private int m_modulePageIndex;
		private int m_modulePageCount;
		private bool m_includeUnreleasedModules;
		private EModuleSortOrder m_moduleSortOrder = EModuleSortOrder.NAME;
		private string m_password = string.Empty;
		private string m_serialNumber = string.Empty;
		private string m_scoreText = "80";
		private Vector2 m_scrollPosition;
		private List<QuickIDUser> m_quickIdUsers = new List<QuickIDUser>();
		private ApexInteractable m_demoInteractable;
		private int m_eventCount;
		private float m_sessionStartTime;

		private void Start()
		{
			ApexSystem.ModuleVersion = m_moduleVersion;
			if (m_moduleId > 0)
			{
				ApexSystem.ModuleID = m_moduleId;
			}

			ApexSystem.ChangePlatformServer(m_platformServer);
			CreateDemoInteractable();
			AddLog($"Core {ApexSystem.CoreVersion ?? "not loaded"}, {ApexSystem.HeadsetAPIEndpoint}, module "
				+ ApexSystem.ModuleID);
			if (!ApexSystem.IsCoreAvailable)
			{
				AddLog("The core is not running: " + ApexSystem.CoreErrorMessage);
			}

			if (!string.IsNullOrEmpty(ApexSystem.PassedLoginToken))
			{
				AddLog("The launcher passed a login token; use Log in with token.");
			}
		}

		private void OnGUI()
		{
			GUILayout.BeginArea(new Rect(10f, 10f, kPanelWidth, Screen.height - 20f), GUI.skin.box);
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition);
			DrawStatus();
			DrawLogin();
			DrawQuickId();
			DrawModules();
			DrawSession();
			DrawAnalytics();
			DrawLog();
			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		// ---------------------------------------------------------------- Sections

		private void DrawStatus()
		{
			GUILayout.Label("<b>Apex Quick Start</b>", RichLabel());
			GUILayout.Label("Environment: " + ApexSystem.PlatformTargetServer.ToDisplayString());
			GUILayout.BeginHorizontal();
			foreach (EPlatformServer platformServer in new[] { EPlatformServer.NA_STAGE, EPlatformServer.NA_DEV,
				EPlatformServer.NA_PRODUCTION })
			{
				if (GUILayout.Button(platformServer.ToString()))
				{
					ApexSystem.ChangePlatformServer(platformServer);
					m_quickIdUsers.Clear();
					AddLog("Switched to " + platformServer.ToDisplayString() + "; logged out.");
				}
			}

			GUILayout.EndHorizontal();
			LoginResponseContent currentUser = ApexSystem.CurrentUser;
			string loginState = currentUser == null
				? "Not logged in"
				: $"{currentUser.First} {currentUser.Last} (user {currentUser.ID}), access: {ApexSystem.UserHasAccess}";
			GUILayout.Label(loginState);
			GUILayout.Label(ApexSystem.IsSessionInProgress
				? "Session " + ApexSystem.SessionId + " in progress"
				: "No session");
		}

		private void DrawLogin()
		{
			GUILayout.Space(8f);
			GUILayout.Label("<b>Login</b>", RichLabel());
			m_username = LabeledField("Username", m_username);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Password", GUILayout.Width(110f));
			m_password = GUILayout.PasswordField(m_password, '*');
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Log in"))
			{
				ApexSystem.Login(m_username, m_password, OnLoginSucceeded, LogFailure("Login"));
			}

			if (GUILayout.Button("Log in with token"))
			{
				ApexSystem.LoginWithToken(OnLoginSucceeded, LogFailure("Token login"));
			}

			if (GUILayout.Button("Log out"))
			{
				ApexSystem.Logout();
				AddLog("Logged out.");
			}

			GUILayout.EndHorizontal();
			if (GUILayout.Button("Ping"))
			{
				ApexSystem.Ping((response, payload) => AddLog("Ping OK"), LogFailure("Ping"));
			}
		}

		private void DrawQuickId()
		{
			GUILayout.Space(8f);
			GUILayout.Label("<b>Quick ID</b>", RichLabel());
			m_serialNumber = LabeledField("Serial number", m_serialNumber);
			if (GUILayout.Button("Get Quick ID users"))
			{
				ApexSystem.GetQuickIDAuthUsers(m_serialNumber, (response, quickIdResponse) =>
				{
					m_quickIdUsers = quickIdResponse.QuickIDUsers;
					AddLog($"{m_quickIdUsers.Count} Quick ID user(s) in {quickIdResponse.OrgProperties.OrgName}");
				}, LogFailure("Quick ID users"));
			}

			foreach (QuickIDUser quickIdUser in m_quickIdUsers)
			{
				string userLabel = $"{quickIdUser.FirstName} {quickIdUser.LastName} ({quickIdUser.Username})";
				if (GUILayout.Button("Log in as " + userLabel))
				{
					ApexSystem.QuickIDLogin(m_serialNumber, quickIdUser.Username, OnLoginSucceeded,
						LogFailure("Quick ID login"));
				}
			}
		}

		private void DrawModules()
		{
			GUILayout.Space(8f);
			GUILayout.Label("<b>Modules</b>", RichLabel());
			m_modulePlatform = LabeledField("Platform", m_modulePlatform);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Check access"))
			{
				ApexSystem.CheckModuleAccess(-1, (response, userInformation) =>
					AddLog($"Access to module {userInformation.ModuleUserInformation.ModuleId}: "
						+ userInformation.ModuleUserInformation.Access), LogFailure("Module access"));
			}

			if (GUILayout.Button("Module list"))
			{
				ApexSystem.GetModulesList(m_modulePlatform, OnModuleList, LogFailure("Module list"));
			}

			if (GUILayout.Button("Login code"))
			{
				ApexSystem.GenerateOneTimeLoginForCurrentUser((response, payload) =>
				{
					GeneratedAssistedLogin loginCode = (GeneratedAssistedLogin)payload;
					AssistedLoginCode assistedLogin = loginCode.AssistedLogin;
					AddLog($"Login code {assistedLogin.AuthCode}, expires {assistedLogin.Expires}");
				}, LogFailure("Login code"));
			}

			GUILayout.EndHorizontal();
			DrawModulePages();
		}

		private void DrawModulePages()
		{
			GUILayout.BeginHorizontal();
			m_includeUnreleasedModules = GUILayout.Toggle(m_includeUnreleasedModules, "Include unreleased");
			m_moduleSortOrder = (EModuleSortOrder)GUILayout.Toolbar((int)m_moduleSortOrder, kSortOrderLabels);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("First page (refresh)"))
			{
				RequestModulePage(0, true);
			}

			if (GUILayout.Button("Previous page") && m_modulePageIndex > 0)
			{
				RequestModulePage(m_modulePageIndex - 1, false);
			}

			if (GUILayout.Button("Next page") && m_modulePageIndex + 1 < m_modulePageCount)
			{
				RequestModulePage(m_modulePageIndex + 1, false);
			}

			GUILayout.EndHorizontal();
		}

		private void RequestModulePage(int pageIndex, bool refresh)
		{
			EModuleLifecycle lifecycles = m_includeUnreleasedModules ? EModuleLifecycle.ALL : EModuleLifecycle.RELEASED;
			ApexSystem.GetModulesPage(m_modulePlatform, pageIndex, Mathf.Max(1, m_modulePageSize), lifecycles,
				m_moduleSortOrder, refresh, OnModulePage, LogFailure("Module page"));
		}

		private void OnModulePage(ApexResponse response, ModulePageResponse pageResponse)
		{
			m_modulePageIndex = pageResponse.PageIndex;
			m_modulePageCount = pageResponse.PageCount;
			string sourceText = response.HttpStatus == 0 ? "kept list" : "fetched";
			AddLog($"Page {pageResponse.PageIndex + 1} of {pageResponse.PageCount}, {pageResponse.TotalCount} "
				+ $"module(s) in all ({sourceText})");
			foreach (Module module in pageResponse.Modules)
			{
				List<string> versionLabels = new List<string>();
				foreach (ModuleVersion moduleVersion in module.Versions)
				{
					versionLabels.Add($"{moduleVersion.Version} {moduleVersion.Lifecycle?.Name ?? "no lifecycle"}");
				}

				AddLog($"  {module.Id}: {module.Name} [{string.Join(", ", versionLabels)}]");
			}
		}

		private void DrawSession()
		{
			GUILayout.Space(8f);
			GUILayout.Label("<b>Session</b>", RichLabel());
			m_scenarioId = LabeledField("Scenario", m_scenarioId);
			m_scoreText = LabeledField("Score (0-100)", m_scoreText);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Join"))
			{
				JObject sessionData = new JObject();
				sessionData["startedFrom"] = "QuickStart";
				ApexSystem.JoinSession(m_scenarioId, sessionData, (response, joinResponse) =>
				{
					m_sessionStartTime = Time.time;
					AddLog("Joined session " + joinResponse.SessionId);
				}, LogFailure("Join"));
			}

			if (GUILayout.Button("Send event"))
			{
				m_eventCount++;
				JObject eventData = new JObject();
				eventData["count"] = m_eventCount;
				ApexSystem.SendSessionEvent("button_pressed", "QuickStartPanel", eventData,
					(response, payload) => AddLog("Event " + m_eventCount + " sent"), LogFailure("Event"));
			}

			if (GUILayout.Button("Complete"))
			{
				float.TryParse(m_scoreText, out float score);
				int durationSeconds = Mathf.RoundToInt(Time.time - m_sessionStartTime);
				SessionData sessionResult = new SessionData(score, 0f, 0f, 100f, durationSeconds, true, score >= 70f);
				ApexSystem.CompleteSession(sessionResult, null,
					(response, payload) => AddLog($"Session complete, score {score}"), LogFailure("Complete"));
			}

			GUILayout.EndHorizontal();
			if (GUILayout.Button("Exit to launcher"))
			{
				ApexSystem.ExitApplication();
			}
		}

		private void DrawAnalytics()
		{
			GUILayout.Space(8f);
			GUILayout.Label("<b>Analytics (telemetry)</b>", RichLabel());
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Begin step"))
			{
				ApexSteps.BeginStep("quickstart_step");
				AddLog("Step begun");
			}

			if (GUILayout.Button("Complete step"))
			{
				ApexSteps.CompleteStep("quickstart_step", 1f);
				AddLog("Step completed");
			}

			GUILayout.EndHorizontal();
			if (m_demoInteractable == null)
			{
				return;
			}

			GUILayout.BeginHorizontal();
			if (GUILayout.Button(m_demoInteractable.IsGrabbed ? "Release cube" : "Grab cube"))
			{
				if (m_demoInteractable.IsGrabbed)
				{
					m_demoInteractable.Release();
				}
				else
				{
					m_demoInteractable.Grab();
				}
			}

			if (GUILayout.Button("Use cube"))
			{
				m_demoInteractable.Use();
			}

			GUILayout.EndHorizontal();
		}

		private void DrawLog()
		{
			GUILayout.Space(8f);
			GUILayout.Label("<b>Results</b>", RichLabel());
			StringBuilder logText = new StringBuilder();
			foreach (string logLine in m_logLines)
			{
				logText.AppendLine(logLine);
			}

			GUILayout.TextArea(logText.ToString(), GUILayout.MinHeight(180f));
		}

		// ---------------------------------------------------------------- Results

		private void OnLoginSucceeded(ApexResponse response, ActiveUserInformation userInformation)
		{
			LoginResponseContent user = userInformation.User;
			string accessText = userInformation.ModuleUserInformation == null
				? "not checked"
				: userInformation.ModuleUserInformation.Access.ToString();
			AddLog($"Logged in as {user.First} {user.Last} ({user.Email}); module access: {accessText}");
		}

		private void OnModuleList(ApexResponse response, object payload)
		{
			UserModulesResponse modulesResponse = (UserModulesResponse)payload;
			List<OrgModule> orgModules = modulesResponse.Modules.ToOrgModules();
			AddLog($"{orgModules.Count} module(s) for {m_modulePlatform}");
			foreach (OrgModule orgModule in orgModules)
			{
				AddLog($"  {orgModule.ID}: {orgModule.Name} ({orgModule.Downloads.Count} download(s))");
			}
		}

		private Action<ApexResponse, FailureResponse> LogFailure(string callName)
		{
			return (response, failureResponse) => AddLog($"{callName} failed: {response}");
		}

		private void AddLog(string logLine)
		{
			string timestampedLine = DateTime.Now.ToString("HH:mm:ss") + "  " + logLine;
			Debug.Log("[ApexQuickStart] " + logLine);
			m_logLines.Add(timestampedLine);
			while (m_logLines.Count > kMaximumLogLines)
			{
				m_logLines.RemoveAt(0);
			}
		}

		// ---------------------------------------------------------------- Helpers

		/// <summary>A cube with an ApexInteractable, so grab and use show up in telemetry.</summary>
		private void CreateDemoInteractable()
		{
			GameObject demoCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			demoCube.name = "QuickStart Cube";
			demoCube.transform.position = new Vector3(0f, 0f, 3f);
			m_demoInteractable = demoCube.AddComponent<ApexInteractable>();
		}

		private static string LabeledField(string fieldLabel, string fieldValue)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(fieldLabel, GUILayout.Width(110f));
			string editedValue = GUILayout.TextField(fieldValue ?? string.Empty);
			GUILayout.EndHorizontal();
			return editedValue;
		}

		private static GUIStyle RichLabel()
		{
			GUIStyle richLabelStyle = new GUIStyle(GUI.skin.label);
			richLabelStyle.richText = true;
			return richLabelStyle;
		}
	}
}
