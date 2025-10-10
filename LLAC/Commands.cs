using System.Drawing;
using System.Linq.Expressions;
using System.Runtime.Versioning;

namespace LLAC;

public partial class LLAC
{
    private static bool TryAlias(string op, string[] args, out string[] fragment)
    {
        switch (op)
        {
            case "exit":
                fragment = ["hlt"];
                return true;
        }
        fragment = [];
        return false;
    }

    private static string[] WriteChar(string arg)
    {
        return [$"st {arg},{0x3C}"];
    }

    private static string[] WriteLine(string[] args, Func<string> label)
    {
        string l = label();
        return [
            $"ldi b,{args[0]}", // Сохраняем в b адрес текста
            $"ldi d,{0x3C}", // Сохраняем в d адрес терминала

            $"{l}:ld a,b", // Помещяем в a букву
            $"st a,d", // Отправляем букву в терминал
            $"inc b", // Увеличиваем адрес буквы
            $"test a", // Проверяем на ноль
            $"jnz {l}",
        ];
    }

    private string[] ReadChar(string[] args, Func<string> label)
    {
        string l = label();
        return [$"{l}:ld {args[0]},{0x3E}", $"test {args[0]}", $"jz {l}"];
    }

    private string[] Connect(string[] args)
    {
        connectedDevices = GetDevices(args);
        return [$"ldi a,{connectedDevices}", $"st a,{0x3E}"];
    }

    private byte GetDevices(string[] args)
    {
        args = [.. args.SelectMany(a => a.Split(" ").Select(a => a.Trim(',')))];
        byte devices = 0;
        if (args.Contains("display")) devices |= 1 << 4; // Устоновка экрана
        if (args.Contains("color")) devices |= 1 << 5; // Устоновка цветного режима экрана
        if (args.Contains("terminal")) devices |= 1 << 0; // Устоновка терминала
        if (args.Contains("counter")) devices |= 1 << 2; // Устоновка счетчика
        if (args.Contains("signed")) devices |= 1 << 3; // Устоновка знакового режима счетчика
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
                int r = pixel.R;
                int g = pixel.G;
                int b = pixel.B;

                int rBit = r > 127 ? 1 : 0;
                int bBit = b > 127 ? 1 : 0;

                if (g > 127)
                {
                    rBit = 0;
                    bBit = 0;
                }

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

    private static string[] String(string arg, string name)
    {
        return [
            $"{name} db {arg},0"
        ];
    }
}