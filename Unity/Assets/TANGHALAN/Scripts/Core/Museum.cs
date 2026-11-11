// The museum's rooms and works, as data. The values are in Generated/MuseumData.g.cs, written by
// Tools/web-export/gen-csharp.mjs from the TANGHALAN web app's content (src/museum/content.js).
// Positions are in the web app's coordinates: metres, x to the east, z to the south, y up.
using System.Collections.Generic;

namespace Tanghalan
{
    public enum Lang { En, Fil }

    public enum WorkKind { Art, Story, Poem, Letter, Comic }

    public sealed class Room
    {
        public readonly string Id, Name, Sub, Pillar, Game;
        public readonly double X0, X1, Z0, Z1;

        public Room(string id, string name, string sub, double x0, double x1, double z0, double z1, string pillar, string game)
        {
            Id = id; Name = name; Sub = sub; X0 = x0; X1 = x1; Z0 = z0; Z1 = z1; Pillar = pillar; Game = game;
        }

        public bool Contains(double x, double z) { return x >= X0 && x <= X1 && z >= Z0 && z <= Z1; }
    }

    /// <summary>
    /// A doorway between two rooms. Axis 'x': in a wall that runs along x, at z = At, from x = From to To.
    /// Axis 'z': in a wall that runs along z, at x = At, from z = From to To.
    /// </summary>
    public sealed class Door
    {
        public readonly string A, B;
        public readonly char Axis;
        public readonly double At, From, To;

        public Door(string a, string b, char axis, double at, double from, double to)
        {
            A = a; B = b; Axis = axis; At = at; From = from; To = to;
        }

        public double MidX { get { return Axis == 'x' ? (From + To) / 2 : At; } }
        public double MidZ { get { return Axis == 'x' ? At : (From + To) / 2; } }
        public string Other(string room) { return room == A ? B : room == B ? A : null; }
    }

    public sealed class Work
    {
        public string Id, Room, Title, By, Grade, Place, Medium, Pillar, Game;
        public WorkKind Kind;
        public int Age;
        /// <summary>The name of the box in the museum model that the visitor points at (HOT__...).</summary>
        public string Hot;
        /// <summary>The texture shown beside the text: the drawing itself, or a story's illustration.</summary>
        public string Picture;
        public bool Stand, Featured;
        /// <summary>What the artist says about a drawing (drawings only).</summary>
        public string AboutEn, AboutFil;
        /// <summary>The text of a story, poem, letter or comic: pages, each a list of lines.</summary>
        public string[][] PagesEn, PagesFil;
        /// <summary>Bayani's tip.</summary>
        public string NoteEn, NoteFil;

        public string About(Lang lang) { return lang == Lang.Fil ? AboutFil : AboutEn; }
        public string[][] Pages(Lang lang) { return lang == Lang.Fil ? PagesFil : PagesEn; }
        public string Note(Lang lang) { return lang == Lang.Fil ? NoteFil : NoteEn; }
        public int PageCount { get { return PagesEn != null ? PagesEn.Length : 1; } }

        /// <summary>The text of one page in one piece (a drawing has a single page: what the artist says).</summary>
        public string PageText(int page, Lang lang)
        {
            var pages = Pages(lang);
            if (pages == null) return About(lang) ?? "";
            if (page < 0 || page >= pages.Length) return "";
            return string.Join(Kind == WorkKind.Poem || Kind == WorkKind.Letter ? "\n" : " ", pages[page]);
        }
    }

    public static partial class MuseumData
    {
        public static Room RoomById(string id)
        {
            foreach (var r in Rooms) if (r.Id == id) return r;
            return null;
        }

        public static Work WorkById(string id)
        {
            foreach (var w in Works) if (w.Id == id) return w;
            return null;
        }

        /// <summary>The room that holds this point, or null (the street, for one, is not a room).</summary>
        public static Room RoomAt(double x, double z)
        {
            foreach (var r in Rooms) if (r.Contains(x, z)) return r;
            return null;
        }

        public static string KindLabel(WorkKind k, Lang lang)
        {
            switch (k)
            {
                case WorkKind.Art: return lang == Lang.Fil ? "Guhit" : "Drawing";
                case WorkKind.Story: return lang == Lang.Fil ? "Kuwento" : "Story";
                case WorkKind.Poem: return lang == Lang.Fil ? "Tula" : "Poem";
                case WorkKind.Letter: return lang == Lang.Fil ? "Liham" : "Letter";
                default: return lang == Lang.Fil ? "Komiks" : "Comic";
            }
        }
    }
}
