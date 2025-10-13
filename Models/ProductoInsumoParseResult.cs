using System.Collections.Generic;

namespace Zenko.Models
{
    public class ProductoInsumoParseResult
    {
        public List<ProductoInsumoExcel> Relaciones { get; } = new List<ProductoInsumoExcel>();
        public Dictionary<string, string> TodasLasVariantes { get; } = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        public List<string> Errores { get; } = new List<string>();
        public List<string> Advertencias { get; } = new List<string>();
    }
}
