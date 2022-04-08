using System.Collections.Generic;
using MediatR;
using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Products.Queries
{
    public class SearchProductsQuery : IRequest<IPagedList<Product>>
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; } = int.MaxValue;
        public IList<int> CategoryIds { get; set; }
        public IList<int> ManufacturerIds { get; set; }
        public int StoreId { get; set; }
        public int VendorId { get; set; }
        public int WarehouseId { get; set; }
        public ProductType? ProductType { get; set; }
        public bool VisibleIndividuallyOnly { get; set; }
        public bool ExcludeFeaturedProducts { get; set; }
        public decimal? PriceMin { get; set; }
        public decimal? PriceMax { get; set; }
        public int ProductTagId { get; set; }
        public string Keywords { get; set; }
        public bool SearchDescriptions { get; set; }
        public bool SearchManufacturerPartNumber { get; set; } = true;
        public bool SearchSku { get; set; } = true;
        public bool SearchProductTags { get; set; }
        public int LanguageId { get; set; }
        public IList<SpecificationAttributeOption> FilteredSpecOptions { get; set; }
        public ProductSortingEnum OrderBy { get; set; } = ProductSortingEnum.Position;
        public bool ShowHidden { get; set; }
        public bool? OverridePublished { get; set; } = true;
    }
}