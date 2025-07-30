﻿using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos.Pastille
{
    public class PastilleUpdateDto
    {
        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? StyleArchitectural { get; set; }
        public string? PeriodeConstruction { get; set; }
        public string? HorairesOuverture { get; set; }
    }
}
