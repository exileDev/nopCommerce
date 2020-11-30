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
        public async Task<int> SearchProducts()
        {
            var p = await _productService.SearchProductsAsync(0, visibleIndividuallyOnly: true, storeId: 1, orderBy: ProductSortingEnum.PriceAsc);
            return p.Count;
        }

        [Benchmark]
        public async Task<int> GetProducts()
        {
            var p = await _dataProvider.GetTableAsync<Product>();
            return p.Count(p => p.Price > 1);
        }
        
        [Benchmark]
        public async Task<Customer> GetCurrentCustomer()
        {
            await _workContext.SetCurrentCustomerAsync();
            return await _workContext.GetCurrentCustomerAsync();
        }

        [Benchmark]
        public async Task<int[]> GetCustomerRoleIds()
        {
            return await _customerService.GetCustomerRoleIdsAsync(new Customer { Id = 1 });
        }

        [Benchmark]
        public async Task<IPagedList<Product>> ToPagedList()
        {
            return await _dataProvider.GetTable<Product>().Where(p => p.Price > 1).ToPagedListAsync(0, int.MaxValue);
        }
    }
}
