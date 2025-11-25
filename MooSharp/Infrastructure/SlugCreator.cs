using System.Text;

namespace MooSharp;

public class SlugCreator
{
    public string CreateSlug(string roomName)
    {
        var normalized = roomName.Trim().ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        var wroteDash = false;

        foreach (var c in normalized)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                wroteDash = false;
            }
            else if (!wroteDash && (char.IsWhiteSpace(c) || c is '-' or '_'))
            {
                builder.Append('-');
                wroteDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}