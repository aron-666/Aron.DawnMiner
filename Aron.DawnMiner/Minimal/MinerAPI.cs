using Aron.DawnMiner.Models;
using Aron.DawnMiner.Services;
using Aron.DawnMiner.ViewModels;
using Aron.NetCore.Util.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Aron.DawnMiner.Minimal
{
    public static class MinerAPI
    {
        public static WebApplication UseMinerAPI(this WebApplication app)
        {

            app.MapGet("/api/Miner/GetMinerRecord", [Authorize] (MinerRecord minerRecord) =>
            {
                var options = MyJsonContext.Default.MinerRecord.Options;
                return Results.Json(minerRecord, options);
            });

            app.MapGet("/api/Miner/RefreshCaptcha", [Authorize] (IMinerService minerService) =>
            {
                ((MinerService)minerService)?.RefreshCaptcha();
                return Results.Ok();
            });

            app.MapPost("/api/Miner/ApplyCaptcha", [Authorize] async (HttpContext httpContext, IMinerService minerService) =>
            {
                var options = MyJsonContext.Default.Answer.Options;
                var answer = await httpContext.Request.ReadFromJsonAsync<Answer>(options);

                
                if (answer != null)
                {
                    await ((MinerService)minerService)?.ApplyCaptcha(answer.ans);
                    return Results.Ok();
                }
                return Results.BadRequest("Invalid request body");
            });

            return app;
        }
    }

    public class Answer
    {
        public string ans { get; set; }
    }
}
