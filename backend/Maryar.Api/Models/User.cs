using System;

namespace Maryar.Api.Models
{
    public class User
    {
        public Guid     Id           { get; set; }
        public string   Email        { get; set; }
        public string   Name         { get; set; }
        public string   PasswordHash { get; set; }
        public string   Role         { get; set; }
        public DateTime CreatedAt    { get; set; }

        // Dados pessoais
        public string Phone { get; set; }
        public string Cpf   { get; set; }

        // Endereço de entrega
        public string Cep         { get; set; }
        public string Logradouro  { get; set; }
        public string Numero      { get; set; }
        public string Complemento { get; set; }
        public string Bairro      { get; set; }
        public string Cidade      { get; set; }
        public string Estado      { get; set; }
    }
}
