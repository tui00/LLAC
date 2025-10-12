using System.Text;

namespace LLAC;

public partial class Llac(string file)
{
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

        return string.Join("\n", codeLines).Replace("\n\n", "\n").Trim();
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
        if (i < 0) i = 0;
        return line[..i].Trim(' ');
    }

    public string ConvertLine(string line)
    {
        Components components = GetComponents(line);

        string[] fragment = [line];

        int argsCount = components.Args.Length;

        switch (components.Op)
        {
            // === Команды ===
            case "connect" when argsCount > 0 && argsCount <= 3: fragment = Connect(components); break;

            case "readchar" when argsCount == 1: fragment = ReadChar(components); break;
            case "writechar" when argsCount == 1: fragment = WriteChar(components); break;
            case "writeline" when argsCount == 1: fragment = WriteLine(components); break;

            case "drawimage" when argsCount == 1: fragment = DrawImage(components); break;
            case "cleardisp" when argsCount == 0: fragment = ClearDisplay(); break;

            case "prepare" when argsCount == 2: fragment = PrepareNum(components); break;
            case "writenum" when argsCount == 1: fragment = SetDigit(components); break;
            case "cleardigit" when argsCount == 0: fragment = ["ldi a,0", $"st a,{0x3A}", $"st a,{0x3B}"]; break;

            default:
                // === Полу-команды ===
                if (TryHalfCommand(components, out string[] halfCommandFragment)) fragment = halfCommandFragment;

                // === Алиасы ===
                if (TryAlias(components, out string[] aliasFragment)) fragment = aliasFragment;
                break;
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

    private string GetLabel()
    {
        return "_labelLLAC" + nextLabelId++;
    }

    private static Components GetComponents(string line)
    {
        string? label = null;
        string op = "";
        string[] args = [];

        line = RemoveComments(line);

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

    public static byte GetLength(string[] fragment)
    {
        byte length = 0;

        foreach (var line in fragment)
        {
            Components components = GetComponents(line);

            string op = components.Op;
            string[] args = components.Args;

            bool arg1reg = args.Length > 0 && (args[0] is "a" or "b" or "c" or "d");
            bool arg2reg = args.Length > 1 && (args[1] is "a" or "b" or "c" or "d");
            switch (op)
            {
                case "nop":
                case "hlt":
                case "inc":
                case "dec":
                case "test":
                case "neg":
                case "not":
                case "ld" when arg2reg:
                case "st" when arg2reg:
                case "jmp" when arg1reg:
                case "jz" when arg1reg:
                case "js" when arg1reg:
                case "jc" when arg1reg:
                case "jo" when arg1reg:
                case "jnz" when arg1reg:
                case "jns" when arg1reg:
                case "jnc" when arg1reg:
                case "jno" when arg1reg:
                case "clr":
                case "mov":
                case "and":
                case "or":
                case "xor":
                case "add":
                case "adc":
                case "sub":
                case "sbb":
                case "rnd":
                case "shl":
                case "shr":
                case "sar":
                case "rcl":
                case "rcr":
                    length += 1;
                    break;

                case "jmp" when !arg1reg:
                case "jz" when !arg1reg:
                case "js" when !arg1reg:
                case "jc" when !arg1reg:
                case "jo" when !arg1reg:
                case "jnz" when !arg1reg:
                case "jns" when !arg1reg:
                case "jnc" when !arg1reg:
                case "jno" when !arg1reg:
                case "ld" when !arg2reg:
                case "st" when !arg2reg:
                case "ldi":
                    length += 2;
                    break;

                case "db":
                    foreach (var arg in args)
                        length += (byte)(arg.StartsWith('"') ? arg.Replace("\\", "").Length - 2 : 1);

                    break;
            }
        }

        return length;
    }
}

public record Components(string? Label, string Op, string[] Args);
