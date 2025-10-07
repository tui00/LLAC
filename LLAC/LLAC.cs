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

        switch (op)
        {
            // === Доп. команды ===
            case "in":
                // 0x3E порт ввода
                fragment = [
                    $"{GetLoopLabel(false)}:", // Сохраняем метку
                    $"ld {args[0]} 0x3E", // Считываем
                    $"test {args[0]}", // Если ничего
                    $"jz {GetLoopLabel(true)}" // Переходим на метку
                ];
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

    private string GetLoopLabel(bool change)
    {
        int tmp = nextLoopId;
        nextLoopId += change ? 1 : 0;
        return $"_loopLLAC{tmp}";
    }
}
