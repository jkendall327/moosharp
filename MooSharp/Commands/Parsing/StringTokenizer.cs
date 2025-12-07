namespace MooSharp.Commands.Parsing;

public static class StringTokenizer
{
    public static Queue<string> Tokenize(string input)
    {
        var tokens = new Queue<string>();
        var currentToken = new System.Text.StringBuilder();
        var inQuotes = false;

        // We care about quotes so we can handle inputs like
        // 'give "shiny sword" to bob'
        // where 'shiny sword' should be one token.

        foreach (var c in input)
        {
            if (c is '"' or '\'')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (currentToken.Length > 0)
                {
                    tokens.Enqueue(currentToken.ToString());
                    currentToken.Clear();
                }
            }
            else
            {
                currentToken.Append(c);
            }
        }

        if (currentToken.Length > 0)
        {
            tokens.Enqueue(currentToken.ToString());
        }

        return tokens;
    }
}