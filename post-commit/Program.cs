using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Net.Mail;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;

namespace post_commit
{
    delegate void Delegate(string repo, string rev);

    class Program
    {
        static int Main(string[] args)
        {
            string repo = args[0]; // File system path to repository.
            string rev = args[1];

            // Get information about commit.
            string files = ExecuteProcess("svnlook.exe", string.Format("changed -r {0} \"{1}\"", rev, repo)).Trim();
            string author = ExecuteProcess("svnlook.exe", string.Format("author -r {0} \"{1}\"", rev, repo)).Trim();
            string log = ExecuteProcess("svnlook.exe", string.Format("log -r {0} \"{1}\"", rev, repo)).Trim();

            // Send a commit email.
            try
            {
                string email = Resources.Changeset_Email;

                email = email.Replace("${revision-url}", string.Format(ConfigurationManager.AppSettings["RevisionUrl"], rev));
                email = email.Replace("${revision}", rev);
                email = email.Replace("${author}", author);
                email = email.Replace("${message}", System.Web.HttpUtility.HtmlEncode(log).Replace("\n", "<br/>"));
                email = email.Replace("${file-list}", files);

                MailMessage message = new MailMessage(ConfigurationManager.AppSettings["EmailFrom"], ConfigurationManager.AppSettings["EmailTo"]);
                message.Subject = string.Format("[svn commit {0}:{1}]", Path.GetFileName(repo), rev);
                message.Body = email;
                message.IsBodyHtml = true;

                SmtpClient client = new SmtpClient();
                client.Send(message);
            }
            catch (Exception ex)
            {
                MailMessage mail = new MailMessage(ConfigurationManager.AppSettings["EmailFrom"], ConfigurationManager.AppSettings["EmailTo"]);
                mail.Subject = "Post-Commit: " + ex.Message;
                mail.Body = ex.ToString();

                SmtpClient client = new SmtpClient();
                client.Send(mail);
            }

            return 0;
        }

        static string ExecuteProcess(string fileName, string arguments)
        {
            string output;

            // Set up the process.
            ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;

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