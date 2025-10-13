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
            Components components = GetComponents(line);

            if (components.Op == "db")
                length += GetDbLength(components.Args);
            else
                length += GetCommandLength(components);
        }

        return length;
    }
}
