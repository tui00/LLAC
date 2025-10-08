namespace LLAC;

public partial class LLAC(string file)
{
    public readonly string file = file;

    public int nextLoopId = 0;

    public string Convert()
    {
        return string.Join("\n", file.Split("\n").Select(ConvertLine));
    }

    public string ConvertLine(string line)
    {
        return ConvertWords(RemoveComments(line).Trim().Split(" "));
    }

    private static string RemoveComments(string line)
    {
        line += " ";
        bool inString = false;
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == '\\') i++;
            else if (ch == '"') inString = !inString;
            else if (ch == ';' && !inString) return line[..i].TrimEnd();
        }
        return line;
    }

    private string ConvertWords(string[] words)
    {
        words = [.. words.Select(word => word.Replace(",", ""))];
        if (words.Length == 0)
            return "";
        string op = words[0];
        string[] args = words[1..];

        string label = $"_loopLLAC{nextLoopId}";

        string[] fragment;
        switch (op.ToLower())
        {
            // === Доп. команды ===
            case "setup" when args.Length > 0 && args.Length <= 5:
                byte devices = 0;
                if (args.Contains("display")) devices |= 0b00_01_0000; // Устоновка монохроного режима экрана
                if (args.Contains("coldisplay")) devices |= 0b00_11_0000; // Устоновка цветного режима экрана
                if (args.Contains("terminal")) devices |= 0b0000000_1; // Устоновка терминала
                if (args.Contains("digit")) devices |= 0b00000_01_0; // Устоновка беззнакового режима
                if (args.Contains("digitsign")) devices |= 0b00000_11_0; // Устоновка знакового режима
                fragment = [
                    $"ldi a, {devices}", // Сохраняем девайсы в регистер
                    "st a, 0x3E" // Записываем в порт
                ];
                break;
            case "readkey" when args.Length == 1:
                // 0x3E порт ввода
                fragment = [
                    $"{label}:ld {args[0]}, 0x3E", // Считываем
                    $"test {args[0]}", // Если ничего
                    $"jz {label}" // Переходим на метку
                ];
                nextLoopId++;
                break;

            // === Алиасы ===
            case "exit":
                fragment = ["hlt"];
                break;

            // === Остальное ===
            default:
                fragment = [string.Join(" ", words)];
                break;
        }

        return string.Join("\n", fragment);
    }
}
