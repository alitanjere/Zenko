using System.Collections.Generic;

namespace Zenko.Models
{
    public class ResultadoViewModel
    {
        public List<TelaExcel> TelasDetalle { get; set; }
        public List<AvioExcel> AviosDetalle { get; set; }
        public decimal CostoTotalTelas { get; set; }
        public decimal CostoTotalAvios { get; set; }
        public decimal CostoTotalGeneral { get; set; }
        public string MensajeError { get; set; } // Para errores o información

        public ResultadoViewModel()
        {
            TelasDetalle = new List<TelaExcel>();
            AviosDetalle = new List<AvioExcel>();
        }
    }
}
