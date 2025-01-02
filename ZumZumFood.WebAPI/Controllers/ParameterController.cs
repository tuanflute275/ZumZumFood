﻿namespace ZumZumFood.WebAPI.Controllers
{
    [ApiController]
    [Route("/api/v1/parameter")]
    [Authorize]
    public class ParameterController : ControllerBase
    {
        private readonly IParameterService _parameterService;
        public ParameterController(IParameterService parameterService)
        {
            _parameterService = parameterService;
        }

        [HttpGet]
        public async Task<ResponseObject> FindAll(string? keyword, string? sort, int page = 1)
        {
            return await _parameterService.GetAllPaginationAsync(keyword, sort, page);
        }

        [HttpGet("{id:int}")]
        public async Task<ResponseObject> FindById(int id)
        {
            return await _parameterService.GetByIdAsync(id);
        }

        [HttpPost]
        public async Task<ResponseObject> Save([FromBody] ParameterRequestModel model)
        {
            return await _parameterService.SaveAsync(model);
        }

        [HttpPut("{id}")]
        public async Task<ResponseObject> Update(int id, [FromBody] ParameterRequestModel model)
        {
            return await _parameterService.UpdateAsync(id, model);
        }

        [HttpPost("soft-delete/{id}")]
        public async Task<ResponseObject> SoftDelete(int id)
        {
            return await _parameterService.DeleteFlagAsync(id);
        }

        [HttpGet("deleted-data")]
        public async Task<ResponseObject> GetDeletedUsers()
        {
            return await _parameterService.GetDeletedListAsync();
        }

        [HttpPost("restore/{id}")]
        public async Task<ResponseObject> RestoreUser(int id)
        {
            return await _parameterService.RestoreAsync(id);
        }

        [HttpDelete("{id}")]
        public async Task<ResponseObject> Delete(int id)
        {
            return await _parameterService.DeleteAsync(id);
        }
    }
}
