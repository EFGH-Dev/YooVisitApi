using Microsoft.AspNetCore.SignalR;

namespace YooVisitApi.Hubs
{
    // Un Hub est le point d'entrée pour toutes les connexions temps réel.
    // Il est instancié pour chaque connexion/appel.
    public class Updates : Hub
    {
        // On peut définir des méthodes ici qui peuvent être appelées PAR le client,
        // mais pour notre besoin (broadcast serveur -> client), on n'en a pas besoin.

        public override async Task OnConnectedAsync()
        {
            // Logique à exécuter quand un nouveau client se connecte.
            Console.WriteLine($"--> SignalR client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Logique à exécuter quand un client se déconnecte.
            Console.WriteLine($"<-- SignalR client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}

