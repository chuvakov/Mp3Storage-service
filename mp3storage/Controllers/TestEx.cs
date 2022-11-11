using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using mp3_storage.Services;
using Mp3Storage.Core;

namespace mp3_storage.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : Controller
    {
        private readonly Mp3StorageContext _context;


        public HomeController(Mp3StorageContext context)
        {
            _context = context;
        }

        [HttpGet("GetFile")]
        public IActionResult GetFile(int id = 1)
        {
            // Путь к файлу
            //string file_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "/Users/aleksandr/Yandex.Disk.localized/My projects C# 2/ConsoleApp3/mp3storage/Files/2022-10-26_11-38-03.966146_from_74991969044_to_74951753977_session_2457179605_talk.mp3");
            string file_path = _context.Files.FirstOrDefault(a => a.Id == id).FullPath;
            // Тип файла - content-type
            string file_type = "audio/mpeg";
            // Имя файла - необязательно
            string file_name = _context.Files.FirstOrDefault(a => a.Id == id).Name.ToString();
            return PhysicalFile(file_path, file_type, file_name);
        }
        
        // Отправка потока
        [HttpGet("GetFileStream")]
        public IActionResult GetFileStream()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "/Users/aleksandr/Yandex.Disk.localized/My projects C# 2/ConsoleApp3/mp3storage/Files/2022-10-26_11-38-03.966146_from_74991969044_to_74951753977_session_2457179605_talk.mp3");
            FileStream fs = new FileStream(path, FileMode.Open);
            string file_type = "application/octet-stream";
            string file_name = "audio2.mp3";
            return File(fs, file_type, file_name);
        }
    }
}