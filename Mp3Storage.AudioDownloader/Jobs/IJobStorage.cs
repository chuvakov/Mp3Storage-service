using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mp3Storage.AudioDownloader.Jobs
{
    public interface IJobStorage
    {
        void AddJob(JobDownload job);
        Task ExecuteFirstJob();

        void ChangeState(JobDownload job, JobState state);
    }
}
