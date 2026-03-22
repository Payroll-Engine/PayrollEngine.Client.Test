using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.Client.Test;

/// <summary>Test command base</summary>
/// <typeparam name="TArgs">Command parameter type</typeparam>
public abstract class TestCommandBase<TArgs> :
    TestCommandBase where TArgs : ICommandParameters
{
    /// <summary>Execute the command with typed parameters</summary>
    protected abstract Task<int> Execute(CommandContext context, TArgs parameters);

    /// <inheritdoc />
    protected override async Task<int> OnExecute(CommandContext context, ICommandParameters parameters) =>
        await Execute(context, (TArgs)parameters);
}
