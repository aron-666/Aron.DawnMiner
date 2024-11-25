using Aron.DawnMiner.Extensions;
using Aron.DawnMiner.Models;
using System.Text.Json.Serialization;
using Aron.NetCore.Util.Extensions;
using static Aron.NetCore.Util.Extensions.ServiceExtensions;
using Aron.DawnMiner.ViewModels;
using Aron.NetCore.Util.ViewModels;
using Aron.DawnMiner.Minimal;

namespace Aron.DawnMiner.Services
{
    [JsonSourceGenerationOptions
        (
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = new[] { typeof(JsonStringEnumConverter), typeof(DateTimeConverter) }
        )]
    [JsonSerializable(typeof(MinerRecord))]
    [JsonSerializable(typeof(Answer))]
    [JsonSerializable(typeof(ResponseResult<LoginResp>))]
    [JsonSerializable(typeof(RequestResult<LoginReq>))]
    [JsonSerializable(typeof(ResponseResult<string>))]
    public partial class MyJsonContext : JsonSerializerContext
    {
        
    }
}
