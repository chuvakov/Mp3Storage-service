using Microsoft.AspNetCore.Mvc;
using mp3_storage.Services;
using mp3_storage.Services.Dto;
using Mp3Storage.Core;

namespace mp3_storage.Controllers;

[ApiController]
[Route("[controller]")]
public class GetAllController : ControllerBase
{
    private readonly Mp3StorageContext _context;
    private readonly IDownloadService _service;
    
    public GetAllController(IDownloadService service, Mp3StorageContext context)
    {
        _service = service;
        _context = context;
    }

    [HttpPost("ShowTable")]
    public async Task<IActionResult> ShowTable()
    {
        return Ok(await _service.ShowTable());
    }
}