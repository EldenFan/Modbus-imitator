using Base.Interafaces.Imitator;
using Base.Interafaces.Log;
using Base.Simulation;

namespace OpcUaImitator
{
    public class OpcServer : IImitator, ILogger
    {
        private const string EndpointUrl = "opc.tcp://127.0.0.1:4840/OpcUaImitator";

        public Task RunAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public void Start(SimulatorComplex simulator)
        {
            throw new NotImplementedException();
        }

        public event Action<string>? LogMessage;
    }
}
