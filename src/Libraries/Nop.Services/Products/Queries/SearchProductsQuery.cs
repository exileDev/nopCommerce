using System.Collections.Generic;
using MediatR;
using Nop.Core;
using Nop.Core.Domain.Catalog;
namespace Nop.Services.Products.Queries
{
    /// <summary>
    /// Represents request that provide result list of products
    /// </summary>
    public class SearchProductsQuery : IRequest<IPagedList<Product>>
    {
        /// <summary>
        /// Page index
        /// </summary>
        public int PageIndex { get; set; }
        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; } = int.MaxValue;
        /// <summary>
        /// Category identifiers
        /// </summary>
        public IList<int> CategoryIds { get; set; }
        /// <summary>
        /// Manufacturer identifiers
        /// </summary>
        public IList<int> ManufacturerIds { get; set; }
        /// <summary>
        /// Store identifier; 0 to load all records
        /// </summary>
        public int StoreId { get; set; }
        /// <summary>
        /// Vendor identifier; 0 to load all records
        /// </summary>
        public int VendorId { get; set; }
        /// <summary>
        /// Warehouse identifier; 0 to load all records
        /// </summary>
        public int WarehouseId { get; set; }
        /// <summary>
        /// Product type; 0 to load all records
        /// </summary>
        public ProductType? ProductType { get; set; }
        /// <summary>
        /// A values indicating whether to load only products marked as "visible individually"; "false" to load all records; "true" to load "visible individually" only
        /// </summary>
        public bool VisibleIndividuallyOnly { get; set; }
        /// <summary>
        /// A value indicating whether loaded products are marked as featured (relates only to categories and manufacturers); "false" (by default) to load all records; "true" to exclude featured products from results
        /// </summary>
        public bool ExcludeFeaturedProducts { get; set; }
        /// <summary>
        /// Minimum price; null to load all records
        /// </summary>
        public decimal? PriceMin { get; set; }

        /// <summary>
        /// Maximum price; null to load all records
        /// </summary>
        public decimal? PriceMax { get; set; }

        /// <summary>
        /// Product tag identifier; 0 to load all records
        /// </summary>
        public int ProductTagId { get; set; }
        /// <summary>
        /// Keywords
        /// </summary>
        public string Keywords { get; set; }

        /// <summary>
        /// A value indicating whether to search by a specified "keyword" in product descriptions
        /// </summary>
        public bool SearchDescriptions { get; set; }

        /// <summary>
        /// A value indicating whether to search by a specified "keyword" in manufacturer part number
        /// </summary>
        public bool SearchManufacturerPartNumber { get; set; } = true;

        /// <summary>
        /// A value indicating whether to search by a specified "keyword" in product SKU
        /// </summary>
        public bool SearchSku { get; set; } = true;

        /// <summary>
        /// A value indicating whether to search by a specified "keyword" in product tags
        /// </summary>
        public bool SearchProductTags { get; set; }

        /// <summary>
        /// /// Language identifier (search for text searching)
        /// </summary>
        public int LanguageId { get; set; }

        /// <summary>
        /// Specification options list to filter products; null to load all records
        /// </summary>
        public IList<SpecificationAttributeOption> FilteredSpecOptions { get; set; }

        /// <summary>
        /// Order by
        /// </summary>
        public ProductSortingEnum OrderBy { get; set; } = ProductSortingEnum.Position;
        /// <summary>
        /// A value indicating whether to show hidden records
        /// </summary>
        public bool ShowHidden { get; set; }
        /// <summary>
        /// null - process "Published" property according to "showHidden" parameter
        /// true - load only "Published" products
        /// false - load only "Unpublished" products
        /// </summary>
        public bool? OverridePublished { get; set; } = true;
    }
}