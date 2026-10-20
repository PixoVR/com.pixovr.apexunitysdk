using System.Collections.Generic;
using NUnit.Framework;
using PixoVR.Apex.Native;

namespace PixoVR.Apex.Tests
{
	/// <summary>The models built from the core's JSON payloads, and the launch argument parsers.</summary>
	public class ApexModelTests
	{
		[Test]
		public void UserIsReadFromTheCoreSchema()
		{
			const string kUserJson = @"{
				""id"": 512, ""orgId"": 7, ""firstName"": ""Ada"", ""lastName"": ""Lovelace"", ""username"": null,
				""email"": ""ada@example.test"", ""role"": ""superadmin"", ""token"": ""token-512"",
				""minimumPassingScore"": 70.0,
				""org"": { ""id"": 7, ""name"": ""Pixo"", ""status"": ""active"", ""downloadRegion"": ""na"",
					""type"": ""platform"", ""hubLogoLink"": null, ""primaryColor"": ""#000"",
					""secondaryColor"": null }
			}";

			LoginResponseContent user = LoginResponseContent.FromJson(JsonReading.ParseObject(kUserJson));

			Assert.That(user.ID, Is.EqualTo(512));
			Assert.That(user.OrgId, Is.EqualTo(7));
			Assert.That(user.First, Is.EqualTo("Ada"));
			Assert.That(user.Last, Is.EqualTo("Lovelace"));
			Assert.That(user.Username, Is.Null);
			Assert.That(user.Email, Is.EqualTo("ada@example.test"));
			Assert.That(user.Token, Is.EqualTo("token-512"));
			Assert.That(user.MinimumPassingScore, Is.EqualTo(70));
			Assert.That(user.Org.Name, Is.EqualTo("Pixo"));
			Assert.That(user.IsPlatformSuperadmin(), Is.True);
			Assert.That(user.HasErrored(), Is.False);
		}

		[Test]
		public void ModuleAccessReadsNullAndFractionalScores()
		{
			UserAccessResponseContent noScore = UserAccessResponseContent.FromJson(JsonReading.ParseObject(
				"{\"userId\": 512, \"moduleId\": 42, \"access\": true, \"passingScore\": null}"));
			UserAccessResponseContent fractionalScore = UserAccessResponseContent.FromJson(JsonReading.ParseObject(
				"{\"userId\": 512, \"moduleId\": 42, \"access\": false, \"passingScore\": 69.6}"));

			Assert.That(noScore.Access, Is.True);
			Assert.That(noScore.PassingScore, Is.Null);
			Assert.That(fractionalScore.Access, Is.False);
			Assert.That(fractionalScore.PassingScore, Is.EqualTo(70));
		}

		[Test]
		public void QuickIdUsersLoginCodeAndUserModulesAreRead()
		{
			QuickIDAuthGetUsersResponse quickIdUsers = QuickIDAuthGetUsersResponse.FromJson(JsonReading.ParseObject(
				"{\"org\": {\"name\": \"Pixo\", \"primaryColor\": \"#111\", \"secondaryColor\": \"#222\","
					+ " \"hubLogoUrl\": \"https://logo.test\"},"
					+ " \"users\": [{\"firstName\": \"Grace\", \"lastName\": \"Hopper\", \"username\": \"ghopper\","
					+ " \"email\": null}]}"));
			GeneratedAssistedLogin loginCode = GeneratedAssistedLogin.FromJson(JsonReading.ParseObject(
				"{\"code\": \"ABC123\", \"expiresAt\": \"2026-10-16T12:00:00Z\"}"));
			GetUserModulesResponse userModules = GetUserModulesResponse.FromJson(JsonReading.ParseObject(
				"{\"users\": [{\"userId\": 512, \"moduleIds\": [13, 42]}]}"));

			Assert.That(quickIdUsers.OrgProperties.OrgName, Is.EqualTo("Pixo"));
			Assert.That(quickIdUsers.OrgProperties.HubLogoURL, Is.EqualTo("https://logo.test"));
			Assert.That(quickIdUsers.QuickIDUsers, Has.Count.EqualTo(1));
			Assert.That(quickIdUsers.QuickIDUsers[0].Username, Is.EqualTo("ghopper"));
			Assert.That(loginCode.AssistedLogin.AuthCode, Is.EqualTo("ABC123"));
			Assert.That(userModules.ParsedData[0].UserId, Is.EqualTo(512));
			Assert.That(userModules.ParsedData[0].AvailableModules, Is.EqualTo(new[] { 13, 42 }));
		}

