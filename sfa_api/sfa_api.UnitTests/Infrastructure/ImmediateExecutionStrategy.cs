using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace sfa_api.UnitTests.Infrastructure;

/// <summary>
/// Test double for <see cref="IExecutionStrategy"/> that runs the operation exactly once, inline — the
/// unit-test equivalent of the real non-retrying strategy. Services wrap their transactional cascade in
/// <c>repo.CreateExecutionStrategy().ExecuteAsync(...)</c> (required in production because
/// EnableRetryOnFailure is on); with a mocked repository there is no DbContext to hand back a real
/// strategy, so tests return this. The <see cref="DbContext"/> argument is unused by the
/// <c>ExecuteAsync(Func&lt;Task&gt;)</c> overload the services call, so passing null is safe.
/// </summary>
public sealed class ImmediateExecutionStrategy : IExecutionStrategy
{
    public bool RetriesOnFailure => false;

    public TResult Execute<TState, TResult>(
        TState state,
        Func<DbContext, TState, TResult> operation,
        Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
        => operation(null!, state);

    public Task<TResult> ExecuteAsync<TState, TResult>(
        TState state,
        Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
        Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
        CancellationToken cancellationToken = default)
        => operation(null!, state, cancellationToken);
}
