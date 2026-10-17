using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace PixoVR.Apex.Tests
{
	/// <summary>Module lists as the core returns them (apex-sdk-core spec/schema/module_list.json).</summary>
	public class ModuleExtensionsTests
	{
		private const string kHappyPathJson = @"
{
	""modules"": [
		{
			""id"": 101,
			""name"": ""Module name"",
			""abbreviation"": ""MOD-101"",
			""externalId"": ""external-101"",
			""iconUrl"": ""https://example.test/module.png"",
			""distributor"": ""Example Developer"",
			""shortDescription"": ""Short description"",
			""longDescription"": ""Long description"",
			""industry"": ""healthcare"",
			""details"": ""Module details"",
			""categories"": ""Category A, Category B"",
			""isAvailable"": true,
			""isAuthenticatedLaunch"": true,
			""languages"": [""A"", ""B""],
			""player"": {
				""id"": 201,
				""name"": ""Example Player"",
				""launchProtocol"": ""example-player"",
				""versions"": [
					{ ""id"": 202, ""version"": ""2.0.0"", ""status"": ""Published"",
						""package"": ""com.example.player"", ""platforms"": [""android""] }
				]
			},
			""versions"": [
				{ ""id"": 301, ""version"": ""1.0.0"", ""package"": ""com.example.module"",
					""createdAt"": ""2026-01-01T00:00:00Z"", ""controls"": [""Control 1""],
					""platforms"": [""android"", ""windows""],
					""lifecycle"": { ""id"": 1, ""name"": ""Released"", ""stage"": ""RELEASED"" } },
				{ ""id"": 302, ""version"": ""2.0.0"", ""package"": null, ""createdAt"": null, ""controls"": [],
					""platforms"": [""webgl""],
					""lifecycle"": { ""id"": 8, ""name"": ""Beta"", ""stage"": ""OTHER"" } }
			]
		}
	]
}";

		private const string kNullFieldsJson = @"
{
	""modules"": [
		{
			""id"": -1,
			""name"": null,
			""abbreviation"": null,
			""externalId"": null,
			""iconUrl"": null,
			""distributor"": null,
			""shortDescription"": null,
			""longDescription"": null,
			""industry"": null,
			""details"": null,
			""categories"": null,
			""isAvailable"": false,
			""isAuthenticatedLaunch"": false,
			""languages"": [],
			""player"": null,
			""versions"": [ { ""id"": null, ""version"": null, ""package"": null, ""createdAt"": null, ""controls"": [],
				""platforms"": [""android""] } ]
		}
	]
}";

		[Test]
		public void ToOrgModuleMapsModuleAndNestedDownloadData()
		{
			Module module = UserModulesResponse.FromJson(kHappyPathJson).Modules.Single();
			Assert.That(module.IsAvailable, Is.True);

			OrgModule orgModule = module.ToOrgModule();
			Assert.That(orgModule.ID, Is.EqualTo(101));
			Assert.That(orgModule.Name, Is.EqualTo("Module name"));
			Assert.That(orgModule.Description, Is.EqualTo("Module name"));
			Assert.That(orgModule.ShortDescription, Is.EqualTo("Short description"));
			Assert.That(orgModule.LongDescription, Is.EqualTo("Long description"));
			Assert.That(orgModule.IconURL, Is.EqualTo("https://example.test/module.png"));
			Assert.That(orgModule.Distributor, Is.EqualTo("Example Developer"));
			Assert.That(orgModule.ExternalId, Is.EqualTo("external-101"));
			Assert.That(orgModule.Details, Is.EqualTo("Module details"));
			Assert.That(orgModule.Categories, Is.EqualTo("Category A, Category B"));
			Assert.That(orgModule.IsAuthenticatedLaunch, Is.True);
			Assert.That(orgModule.AvailableLanguages, Is.EqualTo("A, B"));
			Assert.That(orgModule.Industry, Is.EqualTo("Healthcare"));

			Assert.That(orgModule.Downloads, Has.Count.EqualTo(3));
			AssertDownload(orgModule, 301, "1.0.0", "com.example.module", "android");
			AssertDownload(orgModule, 301, "1.0.0", "com.example.module", "windows");
			AssertDownload(orgModule, 302, "2.0.0", null, "webgl");
			OrgModuleDownload releasedDownload = orgModule.Downloads.First(download => download.VersionID == 301);
			Assert.That(releasedDownload.Status, Is.EqualTo("Released"));
			Assert.That(releasedDownload.Lifecycle, Is.EqualTo(EModuleLifecycle.RELEASED));
			OrgModuleDownload customDownload = orgModule.Downloads.First(download => download.VersionID == 302);
			Assert.That(customDownload.Status, Is.EqualTo("Beta"));
			Assert.That(customDownload.Lifecycle, Is.EqualTo(EModuleLifecycle.OTHER));

			Assert.That(orgModule.Player, Is.Not.Null);
			Assert.That(orgModule.Player.Id, Is.EqualTo(201));
			Assert.That(orgModule.Player.Name, Is.EqualTo("Example Player"));
			Assert.That(orgModule.Player.LaunchProtocol, Is.EqualTo("example-player"));
			Assert.That(orgModule.Player.Versions, Has.Count.EqualTo(1));
			Assert.That(orgModule.Player.Versions[0].Id, Is.EqualTo(202));
			Assert.That(orgModule.Player.Versions[0].Version, Is.EqualTo("2.0.0"));
			Assert.That(orgModule.Player.Versions[0].Status, Is.EqualTo("Published"));
			Assert.That(orgModule.Player.Versions[0].ApkName, Is.EqualTo("com.example.player"));
			Assert.That(orgModule.Player.Versions[0].Platform, Is.EqualTo("android"));
		}

		[Test]
		public void NullFieldsUseSafeDefaults()
		{
			UserModulesResponse modulesResponse = null;
			Assert.DoesNotThrow(() => modulesResponse = UserModulesResponse.FromJson(kNullFieldsJson));

			OrgModule orgModule = null;
			Assert.DoesNotThrow(() => orgModule = modulesResponse.Modules.Single().ToOrgModule());
			Assert.That(orgModule.ID, Is.EqualTo(-1));
			Assert.That(orgModule.Name, Is.Empty);
			Assert.That(orgModule.IsAuthenticatedLaunch, Is.False);
			Assert.That(orgModule.Player, Is.Null);
			Assert.That(orgModule.AvailableLanguages, Is.Empty);
			Assert.That(orgModule.Downloads, Has.Count.EqualTo(1));
			Assert.That(orgModule.Downloads[0].VersionID, Is.EqualTo(-1));
		}

		[Test]
		public void EmptyOrMissingModuleListIsEmpty()
		{
			Assert.That(UserModulesResponse.FromJson("{}").Modules, Is.Empty);
			Assert.That(UserModulesResponse.FromJson("{\"modules\": null}").Modules, Is.Empty);
		}

		[Test]
		public void ToOrgModulesMapsListsOneToOne()
		{
			Assert.That(((List<Module>)null).ToOrgModules(), Is.Empty);

			List<Module> modules = new List<Module>
			{
				UserModulesResponse.FromJson(kHappyPathJson).Modules[0],
				new Module(),
			};
			List<OrgModule> orgModules = modules.ToOrgModules();

			Assert.That(orgModules, Has.Count.EqualTo(2));
			Assert.That(orgModules[0].ID, Is.EqualTo(101));
			Assert.That(orgModules[1].ID, Is.EqualTo(-1));
		}

		[Test]
		public void ToOrgModuleOfNullIsNull()
		{
			Assert.That(((Module)null).ToOrgModule(), Is.Null);
		}

		private static void AssertDownload(OrgModule orgModule, int versionId, string version, string packageName,
			string platformName)
		{
			OrgModuleDownload download = orgModule.Downloads.Single(candidate =>
				candidate.VersionID == versionId && candidate.Platform == platformName);
			Assert.That(download.Version, Is.EqualTo(version));
			Assert.That(download.ApkName, Is.EqualTo(packageName));
			Assert.That(download.ID, Is.EqualTo(orgModule.ID));
		}
	}
}
