using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Controllers;

[Authorize]
[ApiController]
[Route("api/storage")]
[EnableRateLimiting("GeneralApiPolicy")]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;

    public StorageController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost("upload")]
    [EnableRateLimiting("UploadPolicy")]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string? folder = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No se proporcionó ningún archivo." });
        }

        var targetFolder = string.IsNullOrWhiteSpace(folder) ? "perfiles" : folder.Trim();
        var url = await _storageService.UploadFileAsync(file, targetFolder);
        return Ok(new { url });
    }
}

