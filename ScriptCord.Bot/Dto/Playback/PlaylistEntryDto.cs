using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptCord.Bot.Dto.Playback
{
    public class PlaylistEntryDto
    {
        public string Title { get; protected set; }
        
        public long Length { get; protected set; }

        public string Path { get; protected set; }

        public PlaylistEntryDto(string title, long length, string path)
        {
            Title = title;
            Length = length;
            Path = path;
        }
    }
}
