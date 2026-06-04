using System;
using System.Collections.Generic;
using Maryar.Api.Dtos;
using Maryar.Api.Models;

namespace Maryar.Api.Repositories.Interfaces
{
    public interface IProductRepository
    {
        IEnumerable<Product> Query(ProductQueryDto q, out int total);
        Product GetById(Guid id);
        Product GetBySlug(string slug);
        PerfumeDetails GetDetailsByProductId(Guid productId);
    }
}
