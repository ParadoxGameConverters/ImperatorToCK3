using System;

namespace ImperatorToCK3.Exceptions;

class UserErrorException : ConverterException {
    public UserErrorException(string message) : base(message) { }

    public UserErrorException(string? message, Exception? innerException) : base(message, innerException) {
    }
}