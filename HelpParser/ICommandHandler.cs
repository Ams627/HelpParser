namespace CliFx.HelpSystem;
using System.Threading.Tasks;

public interface ICommandHandler
{
    Task<int> ExecuteAsync(string[] args);
}
