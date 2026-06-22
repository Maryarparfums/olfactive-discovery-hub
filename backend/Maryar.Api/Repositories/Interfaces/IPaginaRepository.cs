using System.Collections.Generic;
using Maryar.Api.Models;

namespace Maryar.Api.Repositories.Interfaces
{
    public interface IPaginaRepository
    {
        PaginaEstatica       GetBySlug(string slug);
        IEnumerable<FaqItem> GetFaqByTipo(string tipo);
    }
}
