namespace YooVisitApi.Dtos.BackOffice
{
    public class UserListDto
    {
        public Guid IdUtilisateur { get; set; }
        public string Nom { get; set; }
        public string Email { get; set; }
        public DateTime DateInscription { get; set; }
        public int Experience { get; set; }
    }
}
