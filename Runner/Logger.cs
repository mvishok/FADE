using System;
using Spectre.Console;

namespace FSDE
{
    internal class Logger
    {
        public void Info(string message)
        {
            AnsiConsole.MarkupLine("[cyan bold]INFO[/] {0}", message);
        }

        public void Error(string message)
        {
            AnsiConsole.MarkupLine("[red bold]ERROR[/] {0}", message);
        }

        public void Warning(string message)
        {
            AnsiConsole.MarkupLine("[yellow bold]WARNING[/] {0}", message);
        }

        public void Success(string message)
        {
            AnsiConsole.MarkupLine("[green bold]SUCCESS[/] {0}", message);
        }
    }
}
