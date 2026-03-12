

namespace ImperatorToCK3.CommonUtils;

using commonItems;
using commonItems.Exceptions;
using System;
using Polly;
using System.IO;
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

	// Ensures that the given directory path exists. If a file exists with the
	// same name as the desired directory it will be removed first. The method
	// retries the creation when a sharing violation occurs, much like the
	// other helpers in this class. This helps mitigate cases where a transient
	// lock or a stray file prevents folder creation.
	public static void EnsureDirectoryExists(string directoryPath) {
		if (string.IsNullOrEmpty(directoryPath)) {
			return;
		}

		// if the path already exists as a directory we're done
		if (Directory.Exists(directoryPath)) {
			return;
		}

		// if a file exists where we'd like a directory, bail out rather than
		// attempting to delete it.
		if (File.Exists(directoryPath)) {
			throw new UserErrorException(
				$"Cannot create directory \"{directoryPath}\" because a file with the same name already exists.");
		}

		const int maxAttempts = 10;
		int currentAttempt = 0;
		var policy = Policy
			.Handle<IOException>(IsFilesSharingViolation)
			.WaitAndRetry(maxAttempts,
				sleepDurationProvider: _ => TimeSpan.FromSeconds(1),
				onRetry: (_, _, _) => {
				currentAttempt++;
				Logger.Warn($"Attempt {currentAttempt} to create directory \"{directoryPath}\" failed.");
			});

		try {
			policy.Execute(() => Directory.CreateDirectory(directoryPath));
		} catch (IOException ex) when (IsFilesSharingViolation(ex)) {
			Logger.Debug(ex.ToString());
			throw new UserErrorException($"Failed to create directory \"{directoryPath}\". {CloseProgramsHint}");
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