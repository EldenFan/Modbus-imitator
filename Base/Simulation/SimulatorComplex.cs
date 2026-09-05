using Base.Models.Tanks;

namespace Base.Simulation
{
    public class SimulatorComplex
    {
        private readonly ushort[] capacities = [5, 20, 15, 40];
        private readonly ushort[] flows = [1, 2, 3];

        private readonly List<TankSimulator> tankSimulators = [];

        public SimulatorComplex()
        {
            for (int i = 0; i < capacities.Length - 1; i++)
            {
                var capacity = capacities[i];
                var flow = flows[i];
                var tankSimulator = new TankSimulator(capacity, flow);
                tankSimulators.Add(tankSimulator);
            }

            var lastTestUnit = new TankSimulator(capacities[3], 0);
            tankSimulators.Add(lastTestUnit);

            foreach (var unit in tankSimulators)
            {
                unit.SetReceiver(lastTestUnit);
            }
        }

        public IReadOnlyList<TankSimulator> TankSimulators => tankSimulators.AsReadOnly();
    }
}
