using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace LLAC;

public partial class Llac
{
    private byte connectedDevices = 0;
    private byte preConnectedDevices = 0;
    private byte[] preImage = [];

    private delegate string[] CommandHandler(Components components, Llac llac);
    private delegate bool Condition(int argsCount);
    private static readonly Dictionary<string, (Condition condition, CommandHandler handler)> commands = new()
    {
        ["connect"] = (a => a >= 1 && a <= 3, Connect),
        ["readchar"] = (a => a == 1, ReadChar),
        ["writechar"] = (a => a == 1, WriteChar),
        ["writeline"] = (a => a == 1, WriteLine),
        ["drawimage"] = (a => a == 1, DrawImage),
        ["cleardisp"] = (a => a == 0, ClearDisplay),
        ["writenum"] = (a => a == 1, WriteNum),
        ["prepare"] = (a => a == 2, Prepare),
        ["cleardigit"] = (a => a == 0, (_, _) => ["ldi a,0", $"st a,{0x3A}", $"st a,{0x3B}"])
    };

    private bool TryAlias(Components components, out string[] fragment)
    {
        switch (components.Op)
        {
            case "exit": fragment = ["hlt"]; return true;
            case "string": fragment = [$"{components.Args[0]}:db {string.Join(',', components.Args[1..])},0"]; return true;
            case "image":
                fragment = [
                    $"{components.Args[0]}:db {string.Join(',', GetImage(components.Args[1], (connectedDevices & 1 << 5) == 1 << 5))}",
                    $"{components.Args[0]}_length equ $-{components.Args[0]}"
                ];
                return true;
        }
        fragment = [];
        return false;
    }

    private bool TryHalfCommand(Components components, out string[] fragment)
    {
        fragment = [];
        int argsCount = components.Args.Length;
        switch (components.Op.ToLowerInvariant())
        {
            case "@connect" when argsCount > 0 && argsCount <= 3:
                connectedDevices = preConnectedDevices = GetDevices(components.Args);
                return true;
            case "@image" when argsCount == 1 && File.Exists(components.Args[0]):
                preImage = GetImage(components.Args[0], ((connectedDevices >> 5) & 1) == 1);
                return true;
        }
        return false;
    }

    // === Терминал ===
    private static string[] WriteChar(Components components, Llac _)
    {
        return [$"st {components.Args[0]},{0x3C}"];
    }

    private static string[] WriteLine(Components components, Llac llac)
    {
        string label = llac.GetLabel();
        return [
            $"ldi b,{components.Args[0]}", // Сохраняем в b адрес текста
            $"ldi d,{0x3C}", // Сохраняем в d адрес терминала

            $"{label}:ld a,b", // Помещяем в a букву
            $"st a,d", // Отправляем букву в терминал
            $"inc b", // Увеличиваем адрес буквы
            $"test a", // Проверяем на ноль
            $"jnz {label}",
        ];
    }

    // === Клавиатура ===
    private static string[] ReadChar(Components components, Llac llac)
    {
        string label = llac.GetLabel();
        return [$"{label}:ld {components.Args[0]},{0x3E}", $"test {components.Args[0]}", $"jz {label}"];
    }

    // === Дисплей ===
    private static string[] DrawImage(Components components, Llac llac)
    {
        string label = llac.GetLabel();
        return [
            $"ldi b,{components.Args[0]}", // Сохраняем в b адрес изо
            $"ldi c,{components.Args[0]}_length", // Сохраняем длину
            $"ldi d,{0x40}", // Сохраняем в d адрес дисплея

            $"{label}:ld a,b", // Считываем
            $"st a,d", // Записываем
            $"inc b", // Переходим к следущему адресу
            $"inc d",
            $"dec c",
            $"jnz {label}" // Если для вывода еще что-то осталось, переходим
        ];
    }

    private static string[] ClearDisplay(Components _, Llac llac)
    {
        string label = llac.GetLabel();
        return [
            $"ldi a,0",
            $"ldi d,{0x40}", // Сохраняем в d адрес дисплея
            $"ldi c,{llac.DisplayColorsCount * 0x20}",
            $"{label}:st a,d", // Записываем
            $"inc d",
            $"dec c",
            $"jnz {label}"
        ];
    }

    // === Счетчик ===
    private static string[] WriteNum(Components components, Llac _)
    {
        string arg = components.Args[0];
        string[] fragment = [];
        string upperReg = arg.Length == 3 ? arg.Split(':')[0] : "";
        string lowerReg = arg.Split(':')[arg.Length == 3 ? 1 : 0];
        if (upperReg != "")
            fragment = [.. fragment, $"st {upperReg},{0x3B}"];
        fragment = [.. fragment, $"st {lowerReg},{0x3A}"];
        return fragment;
    }

    private static string[] Prepare(Components components, Llac _)
    {
        string arg = components.Args[1].Trim();
        ushort baseValue = components.Args[0] switch
        {
            var s when s.Length == 3 && s.StartsWith('"') => s[1],
            var s when s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) => System.Convert.ToUInt16(s[2..], 16),
            var s when s.StartsWith("0b", StringComparison.OrdinalIgnoreCase) => System.Convert.ToUInt16(s[2..], 2),
            var s when s.StartsWith('0') && s.Length > 1 => System.Convert.ToUInt16(s[1..], 8),
            var s when s.StartsWith('-') => (ushort)short.Parse(s),
            var s => ushort.Parse(s)
        };
        return [$"ldi {arg.Split(':')[1]},{baseValue & 0xFF}", $"ldi {arg.Split(':')[0]},{baseValue >> 8}"];
    }

    // === Подключение устройств ===
    private static string[] Connect(Components components, Llac llac)
    {
        byte devices = GetDevices(components.Args);
        llac.connectedDevices = devices;
        return [$"ldi a,{devices}", $"st a,{0x3E}"];
    }

    private static byte GetDevices(string[] args)
    {
        byte devices = 0;
        if (args.Contains("disp")) devices |= 1 << 4; // Устоновка экрана
        else if (args.Contains("coldisp")) devices |= 3 << 4; // Устоновка цветного режима экрана
        if (args.Contains("term")) devices |= 1 << 0; // Устоновка терминала
        if (args.Contains("digit")) devices |= 1 << 2; // Устоновка счетчика
        else if (args.Contains("signdigit")) devices |= 3 << 2; // Устоновка знакового режима счетчика
        return devices;
    }

    // === Утилиты ===
    private static byte[] GetImage(string path, bool useBlue)
    {
        using var img = Image.Load<Rgba32>(path);

        var redBytes = new List<byte>();
        var blueBytes = new List<byte>();

        int redByte = 0, blueByte = 0;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                var pixel = img[x, y];
                int rBit = pixel.R >> 7;
                int bBit = pixel.B >> 7;

                redByte |= (rBit | (useBlue ? 0 : bBit)) << (7 - x % 8);
                blueByte |= bBit << (7 - x % 8);

                if ((x + 1) % 8 == 0)
                {
                    redBytes.Add((byte)redByte);
                    blueBytes.Add((byte)blueByte);
                    redByte = blueByte = 0;
                }
            }
        }

        return useBlue ? redBytes.Concat(blueBytes).ToArray() : redBytes.ToArray();
    }
}