using GoKidAPI.DTO.Category.Requests;
using GoKidAPI.Services.Category;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
            private readonly ITaskCategoryService _service;
            private readonly ResponseHandler _response;

            public CategoryController(ITaskCategoryService service, ResponseHandler response)
            {
                _service = service;
                _response = response;
            }

            [HttpGet]
            public async Task<IActionResult> GetAll()
                => Ok(await _service.GetAllAsync());

            [HttpGet("{id}")]
            public async Task<IActionResult> GetById(string id)
            {
                var result = await _service.GetByIdAsync(id);
                return StatusCode((int)result.StatusCode, result);
            }

            [HttpPost]
            public async Task<IActionResult> Create([FromForm] CreateCategoryRequest request)
            {
                var result = await _service.CreateAsync(request);
                return StatusCode((int)result.StatusCode, result);
            }

            [HttpPut("{id}")]
            public async Task<IActionResult> Update(string id, [FromForm] UpdateCategoryRequest request)
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
