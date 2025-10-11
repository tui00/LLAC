using System.Collections.Specialized;
using System.Drawing;
using System.Runtime.Versioning;

namespace LLAC;

public partial class Llac
{
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

    private string[] ReadChar(Components components)
    {
        string label = GetLabel();
        return [$"{label}:ld {components.Args[0]},{0x3E}", $"test {components.Args[0]}", $"jz {label}"];
    }

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

    private string[] Connect(Components components)
    {
        connectedDevices = GetDevices(components.Args);
        return [$"ldi a,{connectedDevices}", $"st a,{0x3E}"];
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

    [SupportedOSPlatform("windows")]
    private byte[] GetImage(string path)
    {
        using var img = new Bitmap(path);
        int width = img.Width;
        int height = img.Height;

        if (width != 16 || height != 16)
            throw new ArgumentException("Изображение должно быть 16x16.");

        var redBytes = new List<byte>();
        var blueBytes = new List<byte>();

        for (int y = 0; y < height; y++)
        {
            int redByte = 0;
            int blueByte = 0;
            int bitCount = 0;

            for (int x = 0; x < width; x++)
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
        if ((connectedDevices & 1 << 5) == 1 << 5) // Если доступен синий
        {
            result.AddRange(blueBytes);
        }

        return [.. result];
    }

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
            $"{components.Args[0]}:db {string.Join(',', GetImage(components.Args[1]))}",
            $"{components.Args[0]}_length equ $-{components.Args[0]}"
        ];
    }
}