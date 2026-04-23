using System.Text;

namespace Vortex.Bot.Utility;

public class CommandUtility
{
    public static List<string> ParseParameters(string str)
    {
        List<string> ret = [];
        var sb = new StringBuilder();
        bool instr = false;
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];

            if (c == '\\' && ++i < str.Length)
            {
                if (str[i] != '"' && str[i] != ' ' && str[i] != '\\')
                    sb.Append('\\');
                sb.Append(str[i]);
            }
            else if (c == '"')
            {
                instr = !instr;
                if (!instr)
                {
                    ret.Add(sb.ToString());
                    sb.Clear();
                }
                else if (sb.Length > 0)
                {
                    ret.Add(sb.ToString());
                    sb.Clear();
                }
            }
            else if (IsWhiteSpace(c) && !instr)
            {
                if (sb.Length > 0)
                {
                    ret.Add(sb.ToString());
                    sb.Clear();
                }
            }
            else
                sb.Append(c);
        }
        if (sb.Length > 0)
            ret.Add(sb.ToString());

        return ret;
    }

    private static bool IsWhiteSpace(char c)
    {
        return c == ' ' || c == '\t' || c == '\n';
    }

    public static Dictionary<string, string> ParseCommandLine(List<string> command)
    {
        Dictionary<string, string> args = [];
        for (int i = 0; i < command.Count; i++)
        {
            string cmd = command[i];
            if (cmd.StartsWith('-'))
            {
                string str = "";
                for (int j = i + 1; j < command.Count; j++)
                {
                    if (!command[j].StartsWith('-'))
                        str += " " + command[j];
                    else
                        break;
                }
                if (!string.IsNullOrEmpty(str.Trim()))
                    args[cmd] = str.Trim();
            }
        }
        return args;
    }
}
