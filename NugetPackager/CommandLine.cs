using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace NugetPackager
{
    public static class CommandLine
    {
        public static CommandLineResult Run(string workingDirectory, string command, string arguments, int timeout, TimeUnit timeUnit, bool throwIfNonzeroExitCode = true)
        {
            Console.WriteLine($"{workingDirectory}> {command} {arguments}");

            CommandLineResult result = null;
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = workingDirectory,
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                StringBuilder errorRecord = new StringBuilder();
                StringBuilder outputRecord = new StringBuilder();

                Thread errorReader = new Thread(() => RecordStream(process.StandardError, errorRecord));
                errorReader.IsBackground = true;

                Thread outputReader = new Thread(() => RecordStream(process.StandardOutput, outputRecord));
                outputReader.IsBackground = true;

                process.Start();
                outputReader.Start();
                errorReader.Start();

                int timeoutMilliseconds = timeout * (int)timeUnit;
                process.WaitForExit(timeoutMilliseconds);

                errorReader.Join();
                outputReader.Join();

                int? exitCode = process.HasExited ? process.ExitCode : (int?)null;

                if (process.HasExited == false)
                {
                    process.Kill();
                }

                result = new CommandLineResult(workingDirectory, command, arguments, exitCode, outputRecord.ToString(), errorRecord.ToString());

                if ((exitCode != 0 && throwIfNonzeroExitCode) || exitCode == null)
                {
                    throw new CommandLineException(result);
                }
            }

            return result;
        }

        private static void RecordStream(StreamReader stream, StringBuilder record)
        {
            string line = string.Empty;

            do
            {
                line = stream.ReadLine();

                if (!string.IsNullOrEmpty(line))
                {
                    record.AppendLine(line);
                }
            }
            while (line != null);
        }
    }
}
