﻿namespace YooVisitApi.Dtos.BackOffice
{
    public class PastilleCreateDto
    {
        public string Title { get; set; }
        public string FileKey { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Altitude { get; set; }
        public string Description { get; set; }
        public string StyleArchitectural { get; set; }
        public string PeriodeConstruction { get; set; }
        public string HorairesOuverture { get; set; }
    }
}
