using Autofac;
using Autofac.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using NLog;
using ScriptCord.Bot.Commands;

namespace ScriptCord.Bot
{ 
    class Bot
    {
        //private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private ILoggerFacade<Bot> _loggerFacade;

        private IContainer _container { get; set; }

        public static void Main(string[] args)
            => new Bot().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            IConfiguration configuration = SetupConfiguration();
            _container = SetupAutofac();
            _loggerFacade = SetupLogging();

            using (var scope = _container.BeginLifetimeScope())
            {
                _loggerFacade.Log(LogLevel.Info, "created an autofac lifetime scope for modules and services");

                var client = scope.Resolve<DiscordSocketClient>();
                client.Log += new LoggerFacade<DiscordSocketClient>().LogAsync;
                scope.Resolve<CommandService>().Log += new LoggerFacade<CommandService>().LogAsync;

                #region loggers
                scope.Resolve<ILoggerFacade<TestingModule>>();
                #endregion

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
            builder.RegisterGeneric(typeof(LoggerFacade<>)).As(typeof(ILoggerFacade<>)).InstancePerDependency();
            builder.RegisterAssemblyTypes(typeof(ModuleBase<SocketCommandContext>).Assembly)
                .Where(t => t.Name.EndsWith("Module"));

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

        private ILoggerFacade<Bot> SetupLogging()
        {
            var config = new NLog.Config.LoggingConfiguration();
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "logfile.txt" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            NLog.LogManager.Configuration = config;

            return new LoggerFacade<Bot>();
        }
    }
}