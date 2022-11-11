using System.Text;
using System.IO;
using Microsoft.AspNetCore.Mvc;


namespace mp3_storage.Controllers;

[ApiController]
[Route("[controller]")]
public class Get2Controller : ControllerBase
{
    [HttpGet("StreamGet")]
    public ActionResult Stream()
    {
        var get = new FileService();
        return Ok(get.StreamTst());
    }
    
    [HttpPost("GetUri")]
    public Uri GetUri()
    {
        var get = new FileService();
        return get.GetUri();
    }
}
