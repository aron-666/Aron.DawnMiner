using Aron.DawnMiner.Models;
using Aron.DawnMiner.Services;
using OpenQA.Selenium;
using Quartz;
using System.Drawing;
using System.Net;
using System.Xml.Linq;

namespace Aron.DawnMiner.Jobs
{
    public class ScreensShotJob(MinerRecord _minerRecord, IMinerService minerService) : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                if (minerService.driver == null)
                {
                    return Task.CompletedTask;
                }



                // 截圖
                Screenshot screenshot = ((ITakesScreenshot)minerService.driver).GetScreenshot();

                _minerRecord.Base64Image = "data:image/png;base64," + screenshot.AsBase64EncodedString;

            }
            catch (Exception e)
            {
            }
            return Task.CompletedTask;

        }




    }
}
