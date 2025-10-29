namespace YooVisitApi.Dtos.Shared;

// DTO minimaliste pour l'envoi de l'ID lors des suppressions.
public class IdDto
{
    // Typage fort : L'ID de l'entité à supprimer
    public required string Id { get; set; }
}