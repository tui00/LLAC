using System.Text;

namespace LLAC;

public record Components(string? Label, string Op, string[] Args)
{
    public override string ToString()
    {
        return $"{Label ?? ""}{(Label != null ? ":" : "")}{Op} {string.Join(",", Args)}";
    }

    public static Components Parse(string line)
    {
        string? label = null;
        string op;
        List<string> args = new(line.Split(",").Length);

        string[] words = line.Split(' ');

        if (words[0].Contains(':'))
        {
            int labelEnd = line.IndexOf(':');
            label = line[..labelEnd];
            line = line[(labelEnd + 1)..].Trim();
        }

        words = line.Split(' ');

        op = words[0].Trim();

        string content = string.Join(' ', words[1..]).Trim(); // Берем все после op
        StringBuilder curArg = new(); // Текущий аргумент, который составляется
        bool inString = false; // Находится ли символ в строке
        bool escaped = false;
        for (int i = 0; i < content.Length; i++)
        {
            char ch = content[i];

            if (ch == '"' && !escaped) inString = !inString;
            else if (ch == '\\' && !escaped) escaped = true;
            else escaped = false;

            if (!inString && ch == ',')
            {
                args.Add(curArg.ToString());
                curArg.Clear();
                continue;
            }

            curArg.Append(ch);
        }
        if (curArg.Length != 0)
            args.Add(curArg.ToString());

        return new(label, op.ToLower(), [.. args]);
    }
}
