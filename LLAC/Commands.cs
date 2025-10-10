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

    private int GetDevices(string[] args)
    {
        args = [.. args.SelectMany(a => a.Split(" ").Select(a => a.Trim(',')))];
        byte devices = 0;
        if (args.Contains("display")) devices |= 0b00010000; // Устоновка экрана
        if (args.Contains("color")) devices |= 0b00100000; // Устоновка цветного режима экрана
        if (args.Contains("terminal")) devices |= 0b00000001; // Устоновка терминала
        if (args.Contains("counter")) devices |= 0b00000100; // Устоновка счетчика
        if (args.Contains("signed")) devices |= 0b00001000; // Устоновка знакового режима счетчика
        return devices;
    }

    private static string[] String(string arg, string name)
    {
        return [
            $"{name} db {arg},0"
        ];
    }
}