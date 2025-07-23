namespace YooVisitApi.Dtos
{
    public class UserDto
    {
        public Guid IdUtilisateur { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime DateInscription { get; set; }
        public int Experience { get; set; } = 0;
        public string? Nom { get; set; }
        public string? Biographie { get; set; }
        public string? ProfilePictureUrl { get; set; }

    }
}
