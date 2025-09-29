namespace Zenko.Models
{
    public class ConsumoPromedioPorOperacion
    {
        public string OpNumero { get; set; }
        public string InsumoCodigo { get; set; }
        public string InsumoDescripcion { get; set; }
        public decimal? CostoInsumoOperacion { get; set; }
        public decimal? ConsumoPromedio { get; set; }
        public decimal? ConsumoReal { get; set; }
        public decimal CantidadRemitidaTotal { get; set; }
        public decimal DevolucionTotal { get; set; }
        public string ModeloCodigo { get; set; }
        public string ModeloNombre { get; set; }
    }
}
