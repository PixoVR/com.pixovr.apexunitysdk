using NUnit.Framework;
using PixoVR.Apex.Utils;
using PixoVR.Editor;

namespace PixoVR.Apex.Tests
{
    public class SourceHashTests
    {
        [Test]
        public void SDKSourceHash_MatchesRuntimeSources()
        {
            string packageRoot = ApexSourceHash.FindPackageRoot();
            Assume.That(packageRoot, Is.Not.Null, "The SDK package root could not be found.");

            Assert.That(ApexUtils.SDKSourceHash, Is.EqualTo(ApexSourceHash.Compute(packageRoot)),
                "Runtime/SDK/ApexSourceHashGenerated.cs is stale. Run: python3 \"Tools~/source_hash.py\" --write");
        }
    }
}
