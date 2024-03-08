using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System.Text.Json;
using TDiLife_app.Extensions;
using TDiLife_app.RequestHelpers;

namespace TDiLife_app.Controllers
{
    
    public class ProductsController : BaseApiController
    {
        private readonly AppDbContext _context;
        public ProductsController(AppDbContext context)
        {
            this._context = context;
        }

        [HttpGet]
        public async  Task<ActionResult<PagedList<Product>>> GetProducts([FromQuery]ProductParams productParams)
        {
            var query=_context.Products
                
                .Sort(productParams.OrderBy)
                .Search(productParams.SearchTerm)
                .Filter(productParams.Brands, productParams.Types)
                .AsQueryable();


            var products = await PagedList<Product>.ToPagedList(query,productParams.PageNumber,productParams.PageSize);

            Response.AddPaginationHeader(products.MetaData);

            return products;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product= await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return product;
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetFilters()
        {
            var brands= await _context.Products.Select(p=>p.ProductBrand).ToListAsync();
            var types=await _context.Products.Select(p=>p.ProductType).ToListAsync();

            return Ok(new { brands, types });
        }
    } 
}  
