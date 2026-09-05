using Base.Models.Tanks;
using NModbus;

namespace Modbus_imitator.ModbusSlave
{
    public class ModbusTestUnit
    {
        #region Fields

        private readonly byte id;
        private readonly IModbusSlave slave;
        private readonly TankSimulator tankSimulator;

        private readonly object _lock = new();

        /*
            Карта регистров/coil:

            Holding Registers:
            HR0 - Текущий объем
            HR1 - Процентный объем

            Coils:
            Coil0 - Low level
            Coil1 - High level
            Coil2 - Start filling
            Coil3 - Start draining
            Coil4 - Full
        */
        #endregion

        #region Constructors
        public ModbusTestUnit(byte unitId, IModbusSlave slave, TankSimulator tankSimulator)
        {
            id = unitId;
            this.slave = slave;
            this.tankSimulator = tankSimulator;

            InitializeDataStore();
        }

        #endregion

        #region Public Properties

        public Tank Tank => tankSimulator.Tank;

        public TankSimulator TankController => tankSimulator;

        #endregion

        #region Private Methods
        /// <summary>
        /// Инициализация начальных значений регистров и coil
        /// </summary>
        private void InitializeDataStore()
        {
            lock (_lock)
            {
                slave.DataStore.HoldingRegisters.WritePoints(0,
                [
                    Tank.NowVolume,
                    Tank.Capacity,
                ]);

                slave.DataStore.CoilDiscretes.WritePoints(0,
                [
                    Tank.IsLow,   // Coil0
                    Tank.IsHigh,  // Coil1
                    false,         // Coil2
                    false,         // Coil3
                    Tank.IsFull   // Coil4
                ]);
            }
        }

        /// <summary>
        /// Синхронизация модели Tank с Modbus DataStore
        /// </summary>
        private void SyncDataStore()
        {
            slave.DataStore.HoldingRegisters.WritePoints(0, [Tank.NowVolume, Tank.FillPercentage]);

            slave.DataStore.CoilDiscretes.WritePoints(0,
            [
                Tank.IsLow,
                Tank.IsHigh,
                tankSimulator.IsFlowing,
                tankSimulator.IsDrying,
                Tank.IsFull
            ]);
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Вызывается внешним кодом после записи управляющих coil
        /// </summary>
        public void ProcessControlCommands()
        {
            lock (_lock)
            {
                bool startFill = slave.DataStore.CoilDiscretes.ReadPoints(2, 1)[0];
                bool startDrain = slave.DataStore.CoilDiscretes.ReadPoints(3, 1)[0];

                if (startFill)
                {
                    tankSimulator.StartFilling();
                }

                if (startDrain)
                {
                    tankSimulator.StartDraining();
                }

                slave.DataStore.CoilDiscretes.WritePoints(2, [false, false]);
            }
        }

        /// <summary>
        /// Обновление состояния резервуара
        /// </summary>
        public void Update()
        {
            lock (_lock)
            {
                ProcessControlCommands();

                tankSimulator.Update();

                SyncDataStore();
            }
        }

        /// <summary>
        /// Лог состояния устройства
        /// </summary>
        public string PrintStatus()
        {
            lock (_lock)
            {
                return
                    $"[Slave {id}] " +
                    $"Volume: {Tank.NowVolume}/{Tank.Capacity}, " +
                    $"Flow: {Tank.Flow}, " +
                    $"Low: {Tank.IsLow}, " +
                    $"High: {Tank.IsHigh}, " +
                    $"Filling: {tankSimulator.IsFlowing}, " +
                    $"Draining: {tankSimulator.IsDrying}";
            }
        }

        /// <summary>
        /// Установление приемного бака
        /// </summary>
        /// <param name="receiver"></param>
        public void SetReceiver(ModbusTestUnit receiver)
        {
            tankSimulator.SetReceiver(receiver.TankController);
        }
        #endregion
    }
}