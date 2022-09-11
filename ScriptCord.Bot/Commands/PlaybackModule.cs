﻿using AngleSharp.Dom;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using ScriptCord.Bot.Dto.Playback;
using ScriptCord.Bot.Repositories;
using ScriptCord.Bot.Repositories.Playback;
using ScriptCord.Bot.Services.Playback;
using ScriptCord.Core.DiscordExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using YoutubeExplode;

namespace ScriptCord.Bot.Commands
{
    [Group("playback", "Manages and plays audio in voice channels")]
    public class PlaybackModule : ScriptCordCommandModule
    {
        private readonly Discord.Color _modulesEmbedColor = Discord.Color.DarkRed;
        private readonly ILoggerFacade<PlaybackModule> _logger;
        //private readonly YoutubeClient _client;

        private readonly IPlaylistService _playlistService;
        private readonly IPlaylistEntriesService _playlistEntriesService;

        public PlaybackModule(ILoggerFacade<PlaybackModule> logger, IPlaylistService playlistService, IPlaylistEntriesService playlistEntriesService)
        {
            _logger = logger;
            //_client = new YoutubeClient();

            _playlistService = playlistService;
            _playlistEntriesService = playlistEntriesService;
        }

        #region PlaylistManagement
        
        //[RequireUserPermission(ChannelPermission.Connect)]
        //[RequireUserPermission(ChannelPermission.Speak)]
        [SlashCommand("list-entries", "Lists entries of a given playlist")]
        // [Choice("playlists", "entries")]
        public async Task ListEntries([Summary(description: "Name of the playlist")] string playlistName)
        {
            _logger.LogDebug($"[GuildId({Context.Guild.Id}),ChannelId({Context.Channel.Id})]: Listing entries in {playlistName} playlist");

            var playlistResult = await _playlistService.GetPlaylistDetails((long)Context.Guild.Id, playlistName, IsUserGuildAdministrator());
            if (playlistResult.IsFailure)
            {
                await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithColor(_modulesEmbedColor)
                        .WithTitle("Failure")
                        .WithDescription($"Failed to read playlist's data: {playlistResult.Error}")
                        .Build()
                );
            }

            if (playlistResult.Value.SongCount == 0)
            {
                await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithColor(_modulesEmbedColor)
                        .WithTitle($"{playlistName} entries")
                        .WithDescription($"No entries in this playlist")
                        .Build()
                );
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                int index = 1;
                foreach (var pair in playlistResult.Value.NewestFifteenAudioClips.Select(x => new { Index = index, Entry = x }))
                {
                    sb.AppendLine($"**{pair.Index}**. '{pair.Entry.Title}' (ID: {pair.Entry.Id}, {pair.Entry.Source} ID: {pair.Entry.SourceId}, Length: {pair.Entry.AudioLength})");
                    index++;
                }

                if (playlistResult.Value.SongCount >= 15)
                    sb.AppendLine("...");

