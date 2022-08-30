using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScriptCord.Bot.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptCord.Bot
{
    public class IocSetup
    {
        private ServiceCollection _services;

        public IocSetup(IConfiguration configuration)
        {
            _services = new ServiceCollection();
            
            _services.AddSingleton(new DiscordSocketConfig());
            _services.AddSingleton(configuration);
            _services.AddSingleton<DiscordSocketClient>();
            _services.AddSingleton<InteractionHandler>();
            _services.AddScoped(typeof(LoggerFacade<>));
        }

        public void SetupCommandModules()
        {
            _services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));
        }

        public ServiceProvider Build()
            => _services.BuildServiceProvider();
    }
}
