namespace Modbus_imitator.Model
{
    public class Tank
    {
        private ushort nowVolume = 0;

        public ushort Capacity { get; set; }
        public ushort NowVolume
        {
            get => nowVolume;
            set
            {
                if (value > Capacity)
                {
                    nowVolume = Capacity;
                    return;
                }

                nowVolume = value;
            }
        }

        public bool IsHigh => nowVolume > 0 && (nowVolume / (double)Capacity >= 0.8);
        public bool IsLow => nowVolume == 0 ||(nowVolume / (double)Capacity <= 0.2);
        public bool IsFull => Capacity == nowVolume;
    }
}
