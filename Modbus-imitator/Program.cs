using NModbus;
using System.Net;
using System.Net.Sockets;

namespace ModbusTcpSimulator
{
    class Program
    {
        private static readonly List<ModbusTestUnit> _slaves = new();

        private static readonly ushort[] Capacities = { 5, 20, 15, 40 };
        private static readonly ushort[] Flows = { 1, 2, 3,};

        private static TcpListener _listener = null!;
        private static IModbusSlaveNetwork _slaveNetwork = null!;
        private static readonly CancellationTokenSource _cts = new();

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Modbus TCP Сервер (симулятор устройства) ===");
            Console.WriteLine("Запуск сервера на 127.0.0.1:502...");
            Console.WriteLine("Нажмите Ctrl+C для остановки сервера\n");

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\nОстановка сервера...");
                _cts.Cancel();
            };

            try
            {
                InitializeServer();

                var listenTask = _slaveNetwork.ListenAsync(_cts.Token);
                var simulationTask = RunDataSimulationAsync(_cts.Token);

                await Task.WhenAll(listenTask, simulationTask);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Сервер остановлен.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка: {ex.Message}");
            }
            finally
            {
                Shutdown();
            }
        }

        /// <summary>
        /// Инициализация Modbus TCP сервера и слейвов
        /// </summary>
        private static void InitializeServer()
        {
            _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 502);
            _listener.Start();

            var factory = new ModbusFactory();
            _slaveNetwork = factory.CreateSlaveNetwork(_listener);

            for (byte i = 1; i <= 3; i++)
            {
                var slave = factory.CreateSlave(i);

                var testUnit = new ModbusTestUnit(
                    i,
                    slave,
                    Capacities[i - 1],
                    Flows[i - 1]);

                _slaves.Add(testUnit);

                _slaveNetwork.AddSlave(slave);

                Console.WriteLine(
                    $"Добавлен Slave ID={i} | Capacity={Capacities[i - 1]} | Flow={Flows[i - 1]}");
            }

            var lastSlave = factory.CreateSlave(4);
            var lastTestUnit = new ModbusTestUnit(
                4,
                lastSlave,
                Capacities[3]);

            foreach (var unit in _slaves)
            {
                unit.SetReceiver(lastTestUnit.Tank);
            }

            _slaves.Add(lastTestUnit);
            _slaveNetwork.AddSlave(lastSlave);

            Console.WriteLine("\nСервер успешно запущен.\n");
        }

        /// <summary>
        /// Фоновая симуляция обновления устройств
        /// </summary>
        private static async Task RunDataSimulationAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    foreach (var slave in _slaves)
                    {
                        slave.Update();
                        slave.PrintStatus();
                    }

                    Console.WriteLine(new string('-', 70));

                    await Task.Delay(5000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка симуляции: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Корректная остановка сервера
        /// </summary>
        private static void Shutdown()
        {
            try
            {
                _slaveNetwork?.Dispose();
                _listener?.Stop();
                _cts.Dispose();
            }
            catch
            {
                // Игнорируем ошибки при завершении
            }

            Console.WriteLine("Сервер завершил работу.");
        }
    }
}