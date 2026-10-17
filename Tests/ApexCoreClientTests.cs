using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PixoVR.Apex.Native;
using UnityEngine;

namespace PixoVR.Apex.Tests
{
	/// <summary>
	/// Calls into the native apex_core library. They need the library for the editor's platform
	/// (Windows x64 today) and are skipped where it is missing, such as the Linux CI editor.
	/// None of them reaches the network.
	/// </summary>
	public class ApexCoreClientTests
	{
		private readonly List<ApexCoreClient> m_createdClients = new List<ApexCoreClient>();

		[SetUp]
		public void RequireNativeLibrary()
		{
			bool libraryLoaded = ApexCoreClient.TryLoadLibrary(out string errorMessage);
			Assume.That(libraryLoaded, Is.True, errorMessage);
		}

		[TearDown]
		public void DestroyClients()
		{
			foreach (ApexCoreClient createdClient in m_createdClients)
			{
				createdClient.Dispose();
			}

			m_createdClients.Clear();
		}

		[Test]
		public void ConfigLayoutMatchesTheLibrary()
		{
			ApexConfigData config = default;
			ApexNative.ApexConfigInit(ref config);

			Assert.That(config.StructSize, Is.EqualTo((uint)Marshal.SizeOf<ApexConfigData>()),
				"ApexConfigData no longer matches ApexConfig in Apex.h.");
			Assert.That(config.HeartbeatIntervalSeconds, Is.EqualTo(5.0));
			Assert.That(config.RequestTimeoutMilliseconds, Is.EqualTo(30000u));
		}

		[Test]
		public void LibraryReportsVersionAndBackend()
		{
			Assert.That(ApexCoreClient.GetLibraryVersion(), Does.Match(@"^\d+\.\d+\.\d+"));
			Assert.That(ApexCoreClient.GetBuiltinTransportName(), Is.Not.Empty);
		}

		[Test]
		public void ModuleVersionRulesComeFromTheCore()
		{
			Assert.That(ApexCoreClient.IsModuleVersionValid("1.4.0"), Is.True);
			Assert.That(ApexCoreClient.IsModuleVersionValid("0.4.0"), Is.False);
			Assert.That(ApexCoreClient.IsModuleVersionValid("1.400.0"), Is.False);
			Assert.That(ApexCoreClient.IsModuleVersionValid(null), Is.False);
		}

		[Test]
		public void CallsBeforeLoginFailOnTheNextPoll()
		{
			ApexCoreClient coreClient = CreateClient(new ApexCoreSettings());
			ApexResponse moduleListResponse = null;
			coreClient.GetModuleList("android", (response, payloadJson) => moduleListResponse = response);
			Assert.That(moduleListResponse, Is.Null, "Results arrive on poll, never inside the call.");

			coreClient.Poll(1.0);

			Assert.That(moduleListResponse, Is.Not.Null);
			Assert.That(moduleListResponse.Error, Is.EqualTo(EApexError.APEX_ERR_NOT_LOGGED_IN));

			ApexResponse modulePageResponse = null;
			coreClient.GetModulePage("android", EModuleLifecycle.RELEASED | EModuleLifecycle.QA, EModuleSortOrder.NAME,
				0, 10, false, (response, payloadJson) => modulePageResponse = response);
			coreClient.Poll(2.0);
			Assert.That(modulePageResponse, Is.Not.Null);
			Assert.That(modulePageResponse.Error, Is.EqualTo(EApexError.APEX_ERR_NOT_LOGGED_IN));
			Assert.That(coreClient.IsLoggedIn(), Is.False);
			Assert.That(coreClient.GetCurrentUserJson(), Is.Empty);
		}

		[Test]
		public void LaunchArgumentsBuildTheExitTarget()
		{
			ApexCoreClient coreClient = CreateClient(new ApexCoreSettings());
			coreClient.SetLaunchArgument("pixotoken", "launch-token");
			coreClient.SetLaunchArgument("returntarget", "https://hub.test/return");
			coreClient.SetLaunchArgument("targettype", "url");

			JObject exitTarget = JObject.Parse(coreClient.GetExitTargetJson(null));

			Assert.That(coreClient.GetLaunchArgument("pixotoken"), Is.EqualTo("launch-token"));
			Assert.That((string)exitTarget["action"], Is.EqualTo("open_url"));
			Assert.That((string)exitTarget["url"], Does.StartWith("https://hub.test/return?"));
			Assert.That((string)exitTarget["url"], Does.Contain("pixotoken=launch-token"));
		}

		[Test]
		public void TelemetryPacketsReachTheCallback()
		{
			List<string> receivedPackets = new List<string>();
			ApexCoreSettings coreSettings = new ApexCoreSettings();
			coreSettings.TelemetryDestination = ETelemetryDestination.CALLBACK;
			coreSettings.TelemetryPacketHandler = receivedPackets.Add;
			ApexCoreClient coreClient = CreateClient(coreSettings);

			coreClient.TelemetryObjectRegistered("lever-1", "Lever", "LeverMesh", new Vector3(1.234f, 0f, -0.5f),
				Vector3.zero);
			coreClient.TelemetryStepEnd("step", true, 0.75);
			coreClient.TelemetryFlush();

			Assert.That(receivedPackets, Has.Count.EqualTo(1));
			JObject packet = JObject.Parse(receivedPackets[0]);
			Assert.That((string)packet["ob"][0]["id"], Is.EqualTo("lever-1"));
			Assert.That(packet["ev"][0]["p"].ToObject<int[]>(), Is.EqualTo(new[] { 123, 0, -50 }));
			Assert.That((string)packet["ev"][1]["k"], Is.EqualTo("se"));
		}

		private ApexCoreClient CreateClient(ApexCoreSettings coreSettings)
		{
			coreSettings.PlatformServer = EPlatformServer.LOCAL_DEV;
			coreSettings.ModuleId = 13;
			coreSettings.ModuleVersion = "1.0.0";
			ApexCoreClient coreClient = ApexCoreClient.Create(coreSettings, out string errorMessage);
			Assert.That(coreClient, Is.Not.Null, errorMessage);
			m_createdClients.Add(coreClient);
			return coreClient;
		}
	}
}
