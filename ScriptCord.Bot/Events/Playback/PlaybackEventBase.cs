using Discord;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptCord.Bot.Events.Playback
{
    public class PlaybackEventBase
    {
        protected IAudioClient _client;

        protected DateTime _timeOfExecution;

        protected IVoiceChannel _channel;

        protected IGuildUser _user;

        public PlaybackEventBase(IAudioClient client, DateTime timeOfExecution, IVoiceChannel channel, IGuildUser user)
        {
            _client = client;
            _timeOfExecution = timeOfExecution;
            _channel = channel;
            _user = user;
        }
    }
}
