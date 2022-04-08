using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Catalog;

namespace Nop.Services.Products.Queries
{
    public class GetAllProductsDisplayedOnHomepageQueryHandler : IRequestHandler<GetAllProductsDisplayedOnHomepageQuery, IList<Product>>
    {
        protected readonly IRepository<Product> _productRepository;

        public GetAllProductsDisplayedOnHomepageQueryHandler(IRepository<Product> productRepository)
        {
            _productRepository = productRepository;
        }

        public Task<IList<Product>> Handle(GetAllProductsDisplayedOnHomepageQuery request, CancellationToken cancellationToken)
        {
            return _productRepository.GetAllAsync(query =>
            {
                return from p in query
                       orderby p.DisplayOrder, p.Id
                       where p.Published &&
                             !p.Deleted &&
                             p.ShowOnHomepage
                       select p;
            }, cache => cache.PrepareKeyForDefaultCache(NopCatalogDefaults.ProductsHomepageCacheKey));
        }
    }
}