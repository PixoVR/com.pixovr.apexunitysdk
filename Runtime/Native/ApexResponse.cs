using PixoVR.Apex.Native;

namespace PixoVR.Apex
{
	/// <summary>
	/// The outcome of a platform call, passed to every success and failure callback. It replaces
	/// the HttpResponseMessage of earlier SDK versions.
	/// </summary>
	public sealed class ApexResponse
	{
		public ApexResponse(EApexError error, int httpStatus, string message)
		{
			Error = error;
			HttpStatus = httpStatus;
			Message = message ?? string.Empty;
		}

		/// <summary>APEX_OK on success; otherwise why the call failed.</summary>
		public EApexError Error { get; }

		/// <summary>The HTTP status, or 0 when no response was received or no request was sent.</summary>
		public int HttpStatus { get; }

		/// <summary>Empty on success; otherwise a description of the failure.</summary>
		public string Message { get; }

		public bool Succeeded => Error == EApexError.APEX_OK;

		/// <summary>A failure detected before any request was sent, such as a missing argument.</summary>
		public static ApexResponse LocalFailure(EApexError error, string message)
		{
			return new ApexResponse(error, 0, message);
		}

		/// <summary>The same failure in the shape older callbacks expect.</summary>
		public FailureResponse ToFailureResponse()
		{
			FailureResponse failureResponse = new FailureResponse();
			failureResponse.Error = Succeeded ? "false" : "true";
			failureResponse.HttpCode = HttpStatus == 0 ? string.Empty : HttpStatus.ToString();
			failureResponse.Message = Message;
			return failureResponse;
		}

		public override string ToString()
		{
			return Succeeded ? "OK" : Error + " (HTTP " + HttpStatus + "): " + Message;
		}
	}
}