                await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithColor(_modulesEmbedColor)
                        .WithTitle($"{playlistName} entries")
                        .WithDescription(sb.ToString())
                        .Build()
                );
            }
        }

        [SlashCommand("list-playlists", "Lists playlists")]
        public async Task ListPlaylists()
        {
            _logger.LogDebug($"[GuildId({Context.Guild.Id}),ChannelId({Context.Channel.Id})]: Listing guild's playlists");
            Result<IEnumerable<LightPlaylistListingDto>> playlistsResult = await _playlistService.GetPlaylistDetailsByGuildIdAsync((long)Context.Guild.Id);
            if (playlistsResult.IsFailure)
            {
                await RespondAsync(embed: new EmbedBuilder()
                     .WithTitle($"{Context.Guild.Name}'s Playlists")
                     .WithColor(_modulesEmbedColor)
                     .WithDescription(playlistsResult.Error)
                     .WithCurrentTimestamp().Build()
                 );
                return;
            }

            IEnumerable<LightPlaylistListingDto> playlists = playlistsResult.Value;
            StringBuilder sb = new StringBuilder();
            int count = 1;
            foreach(var playlist in playlists)
                sb.Append($"**{count}. {playlist.Name}**: {playlist.SongCount} song{ (playlist.SongCount > 1 ? "s" : "") }");

            EmbedBuilder builder = new EmbedBuilder()
                    .WithTitle($"{Context.Guild.Name}'s Playlists")
                    .WithColor(_modulesEmbedColor)
                    .WithCurrentTimestamp();
            if (playlists.Count() > 0)
                builder.WithDescription(sb.ToString());
            else
                builder.WithDescription("no playlists are registered in this server");

            await RespondAsync(embed: builder.Build());
        }

        [SlashCommand("create-playlist", "Creates a playlist with the given name")]
        public async Task CreatePlaylist([Summary(description: "Name of the playlist")] string name)
        {
            // TODO: check if user is from "premium" users that can create multiple playlists
            _logger.LogDebug($"[GuildId({Context.Guild.Id}),ChannelId({Context.Channel.Id})]: Creating a playlist");
            var isPremiumUser = true;

            var result = await _playlistService.CreateNewPlaylist((long)Context.Guild.Id, name, isPremiumUser);
            if (result.IsSuccess)
            {
                await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithColor(_modulesEmbedColor)
                        .WithTitle("Success")
                        .WithDescription($"Created a new playlist called {name}.")
                        .Build()
                );
            }
            else
            {
                await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithColor(_modulesEmbedColor)
                        .WithTitle("Failure")
                        .WithDescription($"Failed to create a playlist: {result.Error}")
                        .Build()
                );
            }
        }

        [SlashCommand("rename-playlist", "Renames the selected playlist")]
        public async Task RenamePlaylist([Summary(description: "Old name of the playlist")] string oldName, string newName)
        {
            _logger.LogDebug($"[GuildId({Context.Guild.Id}),ChannelId({Context.Channel.Id})]: Renaming a playlist");
            var result = await _playlistService.RenamePlaylist((long)Context.Guild.Id, oldName, newName, IsUserGuildAdministrator());
            if (result.IsSuccess)
            {
                await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithColor(_modulesEmbedColor)
                        .WithTitle("Success")
                        .WithDescription($"Renamed the specified playlist.")
                        .Build()
                );
            }
            else
            {
                await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithColor(_modulesEmbedColor)
                        .WithTitle("Failure")
                        .WithDescription($"Failed to rename the specified playlist: {result.Error}")
                        .Build()
                );
            }
        }

        [SlashCommand("remove-playlist", "Removes the selected playlist")]
        public async Task RemovePlaylist([Summary(description: "Name of the playlist")] string name)
        {
            _logger.LogDebug($"[GuildId({Context.Guild.Id}),ChannelId({Context.Channel.Id})]: Removing a playlist");
            var result = await _playlistService.RemovePlaylist((long) Context.Guild.Id, name, IsUserGuildAdministrator());
            if (result.IsSuccess)
            {
                await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithColor(_modulesEmbedColor)
                        .WithTitle("Success")
                        .WithDescription($"Successfully deleted playlist '{name}'.")
                        .Build()
                );
            }
            else
            {
                await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithColor(_modulesEmbedColor)
                        .WithTitle("Failure")
                        .WithDescription($"Failed to remove the specified playlist: {result.Error}")
                        .Build()
                );
            }
        }

        #endregion PlaylistManagement

        #region EntriesManagement

        [SlashCommand("add-entry", "Adds a new entry to the specified playlist")]
        public async Task AddEntry([Summary(description: "Name of the playlist")] string playlistName, [Summary(description: "Link to the video or audio")] string url)
        {
            _logger.LogDebug($"[GuildId({Context.Guild.Id}),ChannelId({Context.Channel.Id})]: Adding an entry to a playlist");
            var result = await _playlistEntriesService.AddEntryFromUrlToPlaylistByName((long) Context.Guild.Id, playlistName, url);
            EmbedBuilder builder = new EmbedBuilder().WithColor(_modulesEmbedColor);

            if (result.IsSuccess)
            {
                var metadata = result.Value;
                builder.WithTitle("Success")
                    .WithThumbnailUrl(metadata.Thumbnail)
                    .WithDescription($"Successfully added '{metadata.Title}' from {metadata.SourceType} to '{playlistName}'.");
            }
            else
            {
                builder.WithTitle("Failure")
                    .WithDescription($"Failed to add a new entry to the playlist: {result.Error}!");
            }

            await RespondAsync(embed: builder.Build());
        }

        [SlashCommand("remove-entry", "Removes an entry from the specified playlist")]
        public async Task RemoveEntry([Summary(description: "Name of the playlist")] string playlistName, [Summary(description: "Name of the entry")] string entryName)
        {
            _logger.LogDebug($"[GuildId({Context.Guild.Id}),ChannelId({Context.Channel.Id})]: Removing an entry from a playlist");
            var result = await _playlistEntriesService.RemoveEntryFromPlaylistByName((long)Context.Guild.Id, playlistName, entryName);
            EmbedBuilder builder = new EmbedBuilder().WithColor(_modulesEmbedColor);

            if (result.IsSuccess)
            {
                var metadata = result.Value;
                builder.WithTitle("Success")
                    .WithThumbnailUrl(metadata.Thumbnail)
                    .WithDescription($"Successfully removed '{metadata.Title}' from '{playlistName}'.");
            }
            else
            {
                builder.WithTitle("Failure")
                    .WithDescription($"Failed to remove an entry from the playlist: {result.Error}!");
            }

            await RespondAsync(embed: builder.Build());
        }

        #endregion EntriesManagement
    }
}