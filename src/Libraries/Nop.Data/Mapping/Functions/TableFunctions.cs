using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.SqlQuery;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Functions
{
    public class TableFunctions
    {
        private readonly IDataContext _ctx;

        public TableFunctions(IDataContext ctx)
        {
            _ctx = ctx;
        }

        private (Type[] types, object[] dataParams) PrepareParams(DataParameter[] parameters)
        {
            var paramsIn = parameters.Where(p => p.Direction != ParameterDirection.Output);

            return (paramsIn.Select(p => 
                p.Value is DBNull && !SqlDataType.GetDataType(p.DataType).Type.IsClass ? typeof(Nullable<>).MakeGenericType(SqlDataType.GetDataType(p.DataType).Type) :
                SqlDataType.GetDataType(p.DataType).Type
            ).ToArray(), paramsIn.Select(p => p.Value is DBNull ? null : p.Value).ToArray());

        }

        public IList<T> QueryFunc<T>(string functionName, params DataParameter[] parameters)
        {

            var (types, values) = PrepareParams(parameters);

            var methodInfo = typeof(TableFunctions).GetMethod(functionName, types);

            var d = methodInfo.GetGenericArguments().FirstOrDefault();

            var table = _ctx.GetTable<TableFunctionResult>(this, methodInfo, values);
            
            var s = table.FirstOrDefault();

            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true
            };
            if(s.Entities is null)
                return Enumerable.Empty<T>().ToList();
                
            return JsonSerializer.Deserialize<IList<T>>(s.Entities, options);
        }

        public T ExecuteFunc<T>(string functionName, params DataParameter[] parameters)
        {
            var (types, values) = PrepareParams(parameters);
            var methodInfo = typeof(TableFunctions).GetMethod(functionName, types);
            
            var action = DelegateBuilder.BuildDelegate<T>(methodInfo, values);
            var result = _ctx.Select(action);

            return default;
        }

        [Sql.TableFunction("\"ProductLoadAllPaged\"")]
        public ITable<TableFunctionResult> ProductLoadAllPaged(
            string categoryids,
            int manufacturerid,
            int storeid,
            int vendorid,
            int warehouseid,
            int? producttypeid,
            bool visibleindividuallyonly,
            bool markedasnewonly,
            int producttagid,
            bool? featuredproducts,
            decimal? pricemin,
            decimal? pricemax,
            string keywords,
            bool searchdescriptions,
            bool searchmanufacturerpartnumber,
            bool searchsku,
            bool searchproducttags,
            bool usefulltextsearch,
            int fulltextmode,
            string filteredspecs,
            int languageid,
            int orderby,
            string allowedcustomerroleids,
            int pageindex,
            int pagesize,
            bool showhidden,
            bool? overridepublished,
            bool loadfilterablespecificationattributeoptionids
        )
        {
            throw new InvalidOperationException();
        }

        [Sql.TableFunction("\"ProductTagCountLoadAll\"")]
        public ITable<TableFunctionResult> ProductTagCountLoadAll(int storeId,string allowedCustomerRoleIds)
        {
            throw new InvalidOperationException();
        }

        [Sql.Function("\"DeleteGuests\"", ServerSideOnly = true)]
        public static int DeleteGuests(bool onlyWithoutShoppingCart, DateTime? createdFromUtc, DateTime? createdToUtc)
        {
            throw new InvalidOperationException();
        }

        #region Nested classes

        public class TableFunctionResult
        {
            public string Entities { get; set; }
            public int TotalRecords { get; set; }
            public string FilterableSpecificationAttributeOptionIds { get; set; }
        }

        #endregion
    }
}