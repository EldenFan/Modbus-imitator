namespace ModbusImitator.Model
{
    public class Tank
    {
        private readonly object lockObj = new();

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

                if (value < 0)
                {
                    nowVolume = 0;
                    return;
                }

                nowVolume = value;
            }
        }
        public ushort Flow { get; set; }
        public ushort FillPercentage => (ushort)(Capacity == 0 ? 0 : (nowVolume / (double)Capacity) * 100);

        public bool IsHigh => nowVolume > 0 && (nowVolume / (double)Capacity >= 0.8);
        public bool IsLow => nowVolume == 0 || (nowVolume / (double)Capacity <= 0.2);
        public bool IsFull => Capacity == nowVolume;
        public bool IsEmpty => nowVolume == 0;

        /// <summary>
        /// Добавляет воду в резервуар. Увеличивает NowVolume на Flow, но не выше Capacity.
        /// </summary>
        public void AddWater()
        {
            lock (lockObj)
            {
                NowVolume += Flow;
            }
        }

        /// <summary>
        /// Добавляет указанное количество воды в резервуар. Увеличивает NowVolume на amount, но не выше Capacity.
        /// </summary>
        /// <param name="amount">Добавляемое количество воды</param>
        public void AddWater(ushort amount)
        {
            lock (lockObj)
            {
                NowVolume += amount;
            }
        }

        /// <summary>
        /// Метод по удалению воды из резервуара. Уменьшает NowVolume на Flow, но не ниже 0.
        /// </summary>
        /// <returns>Объем удаленной воды</returns>
        public ushort RemoveWater()
        {
            lock (lockObj)
            {
                var prevVolume = NowVolume;
                NowVolume -= Flow;
                return (ushort)(prevVolume - NowVolume);
            }
        }
    }
}
