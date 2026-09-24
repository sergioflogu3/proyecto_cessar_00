namespace SistemaTickets.Domain.Entities
{
    public class AuditoriaUsuario
    {
        public virtual int Id { get; set; }
        public virtual Usuario Usuario { get; set; } = null!;
        public virtual Usuario ModificadoPor { get; set; } = null!;
        public virtual string Campo { get; set; } = string.Empty;
        public virtual string? ValorAnterior { get; set; }
        public virtual string? ValorNuevo { get; set; }
        public virtual DateTime FechaCambio { get; set; }
    }
}
