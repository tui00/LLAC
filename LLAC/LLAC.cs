namespace LLAC;

public partial class LLAC(string file)
{
    public readonly string file = file;

    public int nextLabelId = 0;
    public int nextCmdAddr = 0;
    public int connectedDevices = 0;
    public int preConnectedDevices = 0;

    public string Convert()
    {
        string[] fragment = [.. file.Split("\n").Select(ConvertLine)];

        if (nextCmdAddr <= 0x3A && preConnectedDevices != 0)
        {
            int voidToPortsCount = 0x3A - nextCmdAddr;
            string voidToPorts = $"_voidLLAC db {string.Join(",", Enumerable.Repeat("0", voidToPortsCount))}";
            string jmpAndPorts = $"_portsLLAC db {string.Join(",", ["0", "0", "0", "0", $"{preConnectedDevices}", "0"])}";

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

    public string ConvertLine(string line)
    {
        line = RemoveComments(line);
        string[] words = [.. line.Split(' ')];
        if (words.Length == 0)
            return "";

        string op = words[0];
        string[] args = [.. string.Join(" ", words[1..]).Split(',').Select(a => a.Trim())];

        string[] fragment = [line];
        switch (op.ToLower())
        {
            // === Команды ===
            case "connect" when args.Length > 0 && args.Length <= 5: fragment = Connect(args); break;
            case "readchar" when args.Length == 1: fragment = ReadChar(args, GetLabel); break;
            case "writechar" when words[1..].Length != 0: fragment = WriteChar(string.Join(" ", words[1..])); break;
            case "writeline" when words[1..].Length != 0: fragment = WriteLine(args, GetLabel); break;
            case "string" when words[1..].Length != 0: fragment = String(string.Join(" ", words[2..]), args[0]); break;

            default:
                // === Полу-команды ===
                if (op.Equals("@connect", StringComparison.CurrentCultureIgnoreCase) && args.Length > 0 && args.Length <= 5)
                {
                    preConnectedDevices = GetDevices(args);
                    fragment = [];
                }

                // === Остальное ===
                if (TryAlias(op, args, out string[] aliasFragment))
                {
                    fragment = aliasFragment;
                }
                break;
        }

        nextCmdAddr += GetLength(fragment);

        // === Перепрыгивание через порты ===
        // 0xКонец портов -- Первый порт
        // 3 -- Код команды `jmp <адрес>`
        // 0x02 -- Размер команды `jmp <адрес>`
        // 0x38 = 0x3A - 0x02

        int displayActive = (preConnectedDevices & (1 << 4)) >> 4;
        int displayColorsCount = (((preConnectedDevices & (1 << 5)) >> 5) + 1) * displayActive;
        int freeAddr = 0x40;
        freeAddr += 0x20 * displayColorsCount; // 0x20 -- Размер буфера дисплея в одноцветном режиме

        if (nextCmdAddr > 0x38 && nextCmdAddr < freeAddr) // Если мы в зоне портов
        {
            nextCmdAddr -= GetLength(fragment);

            string jmp = $"_jmpLLAC db {string.Join(",", [.. Enumerable.Repeat(0, 0x38 - nextCmdAddr), 3, freeAddr, 0, 0, 0, 0, preConnectedDevices, 0, .. Enumerable.Repeat(0, freeAddr - 0x40)])}";

            fragment = [jmp, .. fragment];

            nextCmdAddr += GetLength(fragment);
        }

        return string.Join("\n", fragment);
    }

    private string GetLabel()
    {
        return "_labelLLAC" + nextLabelId++;
    }

    public static int GetLength(string[] fragment)
    {
        int length = 0;

        foreach (var line in fragment)
        {
            string[] words = [.. line.Split(' ')];
            if (words.Length == 0)
                continue;

            string op = words[0];
            string[] args = [.. string.Join(" ", words[1..]).Split(',').Select(a => a.Trim())];
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
            }
            if (args.Length >= 1)
            {
                string firstArg = args[0].Split(' ')[0];

                if (firstArg == "db")
                {
                    // Собираем все элементы после db
                    string dbContent = string.Join(" ", words[1..]).Trim()[3..].Trim(); // удаляем "db"
                    bool inString = false;

                    int count = 0;
                    var numberBuffer = "";

                    for (int i = 0; i < dbContent.Length; i++)
                    {
                        char c = dbContent[i];

                        if (c == '"')
                        {
                            inString = !inString;
                            continue;
                        }

                        if (inString)
                        {
                            if (c == '\\')
                            {
                                i++;
                            }
                            count++; // символ внутри строки
                        }
                        else
                        {
                            if (char.IsDigit(c) || c == '-' || c == '+')
                            {
                                numberBuffer += c;
                            }
                            else if (c == ',' || char.IsWhiteSpace(c))
                            {
                                if (!string.IsNullOrEmpty(numberBuffer))
                                {
                                    count++;
                                    numberBuffer = "";
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(numberBuffer)) count++;

                    length += count;
                }
                else if (firstArg is "equ" or "=")
                {
                    length += 0;
                }
            }
        }

        return length;
    }
}
