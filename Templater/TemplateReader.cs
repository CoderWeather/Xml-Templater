namespace Templater;

public ref struct TemplateReader(
    ReadOnlySpan<char> template
) {
    public readonly ReadOnlySpan<char> Template = template;
    int offset;
}
