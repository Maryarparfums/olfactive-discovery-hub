using System;

namespace Maryar.Api.Models
{
    // Atributos olfativos persistidos como JSON em perfume_details.
    public class PerfumeDetails
    {
        public Guid ProductId { get; set; }
        // JSON strings carregadas direto da coluna; parseadas no controller/DTO.
        public string NotasTopoJson { get; set; }
        public string NotasCoracaoJson { get; set; }
        public string NotasBaseJson { get; set; }
        public string EstacaoJson { get; set; }   // {"Primavera":8,...}
        public string PeriodoJson { get; set; }   // {"Dia":7,"Noite":9}
        public string OcasiaoJson { get; set; }   // {"Trabalho":8,...}
        public int Fixacao { get; set; }
        public int Projecao { get; set; }
        public string DuracaoHoras { get; set; }
        public string SimilaresJson { get; set; } // ["slug1","slug2"]
    }
}
