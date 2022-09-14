using CSharpFunctionalExtensions;
using Discord;
using Discord.Audio;
using ScriptCord.Bot.Dto.Playback;
using ScriptCord.Bot.Workers.Playback;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptCord.Bot.Events.Playback
{
    public class PlaySongEvent : PlaybackEventBase, IExecutableEvent
    {
        private IList<PlaylistEntryDto> _playlist;

        public ulong GuildId { get; protected set; }

        public PlaySongEvent(IAudioClient client, DateTime timeOfExecution, IVoiceChannel channel, IGuildUser user, ulong guildId, IList<PlaylistEntryDto> playlist) : base(client, timeOfExecution, channel, user) 
        {
            _playlist = playlist;
            GuildId = guildId;
        }

        public bool ShouldBeExecutedNow()
            => DateTime.Now >= _timeOfExecution;

        public async Task<Result> ExecuteAsync()
        {
            var currentEntry = _playlist[0];
            _playlist.RemoveAt(0);
            if (_playlist.Count > 0)
            {
                DateTime playbackEndsAt = DateTime.Now.Add(TimeSpan.FromMilliseconds(currentEntry.Length + 1000));
                PlaybackWorker.Events.Enqueue(new PlaySongEvent(_client, playbackEndsAt, _channel, _user, GuildId, _playlist));
            }

            using (var ffmpeg = CreateStream(currentEntry.Path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = _client.CreatePCMStream(Discord.Audio.AudioApplication.Music))
            {
                try { await output.CopyToAsync(discord);  }
                catch (Exception e) { return Result.Failure(e.Message); }
                finally { await discord.FlushAsync(); }
            }

            return Result.Success();
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
    }
}
