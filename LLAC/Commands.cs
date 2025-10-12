using System.Drawing;
using System.Runtime.Versioning;

namespace LLAC;

public partial class Llac
{
    private byte connectedDevices = 0;
    private byte preConnectedDevices = 0;
    private byte[] preImage = [];

    private static bool TryAlias(Components components, out string[] fragment)
    {
        switch (components.Op)
        {
            case "exit":
                fragment = ["hlt"];
                return true;
        }
        fragment = [];
        return false;
    }

    // === Терминал ===
    private static string[] WriteChar(Components components)
    {
        return [$"st {components.Args[0]},{0x3C}"];
    }

    private string[] WriteLine(Components components)
    {
        string label = GetLabel();
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
    private string[] ReadChar(Components components)
    {
        string label = GetLabel();
        return [$"{label}:ld {components.Args[0]},{0x3E}", $"test {components.Args[0]}", $"jz {label}"];
    }

    // === Дисплей ===
    private string[] DrawImage(Components components)
    {
        string label = GetLabel();
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

    private string[] ClearDisplay()
    {
        string label = GetLabel();
        return [
            $"ldi a,0",
            $"ldi d,{0x40}", // Сохраняем в d адрес дисплея
            $"ldi c,{DisplayColorsCount * 0x20}",
            $"{label}:st a,d", // Записываем
            $"inc d",
            $"dec c",
            $"jnz {label}"
        ];
    }

    // === Счетчик ===
    private static string[] SetDigit(Components components)
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

    private static string[] PrepareNum(Components components)
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
    private string[] Connect(Components components)
    {
        byte devices = GetDevices(components.Args);
        connectedDevices = devices;
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
    [SupportedOSPlatform("windows")]
    private (byte[] image, bool useBlue) GetImage(string path, bool useBlue, bool autoDetectUseBlue = false)
    {
        using var img = new Bitmap(path);

        var redBytes = new List<byte>();
        var blueBytes = new List<byte>();

        for (int y = 0; y < 16; y++)
        {
            int redByte = 0;
            int blueByte = 0;
            int bitCount = 0;

            for (int x = 0; x < 16; x++)
            {
                var pixel = img.GetPixel(x, y);

                int rBit = pixel.R > 127 ? 1 : 0;
                int bBit = pixel.B > 127 ? 1 : 0;

                redByte = (redByte << 1) | rBit;
                blueByte = (blueByte << 1) | bBit;
                bitCount++;

                if (bitCount == 8)
                {
                    redBytes.Add((byte)redByte);
                    blueBytes.Add((byte)blueByte);
                    redByte = blueByte = 0;
                    bitCount = 0;
                }
            }
        }

        var result = new List<byte>();
        result.AddRange(redBytes);
        useBlue |= autoDetectUseBlue && blueBytes.Any(b => b != 0);
        if (useBlue)
        {
            result.AddRange(blueBytes);
        }

        return ([.. result], useBlue);
    }

    // === Вспомогательные
    private static string[] String(Components components)
    {
        return [
            $"{components.Args[0]}:db {string.Join(',', components.Args[1..])},0"
        ];
    }

    [SupportedOSPlatform("windows")]
    private string[] Image(Components components)
    {
        return [
            $"{components.Args[0]}:db {string.Join(',', GetImage(components.Args[1], (connectedDevices & 1 << 5) == 1 << 5).image)}",
            $"{components.Args[0]}_length equ $-{components.Args[0]}"
        ];
    }
}