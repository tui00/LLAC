namespace LLAC;

public static class Asm
{
    private static readonly ((string name, bool[] argsAreReg)[] commands, int length)[] asmCommands = [([
        ("nop", []),
        ("hlt", []),
        ("inc", [true]),
        ("dec", [true]),
        ("add", [true, true]),
        ("adc", [true, true]),
        ("sub", [true, true]),
        ("sbb", [true, true]),
        ("neg", [true]),
        ("not", [true]),
        ("and", [true, true]),
        ("or", [true, true]),
        ("xor", [true, true]),
        ("mov", [true, true]),
        ("clr", [true]),
        ("rnd", [true]),
        ("test", [true]),
        ("shl", [true]),
        ("shr", [true]),
        ("sar", [true]),
        ("rcl", [true]),
        ("rcr", [true]),
        ("ld", [true, true]),
        ("st", [true, true]),
        ("jmp", [true]),
        ("jz", [true]),
        ("js", [true]),
        ("jc", [true]),
        ("jo", [true]),
        ("jnz", [true]),
        ("jns", [true]),
        ("jnc", [true]),
        ("jno", [true]),
    ], 1),
    ([
        ("jmp", [false]),
        ("jz", [false]),
        ("js", [false]),
        ("jc", [false]),
        ("jo", [false]),
        ("jnz", [false]),
        ("jns", [false]),
        ("jnc", [false]),
        ("jno", [false]),
        ("ld", [true, false]),
        ("st", [true, false]),
        ("ldi", [true, false])
    ],2), ([("db", [])], -1)];

    private static byte GetDbLength(string[] args)
    {
        byte length = 0;
        foreach (var arg in args)
        {
            length += (byte)(arg.StartsWith('"') ? arg.Replace("\\", "").Length - 2 : 1);
        }
        return length;
    }

    private static byte GetCommandLength(Components components)
    {
        if (TryFindAsmCommand(components, out var command)) return command.length;
        throw new ArgumentException($"The command \"{components.Op}\" was not found or is being used incorrectly");
    }

    public static byte GetLength(string[] fragment)
    {
        byte length = 0;

        foreach (var line in fragment)
        {
            Components components = Components.Parse(line);

            if (components.Label != null && components.Op == "") continue;

            if (components.Op == "db")
                length += GetDbLength(components.Args);
            else
                length += GetCommandLength(components);
        }

        return length;
    }

    private static bool IsRegister(string arg) => arg is "a" or "b" or "c" or "d";

    private static bool TryFindAsmCommand(Components components, out (string name, bool[] argsAreReg, byte length) command)
    {
        foreach (var (commands, length) in asmCommands)
        {
            foreach (var (name, argsAreReg) in commands)
            {
                if (name != components.Op) continue;
                if (components.Args.Length < argsAreReg.Length) continue;
                if (argsAreReg.Index().Any(el => IsRegister(components.Args[el.Index]) != el.Item)) continue;

                command = (name, argsAreReg, (byte)length);
                return true;
            }
        }
        command = default;
        return false;
    }
}
