using System.Collections.Generic;
using Maryar.Api.Models;

namespace Maryar.Api.Repositories.Interfaces
{
    public interface IFamilyRepository
    {
        IEnumerable<FragranceFamily> GetAll();
    }
}
