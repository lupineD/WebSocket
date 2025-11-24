using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TimeWebChatClient
{
    class Program
    {
        private static ClientWebSocket webSocket;
        //private static string serverUrl = "ws://localhost:8888/";
        private static string serverUrl = "ws://89.223.69.163:8888/"; // WebSocket URL
        private static string userName;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== TimeWeb Chat Client ===");
            Console.WriteLine($"Подключаемся к: {serverUrl}");

            Console.Write("Введите ваше имя: ");
            userName = Console.ReadLine();

            webSocket = new ClientWebSocket();

            try
            {
                await webSocket.ConnectAsync(new Uri(serverUrl), CancellationToken.None);
                Console.WriteLine("✅ Подключение успешно!");
                Console.WriteLine("=== Начинайте общение ===");
                Console.WriteLine("(введите 'exit' для выхода)");
                Console.WriteLine(new string('-', 40));

                // Запускаем получение сообщений
                var receiveTask = ReceiveMessages();

                // Основной цикл отправки
                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("Вы: ");
                    Console.ResetColor();

                    string text = Console.ReadLine();

                    if (text?.ToLower() == "exit")
                        break;

                    if (!string.IsNullOrEmpty(text))
                    {
                        string fullMessage = $"{userName}: {text}";
                        await SendMessage(fullMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка подключения: {ex.Message}");
                Console.WriteLine("Убедитесь, что сервер запущен");
            }
            finally
            {
                if (webSocket?.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Выход",
                        CancellationToken.None
                    );
                }
                Console.WriteLine("Отключено от сервера");
            }
        }

        static async Task SendMessage(string message)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки: {ex.Message}");
            }
        }

        static async Task ReceiveMessages()
        {
            byte[] buffer = new byte[4096];

            try
            {
                while (webSocket?.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None
                    );

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        // Разные цвета для системных сообщений и сообщений пользователей
                        if (message.Contains("присоединился") || message.Contains("покинул") || message.Contains("Добро пожаловать"))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                        }
                        else if (message.StartsWith(userName + ":"))
                        {
                            Console.ForegroundColor = ConsoleColor.Green; // Свои сообщения
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan; // Сообщения других
                        }

                        Console.WriteLine($"\n{message}");
                        Console.ResetColor();
                        Console.Write("Вы: ");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Ошибка получения: {ex.Message}");
            }
        }
    }
}