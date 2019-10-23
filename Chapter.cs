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

        public override string ToString()
        {
            return $@"{ReleaseDate.ToShortDateString()} - {Authors} - {Title} {string.Join(' ', Tags.Select(t => $"[{t}]"))}";
        }
    }
}