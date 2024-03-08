using Domain.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace TDiLife_app.Controllers
{

    public class CartController : BaseApiController
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context; 
        }

        [HttpGet(Name = "Get Cart")]
        public async Task<ActionResult<CartDto>> GetCart()
        {
            var cart = await RetrieveCart(GetBuyerId());

            if (cart == null) return NotFound();
            return MapCartToDto(cart);
        }

       
        [HttpPost]
        public async Task<ActionResult<CartDto>> AddItemtoCart(int productid, int quantity)
        {
            var cart = await RetrieveCart(GetBuyerId());

            if (cart == null) cart = CreateCart();

            var product= await _context.Products.FindAsync(productid);

            if (product == null) return NotFound();

            cart.AddItem(product, quantity);

            var result = await _context.SaveChangesAsync() > 0;

            if (result) return CreatedAtRoute("Get Cart",MapCartToDto(cart));

            return BadRequest(new ProblemDetails {Title=" Problem saving item to basket" });

        }

        [HttpDelete(Name ="Delete Item from cart")]
        public async Task<ActionResult> RemoveCartItem(int productId, int quantity)
        {
            var cart= await RetrieveCart(GetBuyerId());

            if (cart == null) return NotFound();

            cart.RemoveItem(productId, quantity);

            var result= await _context.SaveChangesAsync() > 0;

            if (result) return Ok();
            return BadRequest(new ProblemDetails { Title = "Problem removing item from basket" });
        }

        private async Task<Cart> RetrieveCart(string buyerId)
        {
            if (string.IsNullOrEmpty(buyerId))
            {
                Response.Cookies.Delete("buyerId");
                return null;
            }
            return await _context.Carts
                .Include(i => i.Items)
                .ThenInclude(p => p.Product)
                .FirstOrDefaultAsync(x => x.BuyerId == Request.Cookies["buyerid"]);
        }

        private string GetBuyerId()
        {
            return User.Identity?.Name ?? Request.Cookies["buyerId"];
        }

        private Cart CreateCart()
        {
            var buyerId = User.Identity?.Name; 

            if (string.IsNullOrEmpty(buyerId))
            {
                buyerId = Guid.NewGuid().ToString();
                var cookieOptions = new CookieOptions { IsEssential = true, Expires = DateTime.Now.AddDays(30) };
                Response.Cookies.Append("buyerId", buyerId, cookieOptions);
            }

            var cart = new Cart { BuyerId = buyerId };

            _context.Carts.Add(cart);

            return cart;
        }

        private CartDto MapCartToDto(Cart cart)
        {
            return new CartDto
            {
                Id = cart.Id,
                BuyerId = cart.BuyerId,
                Items = cart.Items.Select(item => new CartItemDto
                {
                    ProductId = item.ProductId,
                    Name = item.Product.Name,
                    Price = item.Product.Price,
                    PictureUrl = item.Product.PictureUrl,
                    Type = item.Product.ProductType,
                    Brand = item.Product.ProductBrand,
                    Quantity = item.Quantity
                }).ToList()
            };
        }

    }
}
