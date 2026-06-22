using System;

namespace Maryar.Api.Models
{
    public class FaqItem
    {
        public int      Id        { get; set; }
        public string   Tipo      { get; set; }
        public string   Titulo    { get; set; }
        public string   Corpo     { get; set; }
        public int      Ordem     { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool     Ativo     { get; set; }
    }
}
