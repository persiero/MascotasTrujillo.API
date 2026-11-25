using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MascotasTrujillo.API.DTOs
{
    public class CompararMascotaDTO
    {
        [Required]
        public IFormFile FotoEncontrada { get; set; } = null!;
    }

    public class ResultadoSimilitudDTO
    {
        public bool Exito { get; set; }

        public double PorcentajeSimilitud { get; set; }

        public string NivelSimilitud { get; set; } = string.Empty;

        public string Titulo { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public string Metodo { get; set; } = string.Empty;

        public string Advertencia { get; set; } = string.Empty;
    }
}