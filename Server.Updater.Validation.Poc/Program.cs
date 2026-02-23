using Microsoft.Extensions.DependencyInjection;
using Server.Updater.Validation.Poc.Services;

var services = new ServiceCollection();

services.AddSingleton<StagingService>();

using var serviceProvider = services.BuildServiceProvider();

_ = serviceProvider.GetRequiredService<StagingService>();

