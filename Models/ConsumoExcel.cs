using System;

namespace Zenko.Models
{
    public class ConsumoExcel
    {
        public string Canal { get; set; }
        public string OpNumero { get; set; }
        public string ModeloCodigo { get; set; }
        public string ModeloNombre { get; set; }
        public string ColorCodigo { get; set; }
        public string ColorNombre { get; set; }
        public string InsumoCodigo { get; set; }
        public string InsumoDescripcion { get; set; }
        public decimal? CostoInsumoOperacion { get; set; }
        public DateTime? Fecha { get; set; }
        public decimal? Corte { get; set; }
        public decimal? ConsumoPromedio { get; set; }
        public decimal? CantidadRemitida { get; set; }
        public decimal? Devolucion { get; set; }
        public decimal? ConsumoRealOperacion { get; set; }
    }
}
