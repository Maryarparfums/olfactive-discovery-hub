using System;

namespace Maryar.Api.Models
{
    public class PaginaEstatica
    {
        public int      Id        { get; set; }
        public string   Slug      { get; set; }
        public string   Titulo    { get; set; }
        public string   Corpo     { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool     Ativo     { get; set; }
    }
}
