using System.Collections.Generic;
using MediatR;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Products.Queries
{
    /// <summary>
    /// Represents request that provide result list of all products displayed on the home page
    /// </summary>
    public class GetAllProductsDisplayedOnHomepageQuery : IRequest<IList<Product>> {}
}