		[Test]
		public void ModulePageIsReadFromTheCoreSchema()
		{
			ModulePageResponse modulePage = ModulePageResponse.FromJson(@"{
				""modules"": [ { ""id"": 7, ""name"": ""Forklift"", ""versions"": [
					{ ""id"": 70, ""version"": ""1.2.0"", ""platforms"": [""android""],
						""lifecycle"": { ""id"": 2, ""name"": ""Development"", ""stage"": ""DEVELOPMENT"" } },
					{ ""id"": 71, ""version"": ""1.1.0"", ""platforms"": [""android""],
						""lifecycle"": { ""id"": null, ""name"": null, ""stage"": ""OTHER"" } } ] } ],
				""pageIndex"": 1, ""pageSize"": 1, ""pageCount"": 3, ""totalCount"": 3 }");

			Assert.That(modulePage.PageIndex, Is.EqualTo(1));
			Assert.That(modulePage.PageSize, Is.EqualTo(1));
			Assert.That(modulePage.PageCount, Is.EqualTo(3));
			Assert.That(modulePage.TotalCount, Is.EqualTo(3));
			Module module = modulePage.Modules[0];
			Assert.That(module.Id, Is.EqualTo(7));
			Assert.That(module.Versions[0].Lifecycle.Id, Is.EqualTo(2));
			Assert.That(module.Versions[0].Lifecycle.Name, Is.EqualTo("Development"));
			Assert.That(module.Versions[0].Lifecycle.Stage, Is.EqualTo(EModuleLifecycle.DEVELOPMENT));
			Assert.That(module.Versions[1].Lifecycle.Name, Is.Null);
			Assert.That(module.Versions[1].Lifecycle.Stage, Is.EqualTo(EModuleLifecycle.OTHER));
			Assert.That(ModulePageResponse.FromJson("null").Modules, Is.Empty);
		}

		[Test]
		public void LifecycleFlagsMatchTheCore()
		{
			Assert.That((int)EModuleLifecycle.RELEASED, Is.EqualTo(1));
			Assert.That((int)EModuleLifecycle.OTHER, Is.EqualTo(16));
			Assert.That((int)EModuleLifecycle.ALL, Is.EqualTo(31));
			Assert.That((int)EModuleSortOrder.NEWEST_VERSION, Is.EqualTo(3));
		}

		[Test]
		public void MalformedJsonReadsAsEmpty()
		{
			Assert.That(JsonReading.ParseObject("not json").Count, Is.EqualTo(0));
			Assert.That(JsonReading.ParseObject(null).Count, Is.EqualTo(0));
			Assert.That(LoginResponseContent.FromJson(JsonReading.ParseObject("[]")).HasErrored(), Is.True);
		}

		[Test]
		public void FailedResponseConvertsToFailureResponse()
		{
			ApexResponse response = new ApexResponse(EApexError.APEX_ERR_PLATFORM, 401, "Invalid credentials");
			FailureResponse failureResponse = response.ToFailureResponse();

			Assert.That(response.Succeeded, Is.False);
			Assert.That(failureResponse.HasErrored(), Is.True);
			Assert.That(failureResponse.HttpCode, Is.EqualTo("401"));
			Assert.That(failureResponse.Message, Is.EqualTo("Invalid credentials"));
		}

		[Test]
		public void LaunchUrlKeepsOptionalJsonWhole()
		{
			PixoGenericPlatformUtilities platformUtilities = new PixoGenericPlatformUtilities();
			Dictionary<string, string> launchArguments = platformUtilities.ParseURLArguments(
				"pixovr://launch?pixotoken=abc&optional={\"a\":1,\"b\":\"x&y\"}&returntarget=com.pixovr.hub");

			Assert.That(launchArguments["pixotoken"], Is.EqualTo("abc"));
			Assert.That(launchArguments["optional"], Is.EqualTo("{\"a\":1,\"b\":\"x&y\"}"));
			Assert.That(launchArguments["returntarget"], Is.EqualTo("com.pixovr.hub"));
		}

		[Test]
		public void CommandLineReadsNameValuePairs()
		{
			Dictionary<string, string> launchArguments = DesktopLaunching.ParseCommandLine(new[]
			{
				"Module.exe", "-pixotoken", "abc", "-returntarget", "C:/Hub/Hub.exe", "stray",
			});

			Assert.That(launchArguments["pixotoken"], Is.EqualTo("abc"));
			Assert.That(launchArguments["returntarget"], Is.EqualTo("C:/Hub/Hub.exe"));
			Assert.That(launchArguments.Count, Is.EqualTo(2));
		}
	}
}
