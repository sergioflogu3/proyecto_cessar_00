namespace SistemaTickets.Domain.Entities
{
    public class AsignacionHerramienta
    {
        public virtual int       Id               { get; set; }
        public virtual Herramienta Herramienta    { get; set; } = null!;
        public virtual Usuario   Tecnico          { get; set; } = null!;
        public virtual Usuario   AsignadoPor      { get; set; } = null!;
        public virtual DateTime  FechaAsignacion  { get; set; }
        public virtual DateTime? FechaDevolucion  { get; set; }
        public virtual string?   Observaciones    { get; set; }
        public virtual bool      Activa           { get; set; } = true;
    }
}
