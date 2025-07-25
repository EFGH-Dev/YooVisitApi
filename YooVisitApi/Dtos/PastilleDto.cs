﻿namespace YooVisitApi.Dtos
{
    public class PastilleDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Altitude { get; set; }
        public string? StyleArchitectural { get; set; }
        public string? PeriodeConstruction { get; set; }
        public string? HorairesOuverture { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; } // Le nom du créateur
        public double AverageRating { get; set; }
        public List<PhotoDto> Photos { get; set; } = new();
        public bool IsOwner { get; set; }
    }
}
