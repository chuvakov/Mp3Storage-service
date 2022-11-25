using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mp3Storage.AudioDownloader.Jobs
{
    public class JobDownload
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }        
        public IEnumerable<JobDownload> ChildJobs { get; set; } 
        public bool IsExecute { get; set; } 
    }
}
