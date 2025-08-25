namespace Zenko.Models
{
    public class TelaExcel
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal CostoPorMetro { get; set; }
        public string Proveedor { get; set; }

        // Unidad de medida asociada al costo. Se almacena en la unidad base.
        public string UnidadMedida { get; set; } = Zenko.Services.CalculoService.UnidadBaseTela;
    }
}
