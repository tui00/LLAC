namespace LLAC;

public class LLAC(string file)
{
    public readonly string[] code = file.Split("\n");

    public string Convert()
    {
        return string.Join("\n", code.Select(ConvertLine));
    }

    public static string Convert(string file)
    {
        return new LLAC(file).Convert();
    }

    public static string ConvertLine(string line)
    {
        return line;
    }
}
