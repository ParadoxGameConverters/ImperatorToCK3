using System;

namespace ImperatorToCK3.Exceptions;

public class ConverterException : Exception {
	public ConverterException(string message) : base(message) { }

	public ConverterException() : base() {
	}

	public ConverterException(string? message, Exception? innerException) : base(message, innerException) {
	}
}