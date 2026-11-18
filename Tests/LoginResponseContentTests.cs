using Newtonsoft.Json;
using NUnit.Framework;

namespace PixoVR.Apex.Tests
{
    public class LoginResponseContentTests
    {
        private const string LoginWithPixoBuilderJson = @"
{
  ""ID"": 12,
  ""Email"": ""user@example.test"",
  ""Token"": ""token"",
  ""Licenses"": [
    { ""id"": 3, ""type"": ""pixo-builder"", ""name"": ""Pixo Builder"" }
  ]
}";

        private const string LoginWithoutLicensesJson = @"
{
  ""ID"": 12,
  ""Email"": ""user@example.test"",
  ""Token"": ""token""
}";

        private const string LoginWithNullLicensesJson = @"
{
  ""ID"": 12,
  ""Email"": ""user@example.test"",
  ""Token"": ""token"",
  ""Licenses"": null
}";

        private static LoginResponseContent Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<LoginResponseContent>(json);
        }

        [Test]
        public void Deserialize_ReadsEachLicense()
        {
            LoginResponseContent login = Deserialize(LoginWithPixoBuilderJson);

            Assert.AreEqual(1, login.Licenses.Count);
            Assert.AreEqual(3, login.Licenses[0].Id);
            Assert.AreEqual(LicenseTypes.PixoBuilder, login.Licenses[0].Type);
            Assert.AreEqual("Pixo Builder", login.Licenses[0].Name);
        }

        [Test]
        public void HasLicense_IsTrueForALicenseTheUserHas()
        {
            LoginResponseContent login = Deserialize(LoginWithPixoBuilderJson);

            Assert.IsTrue(login.HasLicense(LicenseTypes.PixoBuilder));
        }

        [Test]
        public void HasLicense_IsFalseForALicenseTheUserDoesNotHave()
        {
            LoginResponseContent login = Deserialize(LoginWithPixoBuilderJson);

            Assert.IsFalse(login.HasLicense("another-license"));
        }

        [Test]
        public void HasLicense_IsFalseWhenTheLoginHasNoLicenses()
        {
            Assert.IsFalse(Deserialize(LoginWithoutLicensesJson).HasLicense(LicenseTypes.PixoBuilder));
            Assert.IsFalse(Deserialize(LoginWithNullLicensesJson).HasLicense(LicenseTypes.PixoBuilder));
        }
    }
}
