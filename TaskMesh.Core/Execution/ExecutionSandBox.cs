using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMesh.Core.Models;

namespace TaskMesh.Core.Execution
{
    public class ExecutionSandBox
    {

        private JavaCompiler _compiler = new JavaCompiler();
        private TestCaseRunner _runner = new TestCaseRunner();
        private string _workingDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Submissions");

        public async Task<JudgeResult> JudgeAsync(
            Guid problemId,
            string workerId,
            string code,
            List<string> inputs,
            List<string> expectedOutputs,
            int timeLimitSeconds)
        {
            if (!_compiler.IsJavaInstalled())
            {
                return new JudgeResult
                {
                    ProblemId = problemId,
                    WorkerId = workerId,
                    IsSuccess = false,
                    ErrorMessage = "Java is not installed on this worker."
                };
            }
            Directory.CreateDirectory(_workingDirectory);
            string? compilationError = await _compiler.CompileAsync(code, _workingDirectory);
            if (compilationError != null)
            {
                return new JudgeResult
                {
                    ProblemId = problemId,
                    WorkerId = workerId,
                    IsSuccess = false,
                    ErrorMessage = compilationError,
                    TotalTestCaseCount = inputs.Count
                };
            }

            int passCount = 0;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < inputs.Count; i++)
            {
                var cts = new CancellationTokenSource(timeLimitSeconds * 1000);
                bool passed = await Task.Run(
                    () => _runner.RunTestCaseAsync(_workingDirectory, inputs[i], expectedOutputs[i]),
                    cts.Token);
                if (passed) passCount++;
            }

            stopwatch.Stop();
            return new JudgeResult
            {
                ProblemId = problemId,
                WorkerId = workerId,
                IsSuccess = passCount == inputs.Count,
                TestCasePassCount = passCount,
                TotalTestCaseCount = inputs.Count,
                DurationSecond = stopwatch.Elapsed.TotalSeconds,
                SubmitCode = code
            };
            
        }
    }
}
