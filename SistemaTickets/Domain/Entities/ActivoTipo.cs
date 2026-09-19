namespace SistemaTickets.Domain.Entities
{
    public class ActivoTipo
    {
        public virtual int    Id     { get; set; }
        public virtual string Nombre { get; set; } = string.Empty;
        public virtual bool   Activo { get; set; } = true;
    }
}
