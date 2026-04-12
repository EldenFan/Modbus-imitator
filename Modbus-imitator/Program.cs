using Modbus.Data;
using Modbus.Device;
using Modbus_imitator.Model;
using System.Net;
using System.Net.Sockets;

namespace ModbusTcpSimulator
{
    class Program
    {
        private static ModbusTcpSlave slave;
        private static TcpListener listener;
        private static bool isRunning = true;
        private static bool isFloating = false;
        private static Tank tank;
        private static readonly ManualResetEvent shutdownEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Console.WriteLine("=== Modbus TCP Сервер (симулятор устройства) ===");
            Console.WriteLine("Запуск сервера на 127.0.0.1:502...\n");
            Console.WriteLine("Нажмите Ctrl+C для остановки сервера\n");

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                isRunning = false;
                Console.WriteLine("\nОстановка сервера...");
                shutdownEvent.Set();
            };

            try
            {
                tank = new Tank()
                {
                    Capacity = 85
                };

                listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 502);
                slave = ModbusTcpSlave.CreateTcp(1, listener);
                slave.DataStore = DataStoreFactory.CreateDefaultDataStore();

                slave.ModbusSlaveRequestReceived += OnRequestReceived;
                slave.WriteComplete += OnWriteComplete;

                var updateThread = new Thread(UpdateDataSimulation)
                {
                    IsBackground = true
                };
                updateThread.Start();

                Task.Run(() => RunModbusServer());

                shutdownEvent.WaitOne();

                Console.WriteLine("Сервер остановлен");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        /// <summary>
        /// Запуск Modbus Server
        /// </summary>
        static void RunModbusServer()
        {
            try
            {
                slave.Listen();
                slave.DataStore.HoldingRegisters[1] = 0;
                slave.DataStore.CoilDiscretes[1] = false;
                slave.DataStore.CoilDiscretes[2] = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в работе сервера: {ex.Message}");
                shutdownEvent.Set();
            }
        }

        /// <summary>
        /// Фоновый поток для симуляции изменения данных
        /// </summary>
        static void UpdateDataSimulation()
        {
            while (isRunning)
            {
                try
                {
                    if (isFloating)
                    {
                        tank.NowVolume += 2;
                        slave.DataStore.HoldingRegisters[1] = tank.NowVolume;
                        slave.DataStore.CoilDiscretes[1] = tank.IsLow;
                        slave.DataStore.CoilDiscretes[2] = tank.IsHigh;

                        if (tank.IsFull)
                        {
                            isFloating = false;
                            slave.DataStore.CoilDiscretes[3] = false;
                        }
                    }
                    Thread.Sleep(5000);
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        Console.WriteLine($"Ошибка обновления данных: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Обработчик события получения запроса от клиента
        /// </summary>
        static void OnRequestReceived(object sender, ModbusSlaveRequestEventArgs e)
        {
            Console.WriteLine($"Получен запрос от клиента. " +
                $"Unit ID: {e.Message.SlaveAddress}, " +
                $"Function Code: {e.Message.FunctionCode}");
        }

        /// <summary>
        /// Обработчик события завершения записи данных
        /// </summary>
        static void OnWriteComplete(object sender, ModbusSlaveRequestEventArgs e)
        {
            Console.WriteLine($"Запись данных завершена. " +
                $"Function Code: {e.Message}");

            if (!isFloating)
            {
                isFloating = true;
                slave.DataStore.CoilDiscretes[3] = true;
            }
        }
    }
}