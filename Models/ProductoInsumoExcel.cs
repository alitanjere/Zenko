namespace Zenko.Models
{
    public class ProductoInsumoExcel
    {
        // Product fields
        public string VarianteCodigo { get; set; }
        public string VarianteNombre { get; set; }
        public string ModeloCodigo { get; set; }
        public string ModeloNombre { get; set; }

        // Insumo fields
        public string InsumoCodigo { get; set; }
        public string InsumoDescripcion { get; set; }

        // Relationship field
        public decimal Cantidad { get; set; }
    }
}
