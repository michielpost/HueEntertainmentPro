using HueLightDJ.Services.Interfaces.Models;
using HueLightDJ.Services.Interfaces.Models.Requests;
using ProtoBuf.Grpc;
using System.ServiceModel;
using System.Threading.Tasks;

namespace HueLightDJ.Services.Interfaces
{
  [ServiceContract]
  public interface ILightDJService
  {
    [OperationContract]
    Task Connect(GroupConfiguration config, CallContext context = default);

    [OperationContract]
    Task<StatusModel> GetStatus(CallContext context = default);

    [OperationContract]
    Task<EffectsVM> GetEffects(CallContext context = default);

    [OperationContract]
    Task StartEffect(StartEffectRequest request, CallContext context = default);

    [OperationContract]
    Task StartGroupEffect(StartEffectRequest request, CallContext context = default);

    [OperationContract]
    Task IncreaseBPM(IntRequest value, CallContext context = default);

    [OperationContract]
    Task SetBPM(IntRequest value, CallContext context = default);

    [OperationContract]
    Task SetBri(DoubleRequest value, CallContext context = default);

    [OperationContract]
    Task StartRandom(CallContext context = default);

    [OperationContract]
    Task StartAutoMode(CallContext context = default);

    [OperationContract]
    Task StopAutoMode(CallContext context = default);

    [OperationContract]
    Task SetAutoRandomMode(BoolRequest value, CallContext context = default);

    [OperationContract]
    Task StopEffects(CallContext context = default);

    //void SetColors(string[,] matrix);

    //void SetColorsList(List<List<string>> matrix);

    [OperationContract]
    Task Beat(CallContext context = default);

    [OperationContract]
    Task Disconnect(CallContext context = default);
  }
}
