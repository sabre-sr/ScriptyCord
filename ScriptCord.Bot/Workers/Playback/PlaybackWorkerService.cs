using FluentNHibernate.Conventions;
using ScriptCord.Bot.Events;
using ScriptCord.Bot.Events.Playback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptCord.Bot.Workers.Playback
{
    public class PlaybackWorker : IWorker
    {
        private readonly ILoggerFacade<PlaybackWorker> _logger;

        // TODO: Perhaps change this into a property with a mutex if there is no suitable implementation of queue for that
        public static Queue<IExecutableEvent> Events { get; } = new Queue<IExecutableEvent>();

        private Dictionary<ulong, Thread> _playbackThreadsByGuildId;

        private bool _stop = false;

        public PlaybackWorker(ILoggerFacade<PlaybackWorker> logger)
        {
            _logger = logger;
            _playbackThreadsByGuildId = new Dictionary<ulong, Thread>();
        }

        public async Task Run()
        {
            _logger.LogInfo("starting worker execution");
            while (!_stop)
            {
                while (Events.IsNotEmpty())
                {
                    int queueLength = Events.Count;
                    for (int i = 0; i < queueLength; i++)
                    {
                        var playbackEvent = Events.Dequeue();

                        if (playbackEvent.ShouldBeExecutedNow())
                        {
                            if (playbackEvent is PlaySongEvent)
                            {
                                Thread thread = new Thread(new ThreadStart(
                                    () =>
                                    {
                                        var resultTask = playbackEvent.ExecuteAsync();
                                        resultTask.Wait();
                                        var result = resultTask.Result;
                                        if (result.IsFailure)
                                            _logger.LogError(result);
                                    }
                                ));
                                _playbackThreadsByGuildId[playbackEvent.GuildId] = thread;
                                thread.Start();
                            }
                            //else
                            //{
                            //    var result = await playbackEvent.ExecuteAsync();
                            //    if (result.IsFailure)
                            //        _logger.LogError(result);
                            //}
                        }
                        else
                            Events.Enqueue(playbackEvent);
                    }
                    await Task.Delay(500);
                }
                await Task.Delay(500);
            }
        }

        public void Stop()
            => _stop = true;
    }
}
