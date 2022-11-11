using Mp3Storage.Core;

namespace mp3_storage;

public class FileService
{
    private const string Path3 =
        "/Users/aleksandr/Yandex.Disk.localized/My projects C# 2/ConsoleApp3/WebApplication/bin/Debug/net6.0/Новая папка/2022-10-26_11-38-03.966146_from_74991969044_to_74951753977_session_2457179605_talk.mp3";
    
    
    public byte[] StreamTst()
    {
        var path = 
            "/Users/aleksandr/Yandex.Disk.localized/My projects C# 2/ConsoleApp3/WebApplication/bin/Debug/net6.0/Новая папка/";
    
        var filename = "2022-10-26_11-38-03.966146_from_74991969044_to_74951753977_session_2457179605_talk.mp3";
        
        //byte[] file = new byte[mp3.Length];

        byte[] file = null;
        
        file = File.ReadAllBytes(path + filename);
        
        File.WriteAllBytes(path + "chutest.mp3", file);

        return file;
        
        //return File(file,"audio/mpeg");
    }

    public Uri GetUri()
    {
        return new Uri(Path3, UriKind.Relative);
    }
    
    
}