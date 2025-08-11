using System;

namespace Zenko.Models
{
    public class ReporteFinalViewModel
    {
        public string NombreProducto { get; set; }
        public string CodigoProducto { get; set; }
        public string CodigoInsumo { get; set; }
        public decimal CostoInsumo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CostoTotal { get; set; }
    }
}
