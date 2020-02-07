using System;
using System.Collections;
using System.Net;
using System.Text.Json.Serialization;

namespace HttpCommanding.Middleware
{
    public abstract class HttpCommandResult
    {
        protected Guid _commandId;
        protected Guid CommandId => _commandId;

        protected HttpStatusCode _responseCode;
        [JsonIgnore] public HttpStatusCode ResponseCode => _responseCode;

        public static HttpCommandResult CreatedResult(Infrastructure.CommandResult commandResult, Guid commandId)
        {
            return commandResult is Infrastructure.Success
                ? (HttpCommandResult) new Succeed(commandId)
                : (HttpCommandResult) new Rejected(commandId,
                    ((Infrastructure.Failure) commandResult).Reasons);
        }
    }

    public sealed class Succeed : HttpCommandResult
    {
        internal Succeed(Guid commandId)
        {
            _commandId = commandId;
            _responseCode = HttpStatusCode.Accepted;
        }
    }

    public sealed class Rejected : HttpCommandResult
    {
        internal Rejected(Guid commandId, IEnumerable reasons)
        {
            _commandId = commandId;
            Reasons = reasons;
            _responseCode = HttpStatusCode.Forbidden;
        }

        public IEnumerable Reasons { get; }
    }
}