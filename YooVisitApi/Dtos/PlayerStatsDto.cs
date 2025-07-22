namespace YooVisitAPI.Dtos
{
    public class PlayerStatsDto
    {
        public string UserName { get; set; } = string.Empty;
        public double ExplorationProgress { get; set; }
        public int AccessPoints { get; set; }
        public int Experience { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
