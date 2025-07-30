using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos.Pastille
{
    public class PastilleRatingDto
    {
        [Range(1, 5)] // On s'assure que la note est entre 1 et 5
        public int Rating { get; set; }
    }
}
