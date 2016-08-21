using System;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Networking.Envelopes;

namespace PokemonGo.RocketAPI
{
    public class SimpleApiFailureStrategy : IApiFailureStrategy
    {
        public Task<ApiOperation> HandleApiFailure(RequestEnvelope request, ResponseEnvelope response)
        {
            throw new NotImplementedException();
        }

        public void HandleApiSuccess(RequestEnvelope request, ResponseEnvelope response)
        {

        }
    }
}