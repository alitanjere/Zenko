using System;

namespace Zenko.Models
{
    public class SubidaHistorica
    {
        public int IdSubida { get; set; }
        public DateTime FechaSubida { get; set; }
        public string NombreArchivoOriginal { get; set; }
        public int? CantidadRegistrosSubidos { get; set; } // Nullable por si falla antes de contar
    }
}
