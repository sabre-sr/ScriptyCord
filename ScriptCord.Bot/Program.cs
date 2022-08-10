using Autofac;
using Autofac.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using ScriptCord.Bot.Commands;

namespace ScriptCord.Bot
{ 
    class Bot
    {
        private IContainer Container { get; set; }

        public static void Main(string[] args)
            => new Bot().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            IConfiguration configuration = SetupConfiguration();
            Container = SetupAutofac();

            using (var scope = Container.BeginLifetimeScope())
            {
                var client = scope.Resolve<DiscordSocketClient>();
                client.Log += LogAsync;

                // TODO: This below could go into the constructor perhaps once the logging is moved to a class
                scope.Resolve<CommandService>().Log += LogAsync;

                // Do not accidentally upload an API token ;) 
                await client.LoginAsync(TokenType.Bot, configuration.GetSection("discord").GetSection("token").Get<string>(), true);
                await client.StartAsync();

                await scope.Resolve<ICommandHandlingService>().InitializeAsync();
                await Task.Delay(Timeout.Infinite);
            }
        }

        public IContainer SetupAutofac()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<AutofacServiceProvider>().As<IServiceProvider>();
            builder.RegisterType<DiscordSocketClient>().As<DiscordSocketClient>().SingleInstance();
            builder.RegisterType<CommandService>().As<CommandService>().SingleInstance();
            builder.RegisterType<HttpClient>().As<HttpClient>();

            // Set up all the command modules automatically
            builder.RegisterAssemblyTypes(typeof(ModuleBase<SocketCommandContext>).Assembly)
                .Where(t => t.Name.EndsWith("Module"))
                .AsImplementedInterfaces();

            builder.RegisterType<CommandHandlingService>().As<ICommandHandlingService>();
            return builder.Build();
        }

        private IConfiguration SetupConfiguration()
        {
            string? env = Environment.GetEnvironmentVariable("ENVIRONMENT_TYPE");
            string inject = env != null ? $".{env}" : string.Empty;
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile($"appsettings{ inject }.json")
                .Build();
            return config;
        }

        private Task LogAsync(LogMessage log)
        {
            // TODO: normal logger for this
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}