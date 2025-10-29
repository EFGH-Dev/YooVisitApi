namespace YooVisitApi.Dtos.Shared;

// DTO générique pour tous les événements temps réel
public class UpdateReceivedDto
{
    // Typage fort : Nom de l'entité ("Pastille" ou "Zone")
    public required string EntityType { get; set; }

    // Typage fort : Action effectuée ("Created", "Updated", "Deleted")
    public required string Action { get; set; }

    // Typage faible : Le corps réel des données. 
    // Il sera sérialisé/désérialisé par System.Text.Json.
    public required object Payload { get; set; }
}