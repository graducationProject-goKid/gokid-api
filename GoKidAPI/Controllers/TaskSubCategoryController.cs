using GoKidAPI.DTO.Category.Requests;
using GoKidAPI.Services.SubCategory;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskSubCategoryController : ControllerBase
    {
        private readonly ITaskSubCategoryService _service;
        private readonly ResponseHandler _response;

        public TaskSubCategoryController(ITaskSubCategoryService service, ResponseHandler response)
        {
            _service = service;
            _response = response;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(string categoryId)
        {
            var result = await _service.GetByCategoryIdAsync(categoryId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _service.GetByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateSubCategoryRequest request)
        {
            var result = await _service.CreateAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromForm] UpdateSubCategoryRequest request)
        {
            var result = await _service.UpdateAsync(id, request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
