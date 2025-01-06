﻿namespace ZumZumFood.WebAPI.Controllers
{
    [ApiController]
    [Route("/api/v1/coupon")]
    public class CouponController : ControllerBase
    {
        private readonly ICouponService _couponService;
        public CouponController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        [HttpGet]
        public async Task<ResponseObject> FindAll(string? keyword, string? sort, int page = 1)
        {
            return await _couponService.GetAllPaginationAsync(keyword, sort, page);
        }

        [HttpGet("{id:int}")]
        public async Task<ResponseObject> FindById(int id)
        {
            return await _couponService.GetByIdAsync(id);
        }

        [HttpGet("caculate-coupon")]
        public async Task<ResponseObject> CaculateCoupon(string couponCode, double totalAmount)
        {
            return await _couponService.CalculateCouponValueAsync(couponCode, totalAmount);
        }

        [HttpPost]
        public async Task<ResponseObject> Save([FromBody] CouponModel model)
        {
            return await _couponService.SaveAsync(model);
        }

        [HttpPut("{id}")]
        public async Task<ResponseObject> Update(int id, [FromBody] CouponModel model)
        {
            return await _couponService.UpdateAsync(id, model);
        }

        //[Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ResponseObject> Delete(int id)
        {
            return await _couponService.DeleteAsync(id);
        }
    }
}
