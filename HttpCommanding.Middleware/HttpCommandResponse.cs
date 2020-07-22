using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace HttpCommanding.Middleware
{
    public abstract class HttpCommandResponse
    {
        protected Guid _commandId;
        protected HttpStatusCode _responseCode;
        
        public Guid CommandId => _commandId;
        [JsonIgnore] public HttpStatusCode ResponseCode => _responseCode;

        public static HttpCommandResponse CreatedResponse(Infrastructure.CommandResult commandResult, Guid commandId)
        {
            return commandResult is Infrastructure.Succeed
                ? (HttpCommandResponse) new HttpCommandSucceedResponse(commandId)
                : (HttpCommandResponse) new HttpCommandRejectedResponse(commandId,
                    ((Infrastructure.Failed) commandResult).Reasons);
        }
    }

    public sealed class HttpCommandSucceedResponse : HttpCommandResponse
    {
        internal HttpCommandSucceedResponse(Guid commandId)
        {
            _commandId = commandId;
            _responseCode = HttpStatusCode.OK;
        }
    }

    public sealed class HttpCommandRejectedResponse : HttpCommandResponse
    {
        internal HttpCommandRejectedResponse(Guid commandId, IList<string> reasons)
        {
            _commandId = commandId;
            Reasons = reasons;
            _responseCode = HttpStatusCode.Conflict;
        }

        public IList<string> Reasons { get; }
    }
}