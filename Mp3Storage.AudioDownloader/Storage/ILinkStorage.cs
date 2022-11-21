using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mp3Storage.AudioDownloader.Storage
{
    public interface ILinkStorage
    {
        void Add(string link);
        string[] GetLinksNotExist(string[] links);
    }
}
