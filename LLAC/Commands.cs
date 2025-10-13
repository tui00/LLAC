using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;

namespace LLAC;

public partial class Llac
{
    private byte connectedDevices = 0;
    private byte preConnectedDevices = 0;
    private byte[] preImage = [];

    private delegate string[] CommandHandler(Components components, Llac llac);
    private delegate bool Condition(int argsCount, Components components);
    private static readonly Dictionary<string, (Condition condition, CommandHandler handler)> llacCommands = new()
    {
        ["connect"] = ((a, c) => a >= 1 && a <= 3, Connect),
        ["readchar"] = ((a, c) => a == 1, ReadChar),
        ["writechar"] = ((a, c) => a == 1, WriteChar),
        ["writeline"] = ((a, c) => a == 1, WriteLine),
        ["drawimage"] = ((a, c) => a == 1, DrawImage),
        ["cleardisp"] = ((a, c) => a == 0, ClearDisplay),
        ["writenum"] = ((a, c) => a == 1, WriteNum),
        ["prepare"] = ((a, c) => a == 2, Prepare),
        ["cleardigit"] = ((a, c) => a == 0, (_, _) => ["ldi a,0", $"st a,{0x3A}", $"st a,{0x3B}"]),

        ["exit"] = ((a, c) => a == 0, (_, _) => ["hlt"]),
        ["string"] = ((a, c) => a == 2, (c, _) => [$"{c.Args[0]}:db {string.Join(',', c.Args[1..])},0"]),
        ["image"] = ((a, c) => a == 2 && File.Exists(c.Args[1]), (c, l) => [$"{c.Args[0]}:db {string.Join(',', GetImage(c.Args[1], (l.connectedDevices & 1 << 5) == 1 << 5))}", $"{c.Args[0]}_length equ $-{c.Args[0]}"]),

        ["@image"] = ((a, c) => a == 1 && File.Exists(c.Args[0]), PreImage),
        ["@connect"] = ((a, c) => a > 0 && a <= 3, PreConnect),
    };

    private static string[] PreImage(Components components, Llac llac)
    {
        llac.preImage = GetImage(components.Args[0], llac.DisplayColorsCount == 2);
        return [];
    }

    private static string[] PreConnect(Components components, Llac llac)
    {
        llac.preConnectedDevices = llac.connectedDevices = GetDevices(components.Args);
        return [];
    }

    #region Терминал
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
    #endregion

    #region Клавиатура
    private static string[] ReadChar(Components components, Llac llac)
    {
        string label = llac.GetLabel();
        return [$"{label}:ld {components.Args[0]},{0x3E}", $"test {components.Args[0]}", $"jz {label}"];
    }
    #endregion

    #region Дисплей
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
    #endregion

    #region Счетчик
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
        string arg = components.Args[1];
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
    #endregion

    #region Подключение устройств
    private static string[] Connect(Components components, Llac llac)
    {
        byte devices = GetDevices(components.Args);
        llac.connectedDevices = devices;
        return [$"ldi a,{devices}", $"st a,{0x3E}"];
    }

    private static byte GetDevices(string[] args)
    {
        byte devices = 0;
        if (args.Contains("disp")) devices |= 1 << 4; // Установка экрана
        else if (args.Contains("coldisp")) devices |= 3 << 4; // Установка цветного режима экрана
        if (args.Contains("term")) devices |= 1 << 0; // Установка терминала
        if (args.Contains("digit")) devices |= 1 << 2; // Установка счетчика
        else if (args.Contains("signdigit")) devices |= 3 << 2; // Установка знакового режима счетчика
        return devices;
    }
    #endregion

    #region Утилиты
    private static byte[] GetImage(string path, bool useBlue)
    {
        Image<Rgba32> img = Image.Load<Rgba32>(path);
        try
        {
            if (img.Width > 16 || img.Height > 16) img = GetPixelArt(img);
            if (img.Width < 16 || img.Height < 16) throw new ArgumentException("The image must be larger or equal to 16x16");
            img.Save("_tmpLLACimg.bmp", new BmpEncoder());

            var redBytes = new List<byte>();
            var blueBytes = new List<byte>();

            int redByte = 0, blueByte = 0;
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    Rgba32 pixel = img[x, y];
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

            return useBlue ? [.. redBytes, .. blueBytes] : redBytes.ToArray();
        }
        finally
        {
            img.Dispose();
        }
    }

    private static Image<Rgba32> GetPixelArt(Image<Rgba32> img)
    {
        Image<Rgba32> art = new(16, 16, new Rgba32(0, 0, 0, 0));
        int cellWidth = img.Width / 16;
        int cellHeight = img.Height / 16;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int centerX = Math.Min(x * cellWidth + cellWidth / 2, img.Width - 1);
                int centerY = Math.Min(y * cellHeight + cellHeight / 2, img.Height - 1);
                art[x, y] = img[centerX, centerY];
            }
        }
        return art;
    }
    #endregion
}