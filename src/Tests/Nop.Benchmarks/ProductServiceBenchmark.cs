﻿using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Tests;
using Nop.Services.Customers;

namespace Nop.Benchmarks
{
    public class ProductServiceBenchmark : BaseNopTest
    {
        private IProductService _productService;
        private INopDataProvider _dataProvider;
        private IWorkContext _workContext;
        private ICustomerService _customerService;

        [GlobalSetup]
        public void Setup()
        {
            _productService = GetService<IProductService>();
            _dataProvider = GetService<INopDataProvider>();
            _workContext = GetService<IWorkContext>();
            _customerService = GetService<ICustomerService>();
        }

        [Benchmark]
        public int SearchProducts()
        {
            var p = _productService.SearchProducts(visibleIndividuallyOnly: true, storeId: 1, orderBy: ProductSortingEnum.PriceAsc);
            return p.Count;
        }

        [Benchmark]
        public int GetProducts()
        {
            var p = _dataProvider.GetTable<Product>().Where(p => p.Price > 1);
            return p.Count();
        }
        
        [Benchmark]
        public Customer GetCurrentCustomer()
        {
            return _workContext.CurrentCustomer;
        }
        
        [Benchmark]
        public int[] GetCustomerRoleIds()
        {
            return _customerService.GetCustomerRoleIds(_workContext.CurrentCustomer);
        }

        [Benchmark]
        public PagedList<Product> ToPagedList()
        {
            return new PagedList<Product>(_dataProvider.GetTable<Product>().Where(p => p.Price > 1), 0, int.MaxValue);
        }
    }
}
