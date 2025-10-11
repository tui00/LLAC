using System.Runtime.Versioning;

namespace LLAC;

public partial class LLAC(string file)
{
    public readonly string file = file;

    public int nextLabelId = 0;
    public byte nextCmdAddr = 0;
    public byte connectedDevices = 0;
    public byte preConnectedDevices = 0;
    public byte[] preImage = [];

    [SupportedOSPlatform("windows")]
    public string Convert()
    {
        string[] fragment = [.. file.Split("\n").Select(ConvertLine)];

        if (nextCmdAddr <= 0x3A && (preConnectedDevices != 0 || preImage.Length != 0))
        {
            int voidToPortsCount = 0x3A - nextCmdAddr;
            string voidToPorts = $"_voidLLAC db {string.Join(",", Enumerable.Repeat("0", voidToPortsCount))}";
            string jmpAndPorts = $"_portsLLAC db {string.Join(",", [0, 0, 0, 0, preConnectedDevices, 0, .. preImage])}";

            fragment = [.. fragment, voidToPortsCount != 0 ? voidToPorts : "", jmpAndPorts]; // Собираем все в месте
        }

        return string.Join("\n", fragment).Replace("\n\n", "\n").Trim();
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

    [SupportedOSPlatform("windows")]
    public string ConvertLine(string line)
    {
        Components components = GetComponents(line);

        string[] fragment = [line];

        int displayActive = (preConnectedDevices & (1 << 4)) >> 4;
        int displayColorsCount = (((preConnectedDevices & (1 << 5)) >> 5) + 1) * displayActive;

        int argsCount = components.Args.Length;

        switch (components.Op)
        {
            // === Команды ===
            case "connect" when argsCount > 0 && argsCount <= 3: fragment = Connect(components); break;
            case "readchar" when argsCount == 1: fragment = ReadChar(components, GetLabel); break;
            case "writechar" when argsCount == 1: fragment = WriteChar(components); break;
            case "writeline" when argsCount == 1: fragment = WriteLine(components, GetLabel); break;
            case "string" when argsCount == 2: fragment = String(components); break;
            case "drawimage" when argsCount == 1: fragment = DrawImage(components, GetLabel); break;
            case "image" when argsCount == 2 && File.Exists(components.Args[1]): fragment = Image(components); break;

            default:
                // === Полу-команды ===
                if (components.Op.Equals("@connect", StringComparison.CurrentCultureIgnoreCase) && argsCount > 0 && argsCount <= 3)
                {
                    connectedDevices = preConnectedDevices = GetDevices(components.Args);
                    fragment = [];
                }
                if (components.Op.Equals("@image", StringComparison.CurrentCultureIgnoreCase) && argsCount == 1 && File.Exists(components.Args[0]))
                {
                    preImage = GetImage(components.Args[0]);
                    fragment = [];
                }

                // === Остальное ===
                if (TryAlias(components, out string[] aliasFragment))
                {
                    fragment = aliasFragment;
                }
                break;
        }

        nextCmdAddr += GetLength(fragment);

        // === Перепрыгивание через порты ===
        // 0x40 -- Конец портов
        // 3 -- Код команды `jmp <адрес>`
        // 0x02 -- Размер команды `jmp <адрес>`
        // 0x38 = 0x3A - 0x02

        int freeAddr = 0x40;
        freeAddr += 0x20 * displayColorsCount; // 0x20 -- Размер буфера дисплея в одноцветном режиме

        if (nextCmdAddr > 0x38 && nextCmdAddr < freeAddr) // Если мы в зоне портов
        {
            nextCmdAddr -= GetLength(fragment);

            string jmp = $"_jmpLLAC:db {string.Join(",", [..
                Enumerable.Repeat(0, 0x38 - nextCmdAddr), // Нули до адреса 0x38
                3, freeAddr, // jmp на свободный адресс
                0, // Порт для счетчика
                0, // Порт для счетчика
                0, // Порт для терминала
                0, // Порт для терминала
                preConnectedDevices, // 0x3E
                0, // Выбор банка памяти
                ..(preImage.Length != 0 ? preImage : Enumerable.Repeat((byte)0, 0x20*displayColorsCount)) // Видиопамять
            ])}";

            fragment = [jmp, .. fragment];

            nextCmdAddr += GetLength(fragment);
        }

        for (int i = 0; i < fragment.Length; i++)
        {
            components = GetComponents(fragment[i]);
            if (components.Op == "db")
            {
                fragment[i] = $"{components.Label} db {string.Join(',', components.Args)}";
            }
        }

        return string.Join("\n", fragment);
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
        string curArg = ""; // Текущий аргумент, который составляется
        bool inString = false; // Находится ли символ в строке
        for (int i = 0; i < content.Length; i++)
        {
            char ch = content[i];
            if (ch == '"') inString = !inString;
            if (!inString && ch == ',')
            {
                args = [.. args, curArg.Trim()];
                curArg = "";
                continue;
            }
            curArg += ch;
        }
        if (curArg != "")
            args = [.. args, curArg.Trim()];

        return new(label, op, args);
    }

    public static byte GetLength(string[] fragment)
    {
        byte length = 0;

        foreach (var line in fragment)
        {
            Components components = GetComponents(line);

            string? label = components.Label;
            string op = components.Op;
            string[] args = components.Args;

            int l = args.Length;
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
