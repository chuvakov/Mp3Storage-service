using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace mp3_storage.Controllers;

[ApiController]
[Route("[controller]")]
public class GetController : ControllerBase
{
    private readonly string path = 
        "/Users/aleksandr/Yandex.Disk.localized/My projects C# 2/ConsoleApp3/WebApplication/bin/Debug/net6.0/Новая папка/";
    
    string url = "https://app.comagic.ru/system/media/talk/2457179605/16d4a16ac92ea522cdcbaf0a39bfdb60/";
    
    [HttpGet(Name = "Get")]
    public ActionResult Getrecording()
    {
        using (var client = new WebClient())
        {
            var stream = client.OpenRead(url);
            return File(stream, "audio/mpeg");
        }
    }
}
