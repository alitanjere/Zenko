namespace Zenko.Models
{
    public class VariacionCosto
    {
        public string VarianteCodigo { get; set; }
        public decimal CostoActual { get; set; }
        public decimal CostoPrevio { get; set; }
        public decimal Diferencia { get; set; }
    }
}
