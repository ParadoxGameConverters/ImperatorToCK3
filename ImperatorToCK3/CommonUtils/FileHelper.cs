namespace ImperatorToCK3.CommonUtils;

using commonItems;
using System;
using Polly;
using System.IO;
using Exceptions;
using System.Text;

public static class FileHelper {
	private const string CloseProgramsHint = "You should close all programs that may be using the file.";

	private static bool IsFilesSharingViolation(Exception ex) {
		const int sharingViolationHResult = unchecked((int)0x80070020);
		return ex.HResult == sharingViolationHResult;
	}

	public static StreamWriter OpenWriteWithRetries(string filePath) => OpenWriteWithRetries(filePath, Encoding.UTF8);

	public static StreamWriter OpenWriteWithRetries(string filePath, Encoding encoding) {
		const int maxAttempts = 10;
		StreamWriter? writer = null;

		int currentAttempt = 0;

		var policy = Policy
			.Handle<IOException>(IsFilesSharingViolation)
			.WaitAndRetry(maxAttempts,
				sleepDurationProvider: _ => TimeSpan.FromSeconds(30),
				onRetry: (_, _, _) => {
					currentAttempt++;
					Logger.Warn($"Attempt {currentAttempt} to open \"{filePath}\" failed. {CloseProgramsHint}");
				});

		try {
			policy.Execute(() => writer = new StreamWriter(filePath, append: false, encoding));
		} catch (IOException ex) when (IsFilesSharingViolation(ex)) {
			Logger.Debug(ex.ToString());
			throw new UserErrorException($"Failed to open \"{filePath}\" for writing. {CloseProgramsHint}");
		}

		if (writer is null) {
			throw new UserErrorException($"Failed to open \"{filePath}\" for writing: unknown error.");
		}

		return writer;
	}

	public static void DeleteWithRetries(string filePath) {
		const int maxAttempts = 10;
		
		int currentAttempt = 0;
		
		var policy = Policy
			.Handle<IOException>(IsFilesSharingViolation)
			.WaitAndRetry(maxAttempts,
				sleepDurationProvider: _ => TimeSpan.FromSeconds(30),
				onRetry: (_, _, _) => {
					currentAttempt++;
					Logger.Warn($"Attempt {currentAttempt} to delete \"{filePath}\" failed.");
					Logger.Warn(CloseProgramsHint);
				});
		
		try {
			policy.Execute(() => File.Delete(filePath));
		} catch (IOException ex) when (IsFilesSharingViolation(ex)) {
			Logger.Debug(ex.ToString());
			throw new UserErrorException($"Failed to delete \"{filePath}\". {CloseProgramsHint}");
		}
	}
	
	public static void MoveWithRetries(string sourceFilePath, string destFilePath) {
		const int maxAttempts = 10;
		
		int currentAttempt = 0;
		
		var policy = Policy
			.Handle<IOException>(IsFilesSharingViolation)
			.WaitAndRetry(maxAttempts,
				sleepDurationProvider: _ => TimeSpan.FromSeconds(30),
				onRetry: (_, _, _) => {
					currentAttempt++;
					Logger.Warn($"Attempt {currentAttempt} to move \"{sourceFilePath}\" to \"{destFilePath}\" failed.");
					Logger.Warn(CloseProgramsHint);
				});
		
		try {
			policy.Execute(() => File.Move(sourceFilePath, destFilePath));
		} catch (IOException ex) when (IsFilesSharingViolation(ex)) {
			Logger.Debug(ex.ToString());
			throw new UserErrorException($"Failed to move \"{sourceFilePath}\" to \"{destFilePath}\". {CloseProgramsHint}");
		}
	}
}