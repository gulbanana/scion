using System;
using System.Collections.Generic;
using System.Linq;

namespace Scion
{
    class Chapter
    {
        public DateTime ReleaseDate;
        public Uri Link;
        public Uri Thumbnail;
        public string? Doujin;
        public string Authors;
        public string Title;
        public string? Series;
        public string? Subtitle;
        public IReadOnlyList<string> Tags;

        public override string ToString()
        {
            if (Doujin != null)
            {
                if (Series != null)
                {
                    return $@"{ReleaseDate.ToShortDateString()} / {Doujin} - {Authors} - {Series} / {Subtitle} / {string.Join(' ', Tags.Select(t => $"[{t}]"))}";
                }
                else
                {
                    return $@"{ReleaseDate.ToShortDateString()} / {Doujin} - {Authors} / {Title} / {string.Join(' ', Tags.Select(t => $"[{t}]"))}";
                }
            }
            else
            {
                if (Series != null)
                {
                    return $@"{ReleaseDate.ToShortDateString()} / {Authors} - {Series} / {Subtitle} / {string.Join(' ', Tags.Select(t => $"[{t}]"))}";
                }
                else
                {
                    return $@"{ReleaseDate.ToShortDateString()} / {Authors} / {Title} / {string.Join(' ', Tags.Select(t => $"[{t}]"))}";
                }
            }
        }
    }
}