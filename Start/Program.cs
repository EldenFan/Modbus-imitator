using Base.Simulation;
using ModbusTcpSimulator;
using OpcUaImitator;

class Program
{
    private static readonly CancellationTokenSource cts = new();

    private static readonly SimulatorComplex simulator = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Cимулятор устройств ===");
        Console.WriteLine("Нажмите Ctrl+C для остановки\n");

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\nОстановка ...");
            cts.Cancel();
        };

        if (args.Length == 0)
        {
            await StartConsoleWrite();
        }
        else
        {
            await StartService(args);
        }
    }

    private static async Task StartConsoleWrite()
    {
        while (!cts.Token.IsCancellationRequested)
        {
            Console.WriteLine("Доступные сервисы:"); 
            Console.WriteLine(" 1. Modbus TCP"); 
            Console.WriteLine(" 2. OPC UA"); 
            Console.WriteLine(" 0. Выход"); 
            Console.WriteLine();

            Console.Write("Выберите сервис: ");

            var input = Console.ReadLine();

            switch (input?.ToLower()) 
            { 
                case "1": 
                case "modbus": 
                case "modbus tcp": 
                    await StartModbus(); 
                    return; 
                case "2": 
                case "opc": 
                case "opc ua": 
                    await StartOpcUa(); 
                    return; 
                case "0": 
                case "exit": 
                    return; 
                default: 
                    Console.WriteLine($"\nНеизвестный сервис: {input}. Попробуйте ещё раз.\n"); 
                    break; 
            }
        }
    }

    private static async Task StartService(string[] args)
    {
        var serviceName = args[0].ToLower();
        switch (serviceName)
        {
            case "modbus":
            case "modbus tcp":
                await StartModbus();
                break;
            case "opc":
            case "opc ua":
                await StartOpcUa();
                break;
            default:
                Console.WriteLine($"Неизвестный сервис: {serviceName}");
                break;
        }
    }

    private static async Task StartModbus()
    {
        Console.WriteLine("Запуск Modbus TCP...");
        var modbus = new ModbusServer();
        modbus.LogMessage += OnLogMessage;
        modbus.Start(simulator);
        await modbus.RunAsync(cts.Token);
    }

    private static async Task StartOpcUa()
    {
        Console.WriteLine("Запуск OPC UA...");
        var opc = new OpcServer();
        opc.LogMessage += OnLogMessage;
        opc.Start(simulator);
        await opc.RunAsync(cts.Token);
    }

    private static void OnLogMessage(string message)
    {
        Console.WriteLine(message);
    }
}
