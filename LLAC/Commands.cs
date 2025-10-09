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
        string l2 = label();
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

    private static string[] Connect(string[] args)
    {
        byte devices = 0;
        if (args.Contains("display")) devices |= 0b00_01_0000; // Устоновка монохроного режима экрана
        if (args.Contains("coldisplay")) devices |= 0b00_11_0000; // Устоновка цветного режима экрана
        if (args.Contains("terminal")) devices |= 0b0000000_1; // Устоновка терминала
        if (args.Contains("digit")) devices |= 0b00000_01_0; // Устоновка беззнакового режима
        if (args.Contains("digitsign")) devices |= 0b00000_11_0; // Устоновка знакового режима
        return [$"ldi a,{devices}", $"st a,{0x3E}"];
    }
}