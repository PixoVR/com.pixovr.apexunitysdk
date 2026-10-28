using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace PixoVR.Apex
{
    public class LogSubmissionRequest
    {
        public string AuthToken;
        public Dictionary<string, string> FormFields;
        public string FileName;
        public byte[] FileData;
    }

    [Serializable]
    public class SubmitLogResponse
    {
        public string Message;
        public string Location;
    }

    public static class LogSubmission
    {
        public static FailureResponse Prepare(string authToken, int moduleId, string filePath, out LogSubmissionRequest request)
        {
            request = null;

            if (string.IsNullOrEmpty(authToken))
                return Failure("You need to be signed in to submit a log.");

            if (string.IsNullOrEmpty(filePath))
                return Failure("A log file is required.");

            if (!File.Exists(filePath))
                return Failure($"The log file could not be found: {filePath}");

            byte[] fileData;
            try
            {
                fileData = ReadSharedFile(filePath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return Failure($"The log file could not be read: {ex.Message}");
            }

            request = new LogSubmissionRequest
            {
                AuthToken = authToken,
                FormFields = new Dictionary<string, string> { { "moduleId", moduleId.ToString() } },
                FileName = Path.GetFileName(filePath),
                FileData = fileData,
            };
            return null;
        }

        // Unity keeps its own log open for writing; File.ReadAllBytes refuses that on Windows.
        private static byte[] ReadSharedFile(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
        }

        public static FailureResponse Interpret(long statusCode, string body, out SubmitLogResponse response)
        {
            response = null;
            FailureResponse failure;

            if (statusCode >= 200 && statusCode < 300)
            {
                response = TryParse<SubmitLogResponse>(body);
                if (response != null)
                    return null;

                failure = null;
            }
            else
            {
                failure = TryParse<FailureResponse>(body);
            }

            if (failure == null || string.IsNullOrEmpty(failure.Message))
                failure = Failure("The log submission failed: the platform could not be reached or did not give a readable answer.");

            failure.HttpCode = statusCode.ToString();
            return failure;
        }

        private static T TryParse<T>(string body) where T : class
        {
            if (string.IsNullOrEmpty(body))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<T>(body);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static FailureResponse Failure(string message)
        {
            return new FailureResponse { Error = "true", Message = message };
        }
    }
}
