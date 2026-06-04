using System.Collections.Generic;
using Maryar.Api.Models;

namespace Maryar.Api.Repositories.Interfaces
{
    public interface IBrandRepository
    {
        IEnumerable<Brand> GetAll();
    }
}
