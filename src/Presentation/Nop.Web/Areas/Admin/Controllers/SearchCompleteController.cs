using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Products.Queries;
using Nop.Services.Security;

namespace Nop.Web.Areas.Admin.Controllers
{
    public class SearchCompleteController : BaseAdminController
    {
        #region Fields

        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public SearchCompleteController(
            IMediator mediator,
            IPermissionService permissionService,
            IWorkContext workContext)
        {
            _mediator = mediator;
            _permissionService = permissionService;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        public virtual async Task<IActionResult> SearchAutoComplete(string term)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.AccessAdminPanel))
                return Content(string.Empty);

            const int searchTermMinimumLength = 3;
            if (string.IsNullOrWhiteSpace(term) || term.Length < searchTermMinimumLength)
                return Content(string.Empty);

            //a vendor should have access only to his products
            var currentVendor = await _workContext.GetCurrentVendorAsync();
            var vendorId = 0;
            if (currentVendor != null)
            {
                vendorId = currentVendor.Id;
            }

            //products
            const int productNumber = 15;
            var products = await _mediator.Send(new SearchProductsQuery
            {
                PageIndex = 0,
                VendorId = vendorId,
                Keywords = term,
                PageSize = productNumber,
                ShowHidden = true
            });

            var result = (from p in products
                          select new
                          {
                              label = p.Name,
                              productid = p.Id
                          }).ToList();

            return Json(result);
        }

        #endregion
    }
}