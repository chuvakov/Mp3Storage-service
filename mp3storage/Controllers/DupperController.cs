using Microsoft.AspNetCore.Mvc;
using Mp3Storage.Core.Models;

namespace mp3_storage.Controllers;

[ApiController]
[Route("[controller]")]
public class DupperController : ControllerBase
{
    [HttpGet("Get")]
    public IActionResult Get()
    {
        return Ok(FileRepository.GetAll());
    }
    
    [HttpGet("Del")]
    public IActionResult Delete(int id)
    {
        FileRepository.Delete(id);
        return Ok("Ok");
    }
}