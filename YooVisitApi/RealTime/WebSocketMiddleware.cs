using System.Net.WebSockets;

namespace YooVisitApi.RealTime
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketConnectionManager _manager;

        public WebSocketMiddleware(RequestDelegate next, WebSocketConnectionManager manager)
        {
            _next = next;
            _manager = manager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Si la requête est pour notre endpoint WebSocket...
            if (context.Request.Path == "/ws/updates")
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    // On accepte la connexion
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    string connectionId = _manager.AddSocket(webSocket);

                    try
                    {
                        // On maintient la connexion ouverte pour écouter les messages (surtout la fermeture)
                        await HandleWebSocketAsync(webSocket);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"WebSocket error: {ex.Message}");
                    }
                    finally
                    {
                        // Nettoyage : on retire la socket quand la connexion est perdue
                        await _manager.RemoveSocket(connectionId);
                    }
                }
                else
                {
                    context.Response.StatusCode = 400; // Bad Request
                }
            }
            else
            {
                // Si ce n'est pas une requête pour nous, on la passe au middleware suivant
                await _next(context);
            }
        }

        private async Task HandleWebSocketAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            // On attend de recevoir un message. La connexion reste ouverte tant qu'on n'en reçoit pas.
            // Si le client se déconnecte, ReceiveAsync lèvera une exception ou retournera un message de fermeture.
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                // On ne fait rien avec les messages reçus pour l'instant, mais on continue d'écouter.
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
        }
    }
}
