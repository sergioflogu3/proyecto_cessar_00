namespace SistemaTickets.Domain.Entities
{
    public class Activo
    {
        public virtual int       Id               { get; set; }
        public virtual string    Nombre           { get; set; } = string.Empty;
        public virtual string?   Descripcion      { get; set; }
        public virtual string    Tipo             { get; set; } = string.Empty;
        public virtual string?   NumeroSerie      { get; set; }
        public virtual string?   Marca            { get; set; }
        public virtual string?   Modelo           { get; set; }
        public virtual string    Estado           { get; set; } = "Disponible";
        public virtual Sede?     Sede             { get; set; }
        public virtual Area?     Area             { get; set; }
        public virtual Usuario?  AsignadoA        { get; set; }
        public virtual DateTime? FechaAdquisicion { get; set; }
        public virtual DateTime? FechaGarantia    { get; set; }
        public virtual bool      EsActivo         { get; set; } = true;
        public virtual DateTime  CreadoEn         { get; set; }

        public virtual IList<InventarioMovimiento> Movimientos { get; set; }
            = new List<InventarioMovimiento>();
    }
}
