namespace LLAC;

public class LLAC(string file)
{
    public readonly string file = file;

    public int nextLoopId = 0;

    public string Convert()
    {
        return string.Join("\n", file.Split("\n").Select(ConvertLine));
    }

    public string ConvertLine(string line)
    {
        return ConvertWords(line.Split(" "));
    }

    private string ConvertWords(string[] words)
    {
        if (words.Length == 0)
            return "";
        string op = words[0];
        string[] args = words[1..];

        string[] fragment = [];

        string label = $"_loopLLAC{nextLoopId++}";
        switch (op.ToLower())
        {
            // === Доп. команды ===
            case "readkey":
                // 0x3E порт ввода
                fragment = [
                    $"{label}:", // Сохраняем метку
                    $"ld {args[0]} 0x3E", // Считываем
                    $"test {args[0]}", // Если ничего
                    $"jz {label}" // Переходим на метку
                ];
                break;

            // === Алиасы ===
            case "exit":
                fragment = ["hlt"];
                break;

            // === Остальное ===
            default:
                fragment = [string.Join(" ", words)];
                nextLoopId--; // Вернуть как было
                break;
        }

        return string.Join("\n", fragment);
    }
}
