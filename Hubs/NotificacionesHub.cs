using Microsoft.AspNetCore.SignalR;

namespace it_ops_backend.Hubs;

public class NotificacionesHub : Hub
{
    // Métodos a los que el cliente de Next.js o frontend puede suscribirse
    public async Task BroadcastNuevoTicket(object ticket)
    {
        await Clients.All.SendAsync("nuevoTicket", ticket);
    }

    public async Task BroadcastTicketActualizado(object ticket)
    {
        await Clients.All.SendAsync("ticketActualizado", ticket);
    }

    public async Task BroadcastTicketEliminado(string id)
    {
        await Clients.All.SendAsync("ticketEliminado", id);
    }
}

