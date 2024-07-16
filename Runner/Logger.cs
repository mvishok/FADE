using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSDE
{
    internal class Logger
    {
        string NL = Environment.NewLine;
        string NORMAL = Console.IsOutputRedirected ? "" : "\x1b[39m";
        string RED = Console.IsOutputRedirected ? "" : "\x1b[91m";
        string GREEN = Console.IsOutputRedirected ? "" : "\x1b[92m";
        string YELLOW = Console.IsOutputRedirected ? "" : "\x1b[93m";
        string BLUE = Console.IsOutputRedirected ? "" : "\x1b[94m";
        string MAGENTA = Console.IsOutputRedirected ? "" : "\x1b[95m";
        string CYAN = Console.IsOutputRedirected ? "" : "\x1b[96m";
        string GREY = Console.IsOutputRedirected ? "" : "\x1b[97m";
        string BOLD = Console.IsOutputRedirected ? "" : "\x1b[1m";
        string NOBOLD = Console.IsOutputRedirected ? "" : "\x1b[22m";
        string UNDERLINE = Console.IsOutputRedirected ? "" : "\x1b[4m";
        string NOUNDERLINE = Console.IsOutputRedirected ? "" : "\x1b[24m";
        string REVERSE = Console.IsOutputRedirected ? "" : "\x1b[7m";
        string NOREVERSE = Console.IsOutputRedirected ? "" : "\x1b[27m";

        public void Info(string message)
        {
            Console.WriteLine($"{CYAN}{BOLD}INFO {NOBOLD}{message}{NORMAL}");
        }

        public void Error(string message)
        {
            Console.WriteLine($"{RED}{BOLD}ERROR {NOBOLD}{message}{NORMAL}");
        }

        public void Warning(string message)
        {
            Console.WriteLine($"{YELLOW}{BOLD}WARNING {NOBOLD}{message}{NORMAL}");
        }

        public void Success(string message)
        {
            Console.WriteLine($"{GREEN}{BOLD}SUCCESS {NOBOLD}{message}{NORMAL}");
        }
    }
}
