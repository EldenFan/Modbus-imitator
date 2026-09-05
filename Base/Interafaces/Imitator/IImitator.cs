using Base.Simulation;

namespace Base.Interafaces.Imitator
{
    public interface IImitator
    {
        void Start(SimulatorComplex simulator);

        Task RunAsync(CancellationToken token);
    }
}
