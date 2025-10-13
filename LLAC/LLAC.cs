using System.Text;

namespace LLAC;

public partial class Llac(string file)
{
    private static readonly ((string name, bool[] argsAreReg)[] commands, int length)[] asmCommands = [([
        ("nop", []),
        ("hlt", []),
        ("inc", [true]),
        ("dec", [true]),
        ("add", [true, true]),
        ("adc", [true, true]),
        ("sub", [true, true]),
        ("sbb", [true, true]),
        ("neg", [true]),
        ("not", [true]),
        ("and", [true, true]),
        ("or", [true, true]),
        ("xor", [true, true]),
        ("mov", [true, true]),
        ("clr", [true]),
        ("rnd", [true]),
        ("test", [true]),
        ("shl", [true]),
        ("shr", [true]),
        ("sar", [true]),
        ("rcl", [true]),
        ("rcr", [true]),
        ("ld", [true, true]),
        ("st", [true, true]),
        ("jmp", [true]),
        ("jz", [true]),
        ("js", [true]),
        ("jc", [true]),
        ("jo", [true]),
        ("jnz", [true]),
        ("jns", [true]),
        ("jnc", [true]),
        ("jno", [true]),
    ], 1),
    ([
        ("jmp", [false]),
        ("jz", [false]),
        ("js", [false]),
        ("jc", [false]),
        ("jo", [false]),
        ("jnz", [false]),
        ("jns", [false]),
        ("jnc", [false]),
        ("jno", [false]),
        ("ld", [true, false]),
        ("st", [true, false]),
        ("ldi", [true, false])
    ],2), ([("db", [])], -1)];

    public readonly string file = file;

    private int nextLabelId = 0;
    private byte nextCmdAddr = 0;

    private byte DisplayColorsCount
    {
        get
        {
            byte displayActive = (byte)((preConnectedDevices & 1 << 4) >> 4);
            byte displayColorsCount = (byte)((((preConnectedDevices & (1 << 5)) >> 5) + 1) * displayActive);
            return displayColorsCount;
        }
    }

    public string Convert()
    {
        string[] codeLines = [.. file.Split("\n").Select(ConvertLine)];

        if (nextCmdAddr <= 0x3A && (preConnectedDevices != 0))
        {
            codeLines = [.. codeLines, JumpOverPorts()];
        }
        for (int i = 0; i < codeLines.Length; i++)
        {
            Components components = GetComponents(codeLines[i]);
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
            Components components = GetComponents(line);
            if (components.Op == "" && components.Label != null) return (string)components;

            string[] fragment;

            if (llacCommands.TryGetValue(components.Op, out var result))
            {
                if (!result.condition(components.Args.Length, components))
                    throw new ArgumentException($"Invalid args for \"{components.Op}\"");
                else
                    fragment = result.handler(components, this);
            }
            else
            {
                fragment = [(string)components];
            }
            nextCmdAddr += GetLength(fragment);

            byte freeAddr = (byte)(0x40 + 0x20 * DisplayColorsCount);

            if (nextCmdAddr > 0x38 && nextCmdAddr < freeAddr)
            {
                nextCmdAddr -= GetLength(fragment);
                fragment = [JumpOverPorts([3, freeAddr]), .. fragment];
                nextCmdAddr += GetLength(fragment);
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

    private static bool IsRegister(string arg) => arg is "a" or "b" or "c" or "d";

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

    private string GetLabel()
    {
        return "_labelLLAC" + nextLabelId++;
    }

    private string JumpOverPorts(byte[]? additionalCode = null)
    {
        additionalCode ??= [];
        string jmp = $"{GetLabel()}:db {string.Join(",", [
                .. Enumerable.Repeat(0, 0x3A - additionalCode.Length - nextCmdAddr), // Нули
                .. additionalCode, // Дополнительный код
                0, // Порт для счетчика 1
                0, // Порт для счетчика 2
                0, // Порт для терминала 1
                0, // Порт для терминала 2
                preConnectedDevices, // Подключеные устройства
                0, // Выбор банка памяти
                ..(preImage.Length != 0 ? preImage : Enumerable.Repeat((byte)0, 0x20 * DisplayColorsCount)) // Видиопамять
        ])}";

        return jmp;
    }

    private static Components GetComponents(string line)
    {
        string? label = null;
        string op = "";
        string[] args = [];

        if (line.Split(' ')[0].Contains(':'))
        {
            int labelEnd = line.Index().First((e) => e.Item == ':').Index;
            label = line[..labelEnd];
            line = line[(labelEnd + 1)..].Trim();
        }

        op = line.Split(' ')[0].Trim();

        string content = string.Join(' ', line.Split(' ')[1..]).Trim(); // Берем все после op
        StringBuilder curArg = new(); // Текущий аргумент, который составляется
        bool inString = false; // Находится ли символ в строке
        for (int i = 0; i < content.Length; i++)
        {
            char ch = content[i];
            if (ch == '"') inString = !inString;
            if (!inString && ch == ',')
            {
                args = [.. args, curArg.ToString().Trim()];
                curArg.Clear();
                continue;
            }
            curArg.Append(ch);
        }
        if (curArg.Length != 0)
            args = [.. args, curArg.ToString().Trim()];

        return new(label, op.ToLower(), args);
    }

    private static bool TryFindAsmCommand(Components components, out (string name, bool[] argsAreReg, byte length) command)
    {
        foreach (var (commands, length) in asmCommands)
        {
            foreach (var (name, argsAreReg) in commands)
            {
                if (name != components.Op) continue;
                if (components.Args.Length < argsAreReg.Length) continue;
                if (argsAreReg.Index().Any(el => IsRegister(components.Args[el.Index]) != el.Item)) continue;

                command = (name, argsAreReg, (byte)length);
                return true;
            }
        }
        command = default;
        return false;
    }
}
