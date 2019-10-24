#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scion
{
    class Chapter
    {
        public DateTime ReleaseDate;
        public Uri Link;
        public string Title;
        public string Authors;
        public Uri Thumbnail;
        public IReadOnlyList<string> Tags;
        public string Doujin;

        public override string ToString()
        {
            if (Doujin != null)
            {
                return $@"{ReleaseDate.ToShortDateString()} - {Doujin} - {Authors} - {Title} {string.Join(' ', Tags.Select(t => $"[{t}]"))}";
            }
            else
            {
                return $@"{ReleaseDate.ToShortDateString()} - {Authors} - {Title} {string.Join(' ', Tags.Select(t => $"[{t}]"))}";
            }
        }
    }
}