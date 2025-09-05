using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace YooVisitApi.RealTime
{
    public class WebSocketConnectionManager
    {
        // On utilise un ConcurrentDictionary car il est "thread-safe",
        // essentiel dans un environnement serveur où plusieurs requêtes sont traitées en parallèle.
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

        public string AddSocket(WebSocket socket)
        {
            string connectionId = Guid.NewGuid().ToString();
            _sockets.TryAdd(connectionId, socket);
            Console.WriteLine($"--> WebSocket connection established: {connectionId}");
            return connectionId;
        }

        public async Task RemoveSocket(string id)
        {
            if (_sockets.TryRemove(id, out var socket))
            {
                // On s'assure de fermer la connexion proprement si elle est toujours ouverte.
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed by manager", CancellationToken.None);
                }
                Console.WriteLine($"<-- WebSocket connection closed: {id}");
            }
        }

        public async Task BroadcastMessageAsync(string message)
        {
            Console.WriteLine($"Broadcasting message to {_sockets.Count} clients: {message}");
            var messageBuffer = Encoding.UTF8.GetBytes(message);

            // On envoie le message à toutes les sockets connectées.
            foreach (var socket in _sockets.Values)
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(new ArraySegment<byte>(messageBuffer, 0, messageBuffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
