using System;
using System.Collections;
using System.Net;
using System.Text.Json.Serialization;

namespace HttpCommanding.Middleware
{
    public abstract class HttpCommandResponse
    {
        protected Guid _commandId;
        public Guid CommandId => _commandId;

        protected HttpStatusCode _responseCode;
        [JsonIgnore] public HttpStatusCode ResponseCode => _responseCode;

        public static HttpCommandResponse CreatedResult(Infrastructure.CommandResult commandResult, Guid commandId)
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
            _responseCode = HttpStatusCode.Accepted;
        }
    }

    public sealed class HttpCommandRejectedResponse : HttpCommandResponse
    {
        internal HttpCommandRejectedResponse(Guid commandId, IEnumerable reasons)
        {
            _commandId = commandId;
            Reasons = reasons;
            _responseCode = HttpStatusCode.Forbidden;
        }

        public IEnumerable Reasons { get; }
    }
}