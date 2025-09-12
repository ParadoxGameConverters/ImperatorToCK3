using System;

namespace ImperatorToCK3.Exceptions;

internal class ConverterException : Exception {
	public ConverterException(string message) : base(message) { }

	public ConverterException(string? message, Exception? innerException) : base(message, innerException) { }

	public ConverterException() : base() { }
}