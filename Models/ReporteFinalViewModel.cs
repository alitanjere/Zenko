namespace Zenko.Models
{
    public class ReporteFinalViewModel
    {
        public string VarianteCodigo { get; set; }
        public string VarianteNombre { get; set; }
        public string ModeloCodigo { get; set; }
        public string ModeloNombre { get; set; }
        public string CodigoInsumo { get; set; }
        public string InsumoDescripcion { get; set; }
        public decimal CostoInsumo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CostoTotal { get; set; }
    }
}
