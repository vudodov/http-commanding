[![Nuget](https://img.shields.io/nuget/v/http-commanding-middleware)](https://www.nuget.org/packages/http-commanding-middleware/)
# HTTP Commanding Middleware

This is an easy-to-hook-up high-performance middleware for Web Applications aiming to implement the CQRS pattern. The Middleware provides tooling that will make your Web Application set up seamless and development efficient.

The [Querying middleware](https://github.com/vudodov/http-querying) is available as a separate package. Take the adventage of the physical segregation. E.g. scale commands and queries independently. 

## Usage
All you need to do is register the middleware in your [middleware pipeline](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1), add some queries and that's it.

Spin up the project and hit the localhost with `command/<command-name>` add command data to the HTTP Request body and your command will be deserialized and delivered directly to the handler, once the command is executed success or failure result is formed and delivered back to you. For your convenience, you can find a ready-to-go playground inside the repository.

## The Playground
Inside of the repository you can find [the Playground](https://github.com/vudodov/http-commanding/tree/master/HttpCommanding.Playground). Which is essentially a web application with a couple of preset commands and handlers for you to get quick hands-on experience.

And [Some Tests](https://github.com/vudodov/http-commanding/tree/master/HttpCommanding.Playground.Tests) that will spin up a test application server and emulate client requests.

## Under the hood

The framework consists of two main packages [the infrastructure](https://github.com/vudodov/http-commanding/packages/126018) and [the middleware](https://github.com/vudodov/http-commanding/packages/126019).

### The Infrastructure

#### Commands and Handlers

The Infrastructure provides an `ICommand` interface to identify your command data class
```csharp
public class BakePotatoesCommand : ICommand
{
    public int Amount { get; set; }
    public int BakingTemperature { get; set; }
}
```
And `ICommandHandler<TCommand>` interface to tie together command data and command handling functionality. The rest will be done automatically.
```csharp
public class BakePotatoesCommandHandler : ICommandHandler<BakePotatoesCommand>
{
    private readonly IOvent _oven;
    
    public BakePotatoesCommandHandler(IOven oven)
    {
        _oven = oven;
    }
    
    public Task<CommandResult> HandleAsync(BakePotatoesCommand command, Guid commandId, CancellationToken token)
    {
        if(_oven.Temperature == command.BakingTemperature)
        {
            await _oven.BakePotatoesAsync(command.Amount, token);
            return CommandResult.Succeed();
        }
        
        return CommandResult.Failure("Oven temperature is not matching requested baking temeprature.");
    }
}
```

Once the command will hit the middleware it will be delivered to the proper handler. In the example above, once the `BakePotatoesCommand` is received and the oven temperature is matching the requested baking temperature, the handler will bake potatoes and notify the caller that the requested command was successfully executed. If the temperature condition was not met, the handler will notify the caller that the requested action failed and that the reason for that is the oven's temperature.

### The Middleware

The middleware is very easy to set up. Just register the commanding middleware in the `Startup.cs` file. And you are ready to go. 
```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddHttpCommanding();
    ...
}

...

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    ...
    app.UseHttpCommanding();
    ...
}
```

#### Command Registry

By default, the Middleware will register all the commands and handlers in your current web project. If you'd like to store commands and handlers in other projects, you can easily configure that by passing assemblies where those commands and handlers are defined. Just as the following example does.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    var potatoesAssembly = typeof(BakePotatoesCommand).Assembly;
    var beerAssembly = typeof(BrewBeerCommand).Assembly;
    
    services.AddHttpCommanding(potatoesAssembly, beerAssembly);
    ...
}
```

#### Sending Commands

To hit the middleware with a command all you need is [properly constructed HTTP request](
https://valerii-udodov.com/cqrs-commanding-via-http/). 
The request should have 
 - a `POST` request type;
 - URI as following `http://<your-host>/command/<command-name>`;
 - `content-type` header set to `application/json`;
 Don't forget to include command payload in the request body ;)

![command-processing-image](/images/command-request-response-flow.png)

#### Getting Available Commands

At any point in time, you can retrieve a list of available commands with [JSON Schema definition](https://valerii-udodov.com/2019/06/24/making-strongly-typed-language-a-bit-more-loose-with-json-schema/) of the commands' payload. This information might be used to verify the command payload before sending it. To do so, just fire the `GET` request to the endpoint `HTTP/2.0 GET /command`.

The Middleware supports [If-None-Match](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-None-Match) header.

And the response will contain [ETag](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/ETag).

#### Dependency Injection

Dependency injection for command handlers works out of the box as you would expect it to.
Everything that you've registered during application startup will be available in all your handlers via dependency injection, in the same way as it would be if you'd use controllers.

#### Command Id

For tracing purposes, each command handler will receive a command id. The same command id will be added to the HTTP response automatically.

#### Responses

In case of success, the caller will recieve `200 OK` Response with command id.

In case of failure, the caller will recieve `409 Conflict` Response with conflict reasons and command id.

__________________________
Any questions or problems, just add an issue.
