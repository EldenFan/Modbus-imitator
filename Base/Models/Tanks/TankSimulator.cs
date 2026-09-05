namespace Base.Models.Tanks
{
    public class TankSimulator
    {
        private readonly Tank tank;

        private bool isFlowing;
        private bool isDrying;

        private TankSimulator? receiver;

        public TankSimulator(ushort capacity, ushort flow)
        {
            tank = new Tank
            {
                Capacity = capacity,
                Flow = flow
            };
        }

        public Tank Tank => tank;

        public bool IsFlowing => isFlowing;
        public bool IsDrying => isDrying;

        public void StartFilling()
        {
            if (!tank.IsFull)
            {
                isFlowing = true;
                isDrying = false;
            }
        }

        public void StartDraining()
        {
            if (!tank.IsEmpty)
            {
                isDrying = true;
                isFlowing = false;
            }
        }

        public void Update()
        {
            if (isFlowing)
            {
                tank.AddWater();

                if (tank.IsFull)
                    isFlowing = false;
            }

            if (isDrying)
            {
                var amount = tank.RemoveWater();

                receiver?.Tank.AddWater(amount);

                if (tank.IsEmpty)
                    isDrying = false;
            }
        }

        public void SetReceiver(TankSimulator receiver)
        {
            this.receiver = receiver;
        }
    }
}
