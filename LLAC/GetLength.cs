namespace LLAC;

public partial class Llac
{
    private static byte GetDbLength(string[] args)
    {
        byte length = 0;
        foreach (var arg in args)
        {
            length += (byte)(arg.StartsWith('"') ? arg.Replace("\\", "").Length - 2 : 1);
        }
        return length;
    }

    private static byte GetCommandLength(string op, string[] args)
    {
        bool arg1reg = args.Length > 0 && IsRegister(args[0]);
        bool arg2reg = args.Length > 1 && IsRegister(args[1]);

        foreach (var (cmds, len) in asmCommands)
        {
            foreach (var cmd in cmds)
            {
                if (cmd.cmd != op) continue;
                if (cmd.state[0] && !arg1reg) continue;
                if (cmd.state[1] && !arg2reg) continue;
                return (byte)len;
            }
        }
        return 0;
    }

    public static byte GetLength(string[] fragment)
    {
        byte length = 0;

        foreach (var line in fragment)
        {
            Components components = GetComponents(line);

            if (components.Op == "db")
                length += GetDbLength(components.Args);
            else
                length += GetCommandLength(components.Op, components.Args);
        }

        return length;
    }
}
