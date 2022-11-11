using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using mp3_storage.Services;

namespace mp3_storage.Controllers;

[ApiController]
[Route("[controller]")]
public class DownloadController : ControllerBase
{
    //string url = "https://app.comagic.ru/system/media/talk/2457179605/16d4a16ac92ea522cdcbaf0a39bfdb60/";
    private readonly string path = 
        "/Users/aleksandr/Yandex.Disk.localized/My projects C# 2/ConsoleApp3/WebApplication/bin/Debug/net6.0/Новая папка/";

    //var res = DownloadFileAsync().GetAwaiter();
    
    //2022-10-26_11-38-03.966146_from_74991969044_to_74951753977_session_2457179605_talk.mp3

    private readonly IDownloadService _service;
    
    public DownloadController(IDownloadService service)
    {
        _service = service;
    }

    [HttpGet("DownloadFileAsync")]
    public async Task<IActionResult> DownloadFileAsync(string url)
    {
        await _service.DownloadFileAsync(url);
        return Ok();
    }
    
    [HttpGet("ConvertFromBytes")]
    public void ConvertFromBytes()
    {
        _service.ConvertFromBytes();
    }
}