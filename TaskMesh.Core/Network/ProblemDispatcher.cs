using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMesh.Core.Messages;
using TaskMesh.Core.Models;

namespace TaskMesh.Core.Network
{
    public class ProblemDispatcher
    {
        private MasterServer _masterServer;
        private MessageSerializer _serializer = new MessageSerializer();
        private List<ProblemTask> _problems = new List<ProblemTask>();

        public ProblemDispatcher(MasterServer masterServer)
        {
            _masterServer = masterServer;
        }

        // Add a problem to the list
        public void AddProblem(ProblemTask problem)
        {
            if (!_problems.Any(p => p.ProblemId == problem.ProblemId))
                _problems.Add(problem);
        }

        // Send all problems to a specific worker
        public async Task DispatchSingleToWorkerAsync(string workerId, ProblemTask problem)
        {
            var stream = _masterServer.GetWorkerStream(workerId);
            if (stream == null) return;

            MessageSerializer serializer = new MessageSerializer();

            ProblemAssignment assignment = new ProblemAssignment
            {
                ProblemId = problem.ProblemId,
                ProblemName = problem.ProblemName,
                ProblemDescription = problem.ProblemDescription,
                InputTestCases = problem.InputTestCases,
                ExpectedOutputTestCases = problem.ExpectedOutputTestCases,
                TimeLimitSeconds = (int)problem.TimeLimitSeconds
            };

            byte[] bytes = serializer.WrapWithLength(serializer.Serialize(assignment));
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }
        public async Task DispatchToWorkerAsync(string workerId)
        {
            // Get worker stream from master
            // For each problem in _problems:
            //   Create ProblemAssignment from problem
            //   Serialize + wrap + send to worker stream
            var stream = _masterServer.GetWorkerStream(workerId);
            if (stream == null)
            {
                Console.WriteLine($"Worker {workerId} not found.");
                return;
            }
            foreach (var problem in _problems)
            {
                ProblemAssignment assignment = new ProblemAssignment
                {
                    ProblemId = problem.ProblemId,
                    ProblemName = problem.ProblemName,
                    ProblemDescription = problem.ProblemDescription,
                    InputTestCases = problem.InputTestCases,
                    ExpectedOutputTestCases = problem.ExpectedOutputTestCases,
                    TimeLimitSeconds = (int)problem.TimeLimitSeconds
                };
                byte[] bytes = _serializer.WrapWithLength(_serializer.Serialize(assignment));
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
        }
    }
}
