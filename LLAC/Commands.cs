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
        string[] fragment = char.IsLetter(arg[0]) ? [] : [$"ldi a,{arg[1]}"];
        return [.. fragment, $"st {(char.IsLetter(arg[0]) ? arg[0] : 'a')},{0x3C}"];
    }

    private static string[] WriteLine(string[] args, string label)
    {
        return [
            $"ldi b,{args[0]}", // Сохраняем в b адрес текста
            $"ldi c,{args[1]}", // Сохраняем в c размер текста
            $"ldi d,{0x3C}", // Сохраняем в d адрес терминала

            $"{label}:ld a,b", // Помещяем в a букву
            $"st a,d", // Отправляем букву в терминал
            $"inc b", // Увеличиваем адрес буквы
            $"dec c", // Уменьшаем счетчик длины оставшегося текста
            $"jnz {label}", // Если счетчик не 0 повторяем
        ];
    }

    private string[] ReadKey(string[] args, string label)
    {
        nextLoopId++;
        return [$"{label}:ld {args[0]},{0x3E}", $"test {args[0]}", $"jz {label}"];
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