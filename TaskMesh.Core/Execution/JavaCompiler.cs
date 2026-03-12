using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMesh.Core.Execution
{
    public class JavaCompiler
    {
        public bool IsJavaInstalled()
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "java";
                process.StartInfo.Arguments = "-version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
        public async Task<string?> CompileAsync(string code, string workingDirectory)
        {
            string filePath = Path.Combine(workingDirectory, "Main.java");
            await File.WriteAllTextAsync(filePath, code);

            // Run javac
            Process process = new Process();
            process.StartInfo.FileName = "javac";
            process.StartInfo.Arguments = "Main.java";
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            string errors = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Check result
            if (process.ExitCode == 0) return null;
            else return errors;
        }
    }
}
