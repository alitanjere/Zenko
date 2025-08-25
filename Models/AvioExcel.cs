namespace Zenko.Models
{
    public class AvioExcel
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal CostoUnidad { get; set; }
        public string UnidadMedida { get; set; } = Zenko.Services.CalculoService.UnidadBaseAvio;
        public string Proveedor { get; set; }
    }
}
