using Base.Interafaces.Imitator;
using Base.Interafaces.Log;
using Base.Simulation;
using Modbus_imitator.ModbusSlave;
using NModbus;
using System.Net;
using System.Net.Sockets;

namespace ModbusTcpSimulator
{
    public class ModbusServer : IImitator, ILogger
    {
        private readonly List<ModbusTestUnit> slaves = [];

        private TcpListener listener = null!;
        private IModbusSlaveNetwork slaveNetwork = null!;

        public void Start(SimulatorComplex simulator)
        {
            try
            {
                listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 502);
                listener.Start();

                var factory = new ModbusFactory();
                slaveNetwork = factory.CreateSlaveNetwork(listener);

                for (byte i = 0; i < simulator.TankSimulators.Count; i++)
                {
                    var slave = factory.CreateSlave((byte)(i + 1));

                    var testUnit = new ModbusTestUnit(slave.UnitId, slave, simulator.TankSimulators[i]);

                    slaves.Add(testUnit);

                    slaveNetwork.AddSlave(slave);

                    LogMessage?.Invoke($"Добавлен Slave ID={slave.UnitId} | Вместимость={testUnit.Tank.Capacity} | Скорость={testUnit.Tank.Flow}");
                }

                LogMessage?.Invoke("\nModbus успешно запущен.\n");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Критическая ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Фоновая симуляция обновления устройств
        /// </summary>
        public async Task RunAsync(CancellationToken token)
        {
            if (slaveNetwork == null)
            {
                LogMessage?.Invoke("Ошибка: Modbus-сервер не был запущен.");
                return;
            }

            Task.Run(() => slaveNetwork.ListenAsync(token));

            while (!token.IsCancellationRequested)
            {
                try
                {
                    foreach (var slave in slaves)
                    {
                        slave.Update();
                        LogMessage?.Invoke(slave.PrintStatus());
                    }

                    await Task.Delay(5000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke($"Ошибка симуляции: {ex.Message}");
                }
            }

            Stop();
        }

        /// <summary>
        /// Корректная остановка сервера
        /// </summary>
        public void Stop()
        {
            try
            {
                slaveNetwork?.Dispose();
                listener?.Stop();
            }
            catch
            {
                // Игнорируем ошибки при завершении
            }

            LogMessage?.Invoke("Сервер завершил работу.");
        }

        public event Action<string>? LogMessage;
    }
}