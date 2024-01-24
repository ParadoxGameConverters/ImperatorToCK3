namespace ImperatorToCK3.CommonUtils;

using commonItems;
using System;
using Polly;
using System.IO;
using Exceptions;
using System.Text;

public static class FileOpeningHelper {
	private static bool IsFilesSharingViolation(Exception ex) {
		const int sharingViolationHResult = unchecked((int)0x80070020);
		return ex.HResult == sharingViolationHResult;
	}

	public static StreamWriter OpenWriteWithRetries(string filePath) => OpenWriteWithRetries(filePath, Encoding.UTF8);

	public static StreamWriter OpenWriteWithRetries(string filePath, Encoding encoding) {
		const int maxAttempts = 5;
		StreamWriter? writer = null;

		var policy = Policy
			.Handle<IOException>(IsFilesSharingViolation)
			.WaitAndRetry(maxAttempts, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
				(exception, timeSpan, context) => {
					Logger.Warn($"Attempt {context.Count} to open \"{filePath}\" failed. Retrying...");
				});

		try {
			policy.Execute(() => {
				writer = new StreamWriter(filePath, append: false, encoding);
			});
		} catch (IOException ex) when (IsFilesSharingViolation(ex)) {
			Logger.Debug(ex.ToString());

			string errorMessage = $"Failed to open \"{filePath}\" for writing: {ex.Message}";
			if (!errorMessage.EndsWith('.')) {
				errorMessage += '.';
			}

			errorMessage += " Close all programs that may be using the file and try again.";

			throw new UserErrorException(errorMessage);
		}

		if (writer is null) {
			throw new UserErrorException($"Failed to open \"{filePath}\" for writing: unknown error.");
		}

		return writer;
	}
}