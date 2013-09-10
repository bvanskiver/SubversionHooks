using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Configuration;

namespace pre_commit
{
    class Program
    {
        static bool error = false;

        static int Main(string[] args)
        {
            string repo = args[0]; // File system path to repository.
            string txn = args[1];

            try
            {
                // Get information about commit.
                string message = ExecuteProcess("svnlook.exe", string.Format("log -t {0} \"{1}\"", txn, repo)).Trim();
                string dirs = ExecuteProcess("svnlook.exe", string.Format("dirs-changed -t {0} \"{1}\"", txn, repo)).Trim();
                string files = ExecuteProcess("svnlook.exe", string.Format("changed -t {0} \"{1}\"", txn, repo)).Trim();
                string diff = ExecuteProcess("svnlook.exe", string.Format("diff -t {0} \"{1}\"", txn, repo)).Trim();

                // Make sure the message contains some text.
                if (message.Length == 0)
                    Error("Commit message is required.");

                // Check for invalid files.
                Regex regex = new Regex(ConfigurationManager.AppSettings["InvalidFileRegex"], RegexOptions.IgnoreCase | RegexOptions.Multiline);
                MatchCollection matches = regex.Matches(files.Replace("\r\n", "\n"));
                foreach (Match match in matches)
                {
                    Error(match.Value.Trim().Substring(4) + " not allowed in repository.");
                }

                // Check for merge conflicts.
                regex = new Regex(@"^\+(<{7} \.|={7}$|>{7} \.)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (regex.IsMatch(diff))
                    Error("Unresolved merge conflict detected.");

                return error ? 1 : 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        static void Error(string message)
        {
            Console.Error.WriteLine("** " + message);
            error = true;
        }

        static string ExecuteProcess(string fileName, string arguments)
        {
            string output;

            // Set up the process.
            ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            // Execute the process.
            using (Process p = Process.Start(psi))
            {
                p.WaitForExit(15000);

                // Get the output.
                output = p.StandardOutput.ReadToEnd();
            }

            return output;
        }
    }
}