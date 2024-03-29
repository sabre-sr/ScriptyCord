﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using NLog;
using ScriptCord.Core.DiscordExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptCord.Bot.Commands
{
    public class UserManagementModule : ScriptCordCommandModule
    {
        private readonly ILoggerFacade<UserManagementModule> _logger;

        public UserManagementModule(ILoggerFacade<UserManagementModule> logger, DiscordSocketClient client, IConfiguration configuration)
        {
            _logger = logger;
            _logger.SetupDiscordLogging(configuration, client, "userManagement");
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("ban", "Bans specified user from the server")]
        public async Task Ban([Summary(description: "User that will be affected by this command")] IGuildUser user, [Summary(description: "Reason for this action")] string reason = "")
        {
            await user.BanAsync(0, reason);
            _logger.LogDebug($"[GuildId({Context.Guild.Id}),ChannelId({Context.Channel.Id})]: user {user.Nickname} has been banned from the server with '{reason}' reason");
            await RespondAsync(
                reason != null ? $"User {user.Nickname} has been banned from the server with the specified reason" : $"User {user.Nickname} has been banned from the server",
                null, false, true
            );
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("kick", "Kicks specified user from the server")]
        public async Task Kick([Summary(description: "User that will be affected by this command")] IGuildUser user, [Summary(description: "Reason for this action")] string reason = "")
        {
            await user.KickAsync(reason);
            _logger.LogDebug($"[GuildId({Context.Guild.Id}),ChannelId({Context.Channel.Id})]: user {user.Nickname} has been kicked out of the server");
            await RespondAsync($"User {user.Nickname} has been kicked out of the server", null, false, true);
        }
    }
}
