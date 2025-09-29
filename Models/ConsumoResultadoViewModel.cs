using System.Collections.Generic;

namespace Zenko.Models
{
    public class ConsumoResultadoViewModel
    {
        public List<ConsumoPromedioPorOperacion> ConsumosPorOperacion { get; set; }
        public List<ConsumoPromedioPorProducto> ConsumosPorProducto { get; set; }
        public int RegistrosProcesados { get; set; }

        public ConsumoResultadoViewModel()
        {
            ConsumosPorOperacion = new List<ConsumoPromedioPorOperacion>();
            ConsumosPorProducto = new List<ConsumoPromedioPorProducto>();
        }
    }
}
