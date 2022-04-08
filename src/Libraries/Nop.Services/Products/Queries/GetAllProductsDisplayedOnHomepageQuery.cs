using System.Collections.Generic;
using MediatR;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Products.Queries
{
    public class GetAllProductsDisplayedOnHomepageQuery : IRequest<IList<Product>> {}
}