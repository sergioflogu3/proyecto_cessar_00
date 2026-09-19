namespace SistemaTickets.Domain.Entities
{
    public class Herramienta
    {
        public virtual int     Id          { get; set; }
        public virtual string  Nombre      { get; set; } = string.Empty;
        public virtual string? Descripcion { get; set; }
        public virtual string? Codigo      { get; set; }
        public virtual string  Tipo        { get; set; } = string.Empty;
        public virtual string  Estado      { get; set; } = "Disponible";
        public virtual bool    Activa      { get; set; } = true;
        public virtual DateTime CreadoEn  { get; set; }

        public virtual IList<AsignacionHerramienta> Asignaciones { get; set; }
            = new List<AsignacionHerramienta>();
        public virtual IList<InventarioMovimiento> Movimientos { get; set; }
            = new List<InventarioMovimiento>();
    }
}
