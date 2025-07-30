namespace YooVisitApi.Dtos.BackOffice
{
    public class UserDetailDto
    {
        public Guid IdUtilisateur { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Joueur";
        public string Statut { get; set; } = "Actif";
        public DateTime DateInscription { get; set; }
        public int Experience { get; set; }
    }
}
