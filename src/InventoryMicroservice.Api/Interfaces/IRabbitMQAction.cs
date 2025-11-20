namespace InventoryMicroservice.Api.Interfaces;

public interface IRabbitMQEvent
{}

public interface IRabbitMQAction<TInput>: IRabbitMQEvent
{
  Task RunActionAsync(TInput input, CancellationToken cancellationToken);
}

public interface IRabbitMQFunc<TInput, TOutput>: IRabbitMQEvent
{
  Task<TOutput> RunFuncAsync(TInput input, CancellationToken cancellationToken);
}
