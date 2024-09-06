using Spectre.Console;


namespace FADE
{
    internal class Logger
    {
        public void Info(string message) => AnsiConsole.MarkupLine($"[cyan bold]INFO[/] {message}");

        public void Error(string message) => AnsiConsole.MarkupLine($"[red bold]ERROR[/] {message}");

        public void Warning(string message) => AnsiConsole.MarkupLine($"[yellow bold]WARNING[/] {message}");

        public void Success(string message) => AnsiConsole.MarkupLine($"[green bold]SUCCESS[/] {message}");

    }
}
