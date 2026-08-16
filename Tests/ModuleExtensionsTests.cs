using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace PixoVR.Apex.Tests
{
    public class ModuleExtensionsTests
    {
        private const string HappyPathJson = @"
{
  ""modules"": [
    {
      ""id"": ""101"",
      ""abbreviation"": ""MOD-101"",
      ""externalId"": ""external-101"",
      ""imageLink"": ""https://example.test/module.png"",
      ""developer"": ""Example Developer"",
      ""description"": ""Module description"",
      ""shortDesc"": ""Short description"",
      ""longDesc"": ""Long description"",
      ""industry"": ""healthcare"",
      ""details"": ""Module details"",
      ""categories"": ""Category A, Category B"",
      ""isAvailable"": true,
      ""isAuthenticatedLaunch"": true,
      ""availableLanguages"": [
        { ""displayName"": ""A"" },
        { ""displayName"": ""B"" }
      ],
      ""modulePlayer"": {
        ""id"": ""201"",
        ""name"": ""Example Player"",
        ""launchProtocol"": ""example-player"",
        ""versions"": [
          {
            ""id"": ""202"",
            ""version"": ""2.0.0"",
            ""status"": ""Published"",
            ""fileLink"": ""https://example.test/player.apk"",
            ""fileSize"": 42,
            ""platforms"": [
              { ""id"": ""1"", ""name"": ""Android"", ""shortName"": ""android"" }
            ]
          }
        ]
      },
      ""versions"": [
        {
          ""id"": ""301"",
          ""version"": ""1.0.0"",
          ""createdAt"": ""2026-01-01T00:00:00Z"",
          ""fileLink"": ""https://example.test/module-1.apk"",
          ""fileSize"": 100,
          ""controls"": [
            { ""id"": ""401"", ""name"": ""Control 1"" }
          ],
          ""platforms"": [
            { ""id"": ""1"", ""name"": ""Android"", ""shortName"": ""android"" },
            { ""id"": ""2"", ""name"": ""Windows"", ""shortName"": ""windows"" }
          ]
        },
        {
          ""id"": ""302"",
          ""version"": ""2.0.0"",
          ""createdAt"": ""2026-01-02T00:00:00Z"",
          ""fileLink"": ""https://example.test/module-2.apk"",
          ""fileSize"": 200,
          ""controls"": [
            { ""id"": ""402"", ""name"": ""Control 2"" }
          ],
          ""platforms"": [
            { ""id"": ""3"", ""name"": ""WebGL"", ""shortName"": ""webgl"" }
          ]
        }
      ]
    }
  ]
}";

        private const string NullableFieldsJson = @"
{
  ""modules"": [
    {
      ""id"": ""501"",
      ""description"": ""Nullable fields"",
      ""isAvailable"": null,
      ""isAuthenticatedLaunch"": null,
      ""availableLanguages"": [],
      ""versions"": [
        {
          ""id"": ""601"",
          ""version"": ""1.0.0"",
          ""fileLink"": ""https://example.test/nullable.apk"",
          ""fileSize"": null,
          ""platforms"": [
            { ""id"": ""1"", ""name"": ""Android"", ""shortName"": ""android"" }
          ]
        }
      ]
    }
  ]
}";

        private const string MissingAndInvalidFieldsJson = @"
{
  ""modules"": [
    {
      ""description"": ""Missing module id"",
      ""versions"": [
        {
          ""id"": ""not-a-number"",
          ""platforms"": null
        }
      ]
    },
    {
      ""id"": ""also-not-a-number"",
      ""modulePlayer"": null,
      ""versions"": null,
      ""availableLanguages"": null
    }
  ]
}";

        [Test]
        public void ToOrgModule_MapsModuleAndNestedDownloadData()
        {
            UserModulesResponse response = Deserialize(HappyPathJson);
            Module module = response.modules.Single();

            Assert.That(module.IsAvailable, Is.True);

            OrgModule orgModule = module.ToOrgModule();

            Assert.That(orgModule.ID, Is.EqualTo(101));
            Assert.That(orgModule.Name, Is.EqualTo("Module description"));
            Assert.That(orgModule.Description, Is.EqualTo("Module description"));
            Assert.That(orgModule.ShortDescription, Is.EqualTo("Short description"));
            Assert.That(orgModule.LongDescription, Is.EqualTo("Long description"));
            Assert.That(orgModule.IconURL, Is.EqualTo("https://example.test/module.png"));
            Assert.That(orgModule.Distributor, Is.EqualTo("Example Developer"));
            Assert.That(orgModule.externalId, Is.EqualTo("external-101"));
            Assert.That(orgModule.Details, Is.EqualTo("Module details"));
            Assert.That(orgModule.Categories, Is.EqualTo("Category A, Category B"));
            Assert.That(orgModule.IsAuthenticatedLaunch, Is.True);
            Assert.That(orgModule.AvailableLanguages, Is.EqualTo("A, B"));
            Assert.That(orgModule.Industry, Is.EqualTo("Healthcare"));

            Assert.That(orgModule.Downloads, Has.Count.EqualTo(3));
            AssertDownload(orgModule, 301, "1.0.0", 100, "https://example.test/module-1.apk", "android");
            AssertDownload(orgModule, 301, "1.0.0", 100, "https://example.test/module-1.apk", "windows");
            AssertDownload(orgModule, 302, "2.0.0", 200, "https://example.test/module-2.apk", "webgl");

            Assert.That(orgModule.player, Is.Not.Null);
            Assert.That(orgModule.player.id, Is.EqualTo(201));
            Assert.That(orgModule.player.name, Is.EqualTo("Example Player"));
            Assert.That(orgModule.player.launchProtocol, Is.EqualTo("example-player"));
            Assert.That(orgModule.player.versions, Has.Count.EqualTo(1));
            Assert.That(orgModule.player.versions[0].id, Is.EqualTo(202));
            Assert.That(orgModule.player.versions[0].version, Is.EqualTo("2.0.0"));
            Assert.That(orgModule.player.versions[0].status, Is.EqualTo("Published"));
            Assert.That(orgModule.player.versions[0].URL, Is.EqualTo("https://example.test/player.apk"));
            Assert.That(orgModule.player.versions[0].platform, Is.EqualTo("android"));
        }

        [Test]
        public void DeserializeAndConvert_NullableFieldsUseSafeDefaults()
        {
            UserModulesResponse response = null;

            Assert.DoesNotThrow(() => response = Deserialize(NullableFieldsJson));

            Module module = response.modules.Single();
            Assert.That(module.isAvailable, Is.Null);
            Assert.That(module.IsAvailable, Is.False);
            Assert.That(module.isAuthenticatedLaunch, Is.Null);

            OrgModule orgModule = null;
            Assert.DoesNotThrow(() => orgModule = module.ToOrgModule());

            Assert.That(orgModule.IsAuthenticatedLaunch, Is.False);
            Assert.That(orgModule.Downloads, Has.Count.EqualTo(1));
            Assert.That(orgModule.Downloads[0].DownloadSize, Is.EqualTo(0));
        }

        [Test]
        public void ToOrgModule_InvalidAndMissingFieldsUseEmptyCollectionsAndFallbackIds()
        {
            UserModulesResponse response = Deserialize(MissingAndInvalidFieldsJson);

            Assert.That(response.modules[0].ToOrgModule().ID, Is.EqualTo(-1));
            Assert.That(response.modules[0].ToOrgModule().Downloads, Is.Empty);
            Assert.That(response.modules[0].ToOrgModule().player, Is.Null);

            OrgModule secondModule = response.modules[1].ToOrgModule();
            Assert.That(secondModule.ID, Is.EqualTo(-1));
            Assert.That(secondModule.Downloads, Is.Empty);
            Assert.That(secondModule.player, Is.Null);
            Assert.That(secondModule.AvailableLanguages, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ToOrgModules_NullInputIsEmptyAndListsMapOneToOne()
        {
            Assert.That(((List<Module>)null).ToOrgModules(), Is.Empty);

            UserModulesResponse response = Deserialize(HappyPathJson);
            var modules = new List<Module>
            {
                response.modules[0],
                new Module { id = "" }
            };

            var orgModules = modules.ToOrgModules();

            Assert.That(orgModules, Has.Count.EqualTo(2));
            Assert.That(orgModules[0].ID, Is.EqualTo(101));
            Assert.That(orgModules[1].ID, Is.EqualTo(-1));
        }

        [Test]
        public void ToOrgModule_NullInputIsNull()
        {
            Assert.That(((Module)null).ToOrgModule(), Is.Null);
        }

        private static UserModulesResponse Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<UserModulesResponse>(json);
        }

        private static void AssertDownload(
            OrgModule orgModule,
            int versionId,
            string version,
            long size,
            string url,
            string platform)
        {
            OrgModuleDownload download = orgModule.Downloads.Single(item =>
                item.VersionID == versionId && item.Platform == platform);

            Assert.That(download.Version, Is.EqualTo(version));
            Assert.That(download.DownloadSize, Is.EqualTo(size));
            Assert.That(download.URL, Is.EqualTo(url));
        }
    }
}
