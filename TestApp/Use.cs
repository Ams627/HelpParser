using CliFx.HelpSystem;

[CommandHandlerAttribute("use")]
class Use : ICommandHandler
{
    public Task<int> ExecuteAsync(string[] args)
    {
        Console.WriteLine("IN THE USE COMMAND!");
        return Task.FromResult(0);
    }
}


[CommandHandlerAttribute("buy")]
class Buy : ICommandHandler
{
    public Task<int> ExecuteAsync(string[] args)
    {
        Console.WriteLine("IN THE BUY COMMAND!");
        return Task.FromResult(0);
    }
}
