namespace MascotasTrujillo.API.DTOs
{
    public class MascotaCreateDTO
    {
        // Solo pedimos lo esencial para registrar un perrito/gatito
        public string Nombre { get; set; } = string.Empty;
        public string? Especie { get; set; }
        public string? Raza { get; set; }
        public string? ColorPrincipal { get; set; }
        public string? RasgosParticulares { get; set; }

    }
}
