using ModbusImitator.Model;
using NModbus;

public class ModbusTestUnit
{
    #region Fields
    private Tank? receiver;

    private readonly byte _id;
    private readonly IModbusSlave _slave;
    private readonly Tank _tank;

    private readonly object _lock = new();

    private bool _isFlowing;
    private bool _isDrying;

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
    public ModbusTestUnit(byte unitId, IModbusSlave slave, ushort maxCapacity)
    {
        _id = unitId;
        _slave = slave ?? throw new ArgumentNullException(nameof(slave));

        _tank = new Tank
        {
            Capacity = maxCapacity,
            NowVolume = 0
        };

        InitializeDataStore();
    }

    public ModbusTestUnit(byte unitId, IModbusSlave slave, ushort maxCapacity, ushort flow) : this(unitId, slave, maxCapacity)
    {
        _tank.Flow = flow;
    }

    #endregion

    #region Public Properties

    public Tank Tank => _tank;

    #endregion

    #region Private Methods
    /// <summary>
    /// Инициализация начальных значений регистров и coil
    /// </summary>
    private void InitializeDataStore()
    {
        lock (_lock)
        {
            _slave.DataStore.HoldingRegisters.WritePoints(0, new ushort[]
            {
                _tank.NowVolume,
                _tank.Capacity,
                _tank.Flow
            });

            _slave.DataStore.CoilDiscretes.WritePoints(0, new bool[]
            {
                _tank.IsLow,   // Coil0
                _tank.IsHigh,  // Coil1
                false,         // Coil2 - Fill command
                false,         // Coil3 - Drain command
                _tank.IsFull   // Coil4
            });
        }
    }

    /// <summary>
    /// Синхронизация модели Tank с Modbus DataStore
    /// </summary>
    private void SyncDataStore()
    {
        _slave.DataStore.HoldingRegisters.WritePoints(0, new ushort[] { _tank.NowVolume, _tank.FillPercentage });

        _slave.DataStore.CoilDiscretes.WritePoints(0, new bool[]
        {
            _tank.IsLow,
            _tank.IsHigh,
            _isFlowing,
            _isDrying,
            _tank.IsFull
        });
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
            bool startFill = _slave.DataStore.CoilDiscretes.ReadPoints(2, 1)[0];
            bool startDrain = _slave.DataStore.CoilDiscretes.ReadPoints(3, 1)[0];

            if (startFill && !_tank.IsFull)
            {
                _isFlowing = true;
                _isDrying = false;
            }

            if (startDrain && !_tank.IsLow)
            {
                _isDrying = true;
                _isFlowing = false;
            }

            _slave.DataStore.CoilDiscretes.WritePoints(2, new bool[] {false, false});
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

            if (_isFlowing)
            {
                _tank.AddWater();

                if (_tank.IsFull)
                {
                    _isFlowing = false;
                }
            }

            if (_isDrying)
            {
                var flowingWater = _tank.RemoveWater();

                receiver?.AddWater(flowingWater);

                if (_tank.IsEmpty)
                {
                    _isDrying = false;
                }
            }

            SyncDataStore();
        }
    }

    /// <summary>
    /// Лог состояния устройства
    /// </summary>
    public void PrintStatus()
    {
        lock (_lock)
        {
            Console.WriteLine(
                $"[Slave {_id}] " +
                $"Volume: {_tank.NowVolume}/{_tank.Capacity}, " +
                $"Flow: {_tank.Flow}, " +
                $"Low: {_tank.IsLow}, " +
                $"High: {_tank.IsHigh}, " +
                $"Filling: {_isFlowing}, " +
                $"Draining: {_isDrying}"
            );
        }
    }

    /// <summary>
    /// Установление приемного бака
    /// </summary>
    /// <param name="receiver"></param>
    public void SetReceiver(Tank receiver)
    {
        this.receiver = receiver;
    }
    #endregion
}