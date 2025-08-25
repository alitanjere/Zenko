using System;

namespace Zenko.Models
{
    public class Auditoria
    {
        public int Id { get; set; }
        public string Archivo { get; set; }
        public int UsuarioId { get; set; }
        public DateTime Fecha { get; set; }
        public string ResumenCambios { get; set; }
    }
}
