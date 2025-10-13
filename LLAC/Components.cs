namespace LLAC;

public record Components(string? Label, string Op, string[] Args)
{
    public override string ToString()
    {
        return $"{Label ?? ""}{(Label != null ? ":" : "")}{Op} {string.Join(",", Args)}";
    }

    public static explicit operator string(Components components) => components.ToString();
}
