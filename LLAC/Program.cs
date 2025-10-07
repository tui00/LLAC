namespace LLAC;

class Program
{
    internal static readonly string[] helpArgs = ["-h", "--help", "/?"];

    static int Main(string[] args)
    {
#if DEBUG
        if (args.Length == 0) args = ["file.llac", "file.asm"];
#endif
        if (args.Length != 2 || helpArgs.Any(t => args.Select(a => a.ToLower()).Contains(t)))
        {
            Console.WriteLine("https://github.com/tui00/LLAC -- help");
            return 0;
        }
        if (!File.Exists(args[0]))
        {
            Console.WriteLine("File not found");
            return 1;
        }

        string path = args[0];

        LLAC llac = new(File.ReadAllText(path).Replace("\r", ""));
        string output = llac.Convert();
        File.WriteAllText(args[1], output);
        return 0;
    }
}

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
