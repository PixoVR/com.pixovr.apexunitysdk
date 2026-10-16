using System;
using UnityEngine;

namespace PixoVR.Apex
{
	/// <summary>
	/// JNI calls that log and return a default instead of throwing when the Java side fails.
	/// </summary>
	public static class SafeJNI
	{
		private const string kLogTag = "SafeJNI";

		/// <summary>Calls a static void Java method. Returns false when it fails.</summary>
		public static bool SafeCallStatic(this AndroidJavaObject javaObject, string methodName,
			params object[] methodArguments)
		{
			return Run(javaObject, () =>
			{
				if (methodArguments == null || methodArguments.Length == 0)
				{
					javaObject.CallStatic(methodName);
				}
				else
				{
					javaObject.CallStatic(methodName, methodArguments);
				}

				return true;
			});
		}

		/// <summary>Calls a static Java method and returns its result, or the default when it fails.</summary>
		public static TReturn SafeCallStatic<TReturn>(this AndroidJavaObject javaObject, string methodName,
			params object[] methodArguments)
		{
			return Run(javaObject, () => methodArguments == null || methodArguments.Length == 0
				? javaObject.CallStatic<TReturn>(methodName)
				: javaObject.CallStatic<TReturn>(methodName, methodArguments));
		}

		/// <summary>Calls a void Java method. Returns false when it fails.</summary>
		public static bool SafeCall(this AndroidJavaObject javaObject, string methodName,
			params object[] methodArguments)
		{
			return Run(javaObject, () =>
			{
				if (methodArguments == null || methodArguments.Length == 0)
				{
					javaObject.Call(methodName);
				}
				else
				{
					javaObject.Call(methodName, methodArguments);
				}

				return true;
			});
		}

		/// <summary>Calls a Java method and returns its result, or the default when it fails.</summary>
		public static TReturn SafeCall<TReturn>(this AndroidJavaObject javaObject, string methodName,
			params object[] methodArguments)
		{
			return Run(javaObject, () => methodArguments == null || methodArguments.Length == 0
				? javaObject.Call<TReturn>(methodName)
				: javaObject.Call<TReturn>(methodName, methodArguments));
		}

		/// <summary>Reads a static Java field, or the default when it fails.</summary>
		public static TReturn SafeGetStatic<TReturn>(this AndroidJavaObject javaObject, string fieldName)
		{
			return Run(javaObject, () => javaObject.GetStatic<TReturn>(fieldName));
		}

		/// <summary>Reads a Java field, or the default when it fails.</summary>
		public static TReturn SafeGet<TReturn>(this AndroidJavaObject javaObject, string fieldName)
		{
			return Run(javaObject, () => javaObject.Get<TReturn>(fieldName));
		}

		private static TReturn Run<TReturn>(AndroidJavaObject javaObject, Func<TReturn> javaCall)
		{
			if (javaObject == null)
			{
				return default;
			}

			try
			{
				return javaCall();
			}
			catch (AndroidJavaException javaException)
			{
				Debug.unityLogger.Log(LogType.Exception, kLogTag, "JNI Exception: " + javaException);
			}
			catch (Exception exception)
			{
				Debug.unityLogger.Log(LogType.Exception, kLogTag, "Unexpected Exception: " + exception);
			}

			return default;
		}
	}
}
