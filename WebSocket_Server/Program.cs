using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TimeWebChatServer
{
    public class WebSocketServer
    {
        private List<WebSocket> _clients = new List<WebSocket>();
        private HttpListener _listener;

        class Program
        {
            static async Task Main(string[] args)
            {
                Console.WriteLine("🚀 Starting TimeWeb Chat Server for Linux...");

                try
                {
                    var server = new WebSocketServer();
                    await server.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 Failed to start: {ex.Message}");
                    Console.WriteLine("🔧 Possible solutions:");
                    Console.WriteLine("1. Run with sudo for port binding");
                    Console.WriteLine("2. Check if port 8888 is available");
                    Console.WriteLine("3. Configure firewall: sudo ufw allow 8888");
                }
            }
        }

        public async Task Start()
        {
            _listener = new HttpListener();

            // Используем localhost для тестирования
            _listener.Prefixes.Add("http://+:8888/");

            try
            {
                _listener.Start();
                Console.WriteLine("✅ Сервер запущен на http://+:8888/");
                Console.WriteLine("Ожидаем подключения клиентов...");

                await ListenForClients();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка запуска сервера: {ex.Message}");
                throw;
            }
        }

        private async Task ListenForClients()
        {
            while (true)
            {
                try
                {
                    var context = await _listener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        var webSocketContext = await context.AcceptWebSocketAsync(null);
                        var webSocket = webSocketContext.WebSocket;

                        _clients.Add(webSocket);
                        Console.WriteLine($"✅ Новый клиент подключен! Всего: {_clients.Count}");

                        // Отправляем приветственное сообщение новому клиенту
                        await SendToClient(webSocket, "Добро пожаловать в чат! Вы подключены.");

                        // Уведомляем всех о новом пользователе
                        await BroadcastMessage($"Новый пользователь присоединился к чату. Всего участников: {_clients.Count}", null);

                        // Запускаем обработку клиента
                        _ = HandleClient(webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 200;
                        byte[] buffer = Encoding.UTF8.GetBytes("WebSocket Chat Server is running");
                        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при принятии подключения: {ex.Message}");
                }
            }
        }

        private async Task HandleClient(WebSocket webSocket)
        {
            byte[] buffer = new byte[1024];
            string clientInfo = $"Клиент {_clients.IndexOf(webSocket) + 1}";

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None
                    );

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"📨 {clientInfo}: {message}");

                        // Рассылаем ВСЕМ клиентам, включая отправителя
                        await BroadcastMessage(message, null); // null = всем, включая отправителя
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"🔌 {clientInfo} запросил отключение");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка с {clientInfo}: {ex.Message}");
            }
            finally
            {
                _clients.Remove(webSocket);
                Console.WriteLine($"➖ {clientInfo} отключен. Осталось: {_clients.Count}");

                // Уведомляем остальных об уходе пользователя
                await BroadcastMessage($"Пользователь покинул чат. Осталось участников: {_clients.Count}", null);

                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Сервер закрыл соединение",
                        CancellationToken.None
                    );
                }
                webSocket.Dispose();
            }
        }

        // Отправка сообщения всем клиентам (включая отправителя)
        private async Task BroadcastMessage(string message, WebSocket sender)
        {
            if (_clients.Count == 0) return;

            var tasks = new List<Task>();
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            int sentCount = 0;

            foreach (var client in _clients)
            {
                // Отправляем ВСЕМ клиентам, не проверяем client != sender
                if (client.State == WebSocketState.Open)
                {
                    try
                    {
                        tasks.Add(client.SendAsync(
                            new ArraySegment<byte>(bytes),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        ));
                        sentCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка отправки клиенту: {ex.Message}");
                    }
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
                Console.WriteLine($"📤 Сообщение отправлено {sentCount} клиентам");
            }
        }

        // Отправка сообщения конкретному клиенту
        private async Task SendToClient(WebSocket client, string message)
        {
            if (client.State == WebSocketState.Open)
            {
                try
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(message);
                    await client.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки клиенту: {ex.Message}");
                }
            }
        }
    }
}