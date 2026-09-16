using System.IO;
using System.Text;
using NUnit.Framework;

namespace PixoVR.Apex.Tests
{
    public class LogSubmissionTests
    {
        private const string AuthToken = "token-123";
        private const int ModuleId = 42;

        private string logPath;

        [SetUp]
        public void SetUp()
        {
            logPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".log");
            File.WriteAllText(logPath, "log line one\nlog line two\n");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(logPath))
                File.Delete(logPath);
        }

        [Test]
        public void Prepare_WhenNotSignedIn_RefusesAndSendsNothing()
        {
            FailureResponse failure = LogSubmission.Prepare(null, ModuleId, logPath, out LogSubmissionRequest request);

            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.Message, Does.Contain("signed in"));
            Assert.That(request, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        public void Prepare_WithNoFilePath_RefusesAndSendsNothing(string filePath)
        {
            FailureResponse failure = LogSubmission.Prepare(AuthToken, ModuleId, filePath, out LogSubmissionRequest request);

            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.Message, Does.Contain("file is required"));
            Assert.That(request, Is.Null);
        }

        [Test]
        public void Prepare_WhenFileDoesNotExist_RefusesAndSendsNothing()
        {
            File.Delete(logPath);

            FailureResponse failure = LogSubmission.Prepare(AuthToken, ModuleId, logPath, out LogSubmissionRequest request);

            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.Message, Does.Contain("could not be found"));
            Assert.That(request, Is.Null);
        }

        [Test]
        public void Prepare_WhenFileCannotBeRead_RefusesAndSendsNothing()
        {
            using (new FileStream(logPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                FailureResponse failure = LogSubmission.Prepare(AuthToken, ModuleId, logPath, out LogSubmissionRequest request);

                Assert.That(failure, Is.Not.Null);
                Assert.That(failure.Message, Does.Contain("could not be read"));
                Assert.That(request, Is.Null);
            }
        }

        [Test]
        public void Prepare_WithReadableFile_BuildsRequestForTheModule()
        {
            FailureResponse failure = LogSubmission.Prepare(AuthToken, ModuleId, logPath, out LogSubmissionRequest request);

            Assert.That(failure, Is.Null);
            Assert.That(request.AuthToken, Is.EqualTo("token-123"));
            Assert.That(request.FormFields["moduleId"], Is.EqualTo("42"));
            Assert.That(request.FileName, Is.EqualTo(Path.GetFileName(logPath)));
            Assert.That(Encoding.UTF8.GetString(request.FileData), Is.EqualTo("log line one\nlog line two\n"));
        }

        [Test]
        public void Prepare_WhileLogIsStillBeingWritten_ReadsTheWholeFile()
        {
            using (var writer = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                byte[] line = Encoding.UTF8.GetBytes("log line three\n");
                writer.Write(line, 0, line.Length);
                writer.Flush();

                FailureResponse failure = LogSubmission.Prepare(AuthToken, ModuleId, logPath, out LogSubmissionRequest request);

                Assert.That(failure, Is.Null);
                Assert.That(Encoding.UTF8.GetString(request.FileData), Is.EqualTo("log line one\nlog line two\nlog line three\n"));
            }
        }

        [Test]
        public void Interpret_WhenPlatformAcceptsTheLog_ReportsWhereItWasFiled()
        {
            const string body = @"{""message"":""successfully submitted builder log"",""location"":""42/7/9/20260916T120000Z-crash.log""}";

            FailureResponse failure = LogSubmission.Interpret(200, body, out SubmitLogResponse response);

            Assert.That(failure, Is.Null);
            Assert.That(response.Message, Is.EqualTo("successfully submitted builder log"));
            Assert.That(response.Location, Is.EqualTo("42/7/9/20260916T120000Z-crash.log"));
        }

        [TestCase(400, "unsupported file type", "only .txt, .log and .zip files are accepted")]
        [TestCase(404, "module not found", "module not found")]
        [TestCase(413, "log file is too large", "log file exceeds the maximum size of 64MB")]
        public void Interpret_WhenPlatformRefusesTheLog_ReportsThePlatformsReason(int statusCode, string message, string error)
        {
            string body = $@"{{""message"":""{message}"",""error"":""{error}""}}";

            FailureResponse failure = LogSubmission.Interpret(statusCode, body, out SubmitLogResponse response);

            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.HttpCode, Is.EqualTo(statusCode.ToString()));
            Assert.That(failure.Message, Is.EqualTo(message));
            Assert.That(failure.Error, Is.EqualTo(error));
            Assert.That(response, Is.Null);
        }

        [TestCase(0, null)]
        [TestCase(0, "")]
        [TestCase(502, "<html>Bad Gateway</html>")]
        [TestCase(200, "")]
        [TestCase(200, "<html>Sign in to Wi-Fi</html>")]
        public void Interpret_WhenPlatformCannotBeReached_ReportsTheSubmissionFailed(int statusCode, string body)
        {
            FailureResponse failure = LogSubmission.Interpret(statusCode, body, out SubmitLogResponse response);

            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.HttpCode, Is.EqualTo(statusCode.ToString()));
            Assert.That(failure.Message, Does.Contain("submission failed"));
            Assert.That(response, Is.Null);
        }
    }
}
