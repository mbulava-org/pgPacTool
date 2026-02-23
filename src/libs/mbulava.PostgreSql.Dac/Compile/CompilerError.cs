namespace mbulava.PostgreSql.Dac.Compile;

/// <summary>
/// Represents a compilation error
/// </summary>
public class CompilerError
{
    public string Code { get; }
    public string Message { get; }
    public string Location { get; }

    public CompilerError(string code, string message, string location)
    {
        Code = code;
        Message = message;
        Location = location;
    }

    public override string ToString()
    {
        return $"{Code}: {Message} at {Location}";
    }
}

