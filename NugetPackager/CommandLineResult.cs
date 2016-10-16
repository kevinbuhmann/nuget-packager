namespace NugetPackager
{
    public class CommandLineResult
    {
        public CommandLineResult(string workingDirectory, string command, string arguments, int? exitCode, string standardOutput, string standardError)
        {
            this.WorkingDirectory = workingDirectory;
            this.Command = command;
            this.Arguments = arguments;
            this.ExitCode = exitCode;
            this.StandardOutput = standardOutput;
            this.StandardError = standardError;
        }

        public string WorkingDirectory { get; }

        public string Command { get; }

        public string Arguments { get; }

        public int? ExitCode { get; }

        public string StandardOutput { get; }

        public string StandardError { get; }
    }
}
