using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Localization;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Stores;

namespace Nop.Services.Products.Queries
{
    public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, IPagedList<Product>>
    {
        protected readonly CatalogSettings _catalogSettings;
        protected readonly IAclService _aclService;
        protected readonly ILanguageService _languageService;
        protected readonly IRepository<Category> _categoryRepository;
        protected readonly IRepository<LocalizedProperty> _localizedPropertyRepository;
        protected readonly IRepository<Manufacturer> _manufacturerRepository;
        protected readonly IRepository<ProductAttributeCombination> _productAttributeCombinationRepository;
        protected readonly IRepository<ProductCategory> _productCategoryRepository;
        protected readonly IRepository<ProductManufacturer> _productManufacturerRepository;
        protected readonly IRepository<ProductProductTagMapping> _productTagMappingRepository;

        protected readonly IRepository<ProductSpecificationAttribute> _productSpecificationAttributeRepository;
        protected readonly IRepository<ProductTag> _productTagRepository;
        protected readonly IRepository<ProductWarehouseInventory> _productWarehouseInventoryRepository;
        protected readonly IRepository<Product> _productRepository;
        protected readonly IStoreMappingService _storeMappingService;
        protected readonly IStoreService _storeService;
        protected readonly IWorkContext _workContext;

        public SearchProductsQueryHandler(CatalogSettings catalogSettings, IAclService aclService, ILanguageService languageService, IRepository<Category> categoryRepository, IRepository<LocalizedProperty> localizedPropertyRepository, IRepository<Manufacturer> manufacturerRepository, IRepository<ProductAttributeCombination> productAttributeCombinationRepository, IRepository<ProductCategory> productCategoryRepository, IRepository<ProductManufacturer> productManufacturerRepository, IRepository<ProductProductTagMapping> productTagMappingRepository, IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository, IRepository<ProductTag> productTagRepository, IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository, IRepository<Product> productRepository, IStoreMappingService storeMappingService, IStoreService storeService, IWorkContext workContext)
        {
            _catalogSettings = catalogSettings;
            _aclService = aclService;
            _languageService = languageService;
            _categoryRepository = categoryRepository;
            _localizedPropertyRepository = localizedPropertyRepository;
            _manufacturerRepository = manufacturerRepository;
            _productAttributeCombinationRepository = productAttributeCombinationRepository;
            _productCategoryRepository = productCategoryRepository;
            _productManufacturerRepository = productManufacturerRepository;
            _productTagMappingRepository = productTagMappingRepository;
            _productSpecificationAttributeRepository = productSpecificationAttributeRepository;
            _productTagRepository = productTagRepository;
            _productWarehouseInventoryRepository = productWarehouseInventoryRepository;
            _productRepository = productRepository;
            _storeMappingService = storeMappingService;
            _storeService = storeService;
            _workContext = workContext;
        }

        public async Task<IPagedList<Product>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
        {
            //some databases don't support int.MaxValue
            if (request.PageSize == int.MaxValue)
                request.PageSize = int.MaxValue - 1;

            var productsQuery = _productRepository.Table;

            if (!request.ShowHidden)
                productsQuery = productsQuery.Where(p => p.Published);
            else if (request.OverridePublished.HasValue)
                productsQuery = productsQuery.Where(p => p.Published == request.OverridePublished.Value);

            //apply store mapping constraints
            productsQuery = await _storeMappingService.ApplyStoreMapping(productsQuery, request.StoreId);

            //apply ACL constraints
            if (!request.ShowHidden)
            {
                var customer = await _workContext.GetCurrentCustomerAsync();
                productsQuery = await _aclService.ApplyAcl(productsQuery, customer);
            }

            productsQuery =
                from p in productsQuery
                where !p.Deleted &&
                    (!request.VisibleIndividuallyOnly || p.VisibleIndividually) &&
                    (request.VendorId == 0 || p.VendorId == request.VendorId) &&
                    (
                        request.WarehouseId == 0 ||
                        (
                            !p.UseMultipleWarehouses ? p.WarehouseId == request.WarehouseId :
                                _productWarehouseInventoryRepository.Table.Any(pwi => pwi.WarehouseId == request.WarehouseId && pwi.ProductId == p.Id)
                        )
                    ) &&
                    (request.ProductType == null || p.ProductTypeId == (int)request.ProductType) &&
                    (request.ShowHidden ||
                            DateTime.UtcNow >= (p.AvailableStartDateTimeUtc ?? DateTime.MinValue) &&
                            DateTime.UtcNow <= (p.AvailableEndDateTimeUtc ?? DateTime.MaxValue)
                    ) &&
                    (request.PriceMin == null || p.Price >= request.PriceMin) &&
                    (request.PriceMax == null || p.Price <= request.PriceMax)
                select p;

            if (!string.IsNullOrEmpty(request.Keywords))
            {
                var langs = await _languageService.GetAllLanguagesAsync(showHidden: true);

                //Set a flag which will to points need to search in localized properties. If showHidden doesn't set to true should be at least two published languages.
                var searchLocalizedValue = request.LanguageId > 0 && langs.Count >= 2 && (request.ShowHidden || langs.Count(l => l.Published) >= 2);

                IQueryable<int> productsByKeywords;

                productsByKeywords =
                        from p in _productRepository.Table
                        where p.Name.Contains(request.Keywords) ||
                            (request.SearchDescriptions &&
                                (p.ShortDescription.Contains(request.Keywords) || p.FullDescription.Contains(request.Keywords))) ||
                            (request.SearchManufacturerPartNumber && p.ManufacturerPartNumber == request.Keywords) ||
                            (request.SearchSku && p.Sku == request.Keywords)
                        select p.Id;

                if (searchLocalizedValue)
                {
                    productsByKeywords = productsByKeywords.Union(
                        from lp in _localizedPropertyRepository.Table
                        let checkName = lp.LocaleKey == nameof(Product.Name) &&
                                        lp.LocaleValue.Contains(request.Keywords)
                        let checkShortDesc = request.SearchDescriptions &&
                                        lp.LocaleKey == nameof(Product.ShortDescription) &&
                                        lp.LocaleValue.Contains(request.Keywords)
                        where
                            lp.LocaleKeyGroup == nameof(Product) && lp.LanguageId == request.LanguageId && (checkName || checkShortDesc)

                        select lp.EntityId);
                }

                //search by SKU for ProductAttributeCombination
                if (request.SearchSku)
                {
                    productsByKeywords = productsByKeywords.Union(
                        from pac in _productAttributeCombinationRepository.Table
                        where pac.Sku == request.Keywords
                        select pac.ProductId);
                }

                //search by category name if admin allows
                if (_catalogSettings.AllowCustomersToSearchWithCategoryName)
                {
                    productsByKeywords = productsByKeywords.Union(
                        from pc in _productCategoryRepository.Table
                        join c in _categoryRepository.Table on pc.CategoryId equals c.Id
                        where c.Name.Contains(request.Keywords)
                        select pc.ProductId
                    );

                    if (searchLocalizedValue)
                    {
                        productsByKeywords = productsByKeywords.Union(
                        from pc in _productCategoryRepository.Table
                        join lp in _localizedPropertyRepository.Table on pc.CategoryId equals lp.EntityId
                        where lp.LocaleKeyGroup == nameof(Category) &&
                              lp.LocaleKey == nameof(Category.Name) &&
                              lp.LocaleValue.Contains(request.Keywords) &&
                              lp.LanguageId == request.LanguageId
                        select pc.ProductId);
                    }
                }

                //search by manufacturer name if admin allows
                if (_catalogSettings.AllowCustomersToSearchWithManufacturerName)
                {
                    productsByKeywords = productsByKeywords.Union(
                        from pm in _productManufacturerRepository.Table
                        join m in _manufacturerRepository.Table on pm.ManufacturerId equals m.Id
                        where m.Name.Contains(request.Keywords)
                        select pm.ProductId
                    );

                    if (searchLocalizedValue)
                    {
                        productsByKeywords = productsByKeywords.Union(
                        from pm in _productManufacturerRepository.Table
                        join lp in _localizedPropertyRepository.Table on pm.ManufacturerId equals lp.EntityId
                        where lp.LocaleKeyGroup == nameof(Manufacturer) &&
                              lp.LocaleKey == nameof(Manufacturer.Name) &&
                              lp.LocaleValue.Contains(request.Keywords) &&
                              lp.LanguageId == request.LanguageId
                        select pm.ProductId);
                    }
                }

                if (request.SearchProductTags)
                {
                    productsByKeywords = productsByKeywords.Union(
                        from pptm in _productTagMappingRepository.Table
                        join pt in _productTagRepository.Table on pptm.ProductTagId equals pt.Id
                        where pt.Name.Contains(request.Keywords)
                        select pptm.ProductId
                    );

                    if (searchLocalizedValue)
                    {
                        productsByKeywords = productsByKeywords.Union(
                        from pptm in _productTagMappingRepository.Table
                        join lp in _localizedPropertyRepository.Table on pptm.ProductTagId equals lp.EntityId
                        where lp.LocaleKeyGroup == nameof(ProductTag) &&
                              lp.LocaleKey == nameof(ProductTag.Name) &&
                              lp.LocaleValue.Contains(request.Keywords) &&
                              lp.LanguageId == request.LanguageId
                        select pptm.ProductId);
                    }
                }

                productsQuery =
                    from p in productsQuery
                    join pbk in productsByKeywords on p.Id equals pbk
                    select p;
            }

            if (request.CategoryIds is not null)
            {
                if (request.CategoryIds.Contains(0))
                    request.CategoryIds.Remove(0);

                if (request.CategoryIds.Any())
                {
                    var productCategoryQuery =
                        from pc in _productCategoryRepository.Table
                        where (!request.ExcludeFeaturedProducts || !pc.IsFeaturedProduct) &&
                            request.CategoryIds.Contains(pc.CategoryId)
                        group pc by pc.ProductId into pc
                        select new
                        {
                            ProductId = pc.Key,
                            DisplayOrder = pc.First().DisplayOrder
                        };

                    productsQuery =
                        from p in productsQuery
                        join pc in productCategoryQuery on p.Id equals pc.ProductId
                        orderby pc.DisplayOrder, p.Name
                        select p;
                }
            }

            if (request.ManufacturerIds is not null)
            {
                if (request.ManufacturerIds.Contains(0))
                    request.ManufacturerIds.Remove(0);

                if (request.ManufacturerIds.Any())
                {
                    var productManufacturerQuery =
                        from pm in _productManufacturerRepository.Table
                        where (!request.ExcludeFeaturedProducts || !pm.IsFeaturedProduct) &&
                            request.ManufacturerIds.Contains(pm.ManufacturerId)
                        group pm by pm.ProductId into pm
                        select new
                        {
                            ProductId = pm.Key,
                            DisplayOrder = pm.First().DisplayOrder
                        };

                    productsQuery =
                        from p in productsQuery
                        join pm in productManufacturerQuery on p.Id equals pm.ProductId
                        orderby pm.DisplayOrder, p.Name
                        select p;
                }
            }

            if (request.ProductTagId > 0)
            {
                productsQuery =
                    from p in productsQuery
                    join ptm in _productTagMappingRepository.Table on p.Id equals ptm.ProductId
                    where ptm.ProductTagId == request.ProductTagId
                    select p;
            }

            if (request.FilteredSpecOptions?.Count > 0)
            {
                var specificationAttributeIds = request.FilteredSpecOptions
                    .Select(sao => sao.SpecificationAttributeId)
                    .Distinct();

                foreach (var specificationAttributeId in specificationAttributeIds)
                {
                    var optionIdsBySpecificationAttribute = request.FilteredSpecOptions
                        .Where(o => o.SpecificationAttributeId == specificationAttributeId)
                        .Select(o => o.Id);

                    var productSpecificationQuery =
                        from psa in _productSpecificationAttributeRepository.Table
                        where psa.AllowFiltering && optionIdsBySpecificationAttribute.Contains(psa.SpecificationAttributeOptionId)
                        select psa;

                    productsQuery =
                        from p in productsQuery
                        where productSpecificationQuery.Any(pc => pc.ProductId == p.Id)
                        select p;
                }
            }
            return await productsQuery.OrderBy(_localizedPropertyRepository, await _workContext.GetWorkingLanguageAsync(), request.OrderBy).ToPagedListAsync(request.PageIndex, request.PageSize);
        }
    }
}