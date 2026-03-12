using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMesh.Core.Execution
{
    public class TestCaseRunner
    {
        public async Task<bool> RunTestCaseAsync(
        string workingDirectory,
        string input,
        string expectedOutput)
        {
            Process process = new Process();
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = "java";
            process.StartInfo.Arguments = "Main";
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();
            await process.StandardInput.WriteLineAsync(input);
            process.StandardInput.Close();

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output.Trim() == expectedOutput.Trim();
        }
    }
}
