using System;

namespace Zenko.Models
{
    public class InsumoHistorico
    {
        public int IdHistorico { get; set; }
        public string CodigoInsumo { get; set; }
        public int IdTipoInsumo { get; set; }
        public decimal? CostoAnterior { get; set; } // Nullable si es el primer registro
        public DateTime FechaCambio { get; set; } // Fecha en que se movió a histórico
        public int IdSubida { get; set; }

        // Propiedades adicionales para facilitar la visualización (pobladas por la consulta en BD.cs)
        public string NombreTipoInsumo { get; set; }
        public DateTime FechaSubida { get; set; } // De la tabla SubidasHistoricas
        public string NombreArchivoOriginal { get; set; } // De la tabla SubidasHistoricas
    }
}
