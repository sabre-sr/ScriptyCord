using CSharpFunctionalExtensions;
using Discord;
using Discord.Audio;
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
        private Queue<Guid> Playlist;

        public ulong GuildId { get; protected set; }

        public PlaySongEvent(IAudioClient client, DateTime timeOfExecution, IVoiceChannel channel, IGuildUser user, ulong guildId) : base(client, timeOfExecution, channel, user) 
        {
            Playlist = new Queue<Guid>();
            GuildId = guildId;
        }

        public bool ShouldBeExecutedNow()
            => DateTime.Now >= _timeOfExecution;

        public async Task<Result> ExecuteAsync()
        {
            //Guid id = Playlist.Dequeue();

            // TODO: Get metadata of song (don't store audiometadata as it will require )
            string path = "downloads/audio/YouTube-zZuIMcmNZnU.ogg";

            using (var ffmpeg = CreateStream(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = _client.CreatePCMStream(Discord.Audio.AudioApplication.Music))
            {
                try { await output.CopyToAsync(discord);  }
                catch (Exception e) { return Result.Failure(e.Message); }
                finally { await discord.FlushAsync(); }
            }

            // TODO: Schedule next event

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
