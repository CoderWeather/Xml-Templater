namespace Templater;

sealed class TemplateFormatException : Exception {
    public TemplateFormatException() : base("Invalid template.") {
    }

    public TemplateFormatException(string message) : base(message) {
    }

    public TemplateFormatException(ReadOnlySpan<char> message) : base(message.ToString()) {
    }

    public TemplateFormatException(char ch, int index) : base($"Unexpected character '{ch}' at index {index}") {
    }

    public TemplateFormatException(string chars, int index) : base($"Unexpected characters '{chars}' at index {index}") {
    }
}
