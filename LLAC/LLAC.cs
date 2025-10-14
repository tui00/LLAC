namespace LLAC;

public partial class Llac(string file)
{
    public readonly string file = file;

    public int NextLabelId { get; set; } = 0;
    public byte NextCmdAddr { get; set; } = 0;
    public byte ConnectedDevices { get; set; } = 0;
    public byte PreConnectedDevices { get; set; } = 0;
    public byte[] PreImage { get; set; } = [];

    public byte DisplayColorsCount
    {
        get
        {
            byte displayActive = (byte)((PreConnectedDevices & 1 << 4) >> 4);
            byte displayColorsCount = (byte)((((PreConnectedDevices & (1 << 5)) >> 5) + 1) * displayActive);
            return displayColorsCount;
        }
    }

    public string Convert()
    {
        string[] codeLines = [.. file.Split("\n").Select(ConvertLine)];

        if (NextCmdAddr <= 0x3A && (PreConnectedDevices != 0))
        {
            codeLines = [.. codeLines, GetPorts()];
        }
        for (int i = 0; i < codeLines.Length; i++)
        {
            Components components = Components.Parse(codeLines[i]);
            if (components.Op == "db")
            {
                codeLines[i] = $"{components.Label} db {string.Join(',', components.Args)}";
            }
        }

        return string.Join("\n", codeLines.Where(line => !string.IsNullOrEmpty(line))).Trim();
    }

    public string ConvertLine(string line, int number)
    {
        line = RemoveComments(line.Trim());
        if (string.IsNullOrWhiteSpace(line)) return "";
        try
        {
            Components components = Components.Parse(line);

            string[] fragment;

            if (Commands.LlacCommands.TryGetValue(components.Op, out var result))
            {
                if (!result.condition(components.Args.Length, components))
                    throw new ArgumentException($"Invalid args for \"{components.Op}\"");
                else
                    fragment = result.handler(components, this);
            }
            else
            {
                fragment = [components.ToString()];
            }
            NextCmdAddr += Asm.GetLength(fragment);

            byte freeAddr = (byte)(0x40 + 0x20 * DisplayColorsCount);

            if (NextCmdAddr > 0x38 && NextCmdAddr < freeAddr)
            {
                NextCmdAddr -= Asm.GetLength(fragment);
                fragment = [GetPorts([3, freeAddr]), .. fragment];
                NextCmdAddr += Asm.GetLength(fragment);
            }

            return string.Join("\n", fragment);
        }
        catch (ArgumentException e)
        {
            Console.WriteLine($"[WARN] {e.Message} (at line {number + 1})");
            return line;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERROR] {e.Message} (at line {number + 1})");
            return line;
        }
    }

    private static string RemoveComments(string line)
    {
        bool inString = false;
        int i = 0;
        while (i < line.Length)
        {
            char ch = line[i];
            if (ch == '\\') i++;
            else if (ch == '"') inString = !inString;
            else if (ch == ';' && !inString) break;
            i++;
        }
        return line[..i].TrimEnd();
    }

    public string GetLabel()
    {
        return "_labelLLAC" + NextLabelId++;
    }

    private string GetPorts(byte[]? additionalCode = null)
    {
        additionalCode ??= [];
        int[] args = [
            .. Enumerable.Repeat(0, 0x3A - additionalCode.Length - NextCmdAddr), // Нули
            .. additionalCode, // Дополнительный код
            0, // Порт для счетчика
            0, // Порт для счетчика
            0, // Порт для терминала
            0, // Порт для терминала
            PreConnectedDevices, // Подключеные устройства
            0, // Выбор банка памяти
            ..PreImage.Length != 0 ? PreImage : Enumerable.Repeat((byte)0, 0x20 * DisplayColorsCount) // Видиопамять
        ];
        string jmp = $"{GetLabel()}:db {string.Join(",", args)}";

        return jmp;
    }
}
