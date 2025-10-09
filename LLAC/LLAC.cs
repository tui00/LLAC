namespace LLAC;

public partial class LLAC(string file)
{
    public readonly string file = file;

    public int nextLoopId = 0;

    public string Convert()
    {
        return string.Join("\n", file.Split("\n").Select(ConvertLine)).Replace("\n\n", "\n").Trim();
    }

    private static string RemoveComments(string line)
    {
        bool inString = false;
        int i;
        for (i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == '\\') i++;
            else if (ch == '"') inString = !inString;
            else if (ch == ';' && !inString) break;
        }
        if (i < 0) i = 0;
        return line[..i].Trim(' ');
    }

    public string ConvertLine(string line)
    {
        line = RemoveComments(line);
        string[] words = [.. line.Split(' ')];
        if (words.Length == 0)
            return "";

        string op = words[0];
        string[] args = [.. words[1..].Select(arg => arg.Trim(','))];

        string[] fragment;
        switch (op.ToLower())
        {
            // === Доп. команды ===
            case "connect" when args.Length > 0 && args.Length <= 5: fragment = Connect(args); break;
            case "readchar" when args.Length == 1: fragment = ReadChar(args, GetLabel); break;
            case "writechar" when words[1..].Length != 0: fragment = WriteChar(string.Join(" ", words[1..])); break;
            case "writeline" when words[1..].Length != 0: fragment = WriteLine(args, GetLabel); break;

            // === Остальное ===
            default:
                if (TryAlias(op, args, out string[] aliasFragment))
                {
                    fragment = aliasFragment;
                }
                else
                {
                    fragment = [line];
                }
                break;
        }

        return string.Join("\n", fragment);
    }

    private string GetLabel()
    {
        return $"_loopLLAC" + nextLoopId++;
    }
}
