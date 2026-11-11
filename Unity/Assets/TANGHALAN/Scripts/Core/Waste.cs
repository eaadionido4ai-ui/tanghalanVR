// The trash in the canal of "Ang Kanal sa Aming Kalye", and the three bins it is sorted into, as the
// Ecological Solid Waste Management Act (RA 9003) asks every barangay to do: nabubulok (biodegradable),
// nare-recycle (recyclable) and di-nabubulok (residual). Where each piece lies is in the street model
// (the anchors waste_00 to waste_13); what each piece is, is here.
using System.Collections.Generic;

namespace Tanghalan
{
    public enum Bin { Bio, Rec, Res }

    public sealed class WasteKind
    {
        public readonly string Model, NameEn, NameFil, WhyEn, WhyFil;
        public readonly Bin Bin;

        public WasteKind(string model, Bin bin, string nameEn, string nameFil, string whyEn, string whyFil)
        {
            Model = model; Bin = bin; NameEn = nameEn; NameFil = nameFil; WhyEn = whyEn; WhyFil = whyFil;
        }

        public string Name(Lang l) { return l == Lang.Fil ? NameFil : NameEn; }
        public string Why(Lang l) { return l == Lang.Fil ? WhyFil : WhyEn; }
    }

    public static class Waste
    {
        public static readonly WasteKind[] Kinds =
        {
            new WasteKind("banana", Bin.Bio, "Banana peel", "Balat ng saging", "It rots and can become compost.", "Nabubulok ito at puwedeng gawing compost."),
            new WasteKind("leaves", Bin.Bio, "Dry leaves", "Tuyong dahon", "Leaves rot and feed the soil.", "Nabubulok ang dahon at nagpapataba sa lupa."),
            new WasteKind("coconut", Bin.Bio, "Coconut shell", "Bao ng niyog", "It comes from a plant, so it rots in time.", "Galing ito sa halaman, kaya nabubulok din."),
            new WasteKind("foodscraps", Bin.Bio, "Leftover food", "Tirang pagkain", "Food scraps rot and can become compost.", "Nabubulok ang tirang pagkain at puwedeng gawing compost."),
            new WasteKind("bottle", Bin.Rec, "Plastic bottle", "Boteng plastik", "Clean plastic bottles can be made into new things.", "Puwedeng gawing bagong gamit ang malinis na boteng plastik."),
            new WasteKind("can", Bin.Rec, "Tin can", "Lata", "Metal cans can be melted and used again.", "Puwedeng tunawin at gamitin muli ang lata."),
            new WasteKind("glassbottle", Bin.Rec, "Glass bottle", "Boteng babasagin", "Glass can be used again or made into new glass.", "Puwedeng gamitin muli o gawing bagong bote ang babasagin."),
            new WasteKind("cardboard", Bin.Rec, "Cardboard", "Karton", "Paper and cardboard can be recycled.", "Nare-recycle ang papel at karton."),
            new WasteKind("sachet", Bin.Res, "Sachets", "Mga sachet", "Sachets are made of layers that cannot be recycled.", "Gawa sa patong-patong na materyales ang sachet kaya hindi ito nare-recycle."),
            new WasteKind("styro", Bin.Res, "Styrofoam", "Styrofoam", "Styrofoam does not rot and is hard to recycle.", "Hindi nabubulok ang styrofoam at mahirap itong i-recycle."),
            new WasteKind("plasticbag", Bin.Res, "Plastic bag", "Supot na plastik", "Thin plastic bags clog canals and are hard to recycle.", "Bumabara sa kanal ang manipis na supot at mahirap itong i-recycle."),
            new WasteKind("slipper", Bin.Res, "Old slipper", "Lumang tsinelas", "Rubber slippers do not rot and are not recycled here.", "Hindi nabubulok ang tsinelas at hindi ito nire-recycle dito."),
        };

        /// <summary>What each of the 14 pieces in the canal is, in the order of the anchors waste_00 to waste_13.</summary>
        public static readonly string[] Pieces =
        {
            "bottle", "sachet", "banana", "plasticbag", "can", "styro", "leaves",
            "glassbottle", "slipper", "cardboard", "coconut", "foodscraps", "bottle", "sachet",
        };

        public static int Count { get { return Pieces.Length; } }

        public static WasteKind KindOf(string model)
        {
            foreach (var k in Kinds) if (k.Model == model) return k;
            return null;
        }

        public static WasteKind Piece(int i) { return KindOf(Pieces[i]); }

        public static string BinName(Bin b, Lang l)
        {
            switch (b)
            {
                case Bin.Bio: return l == Lang.Fil ? "Nabubulok" : "Biodegradable";
                case Bin.Rec: return l == Lang.Fil ? "Nare-recycle" : "Recyclable";
                default: return l == Lang.Fil ? "Di-nabubulok" : "Residual";
            }
        }

        /// <summary>The bin's id in the street model (ZONE__bin_bio, ZONE__bin_rec, ZONE__bin_res).</summary>
        public static string BinId(Bin b) { return b == Bin.Bio ? "bin_bio" : b == Bin.Rec ? "bin_rec" : "bin_res"; }

        public static bool TryBinFromId(string id, out Bin b)
        {
            b = Bin.Res;
            if (id == "bin_bio") { b = Bin.Bio; return true; }
            if (id == "bin_rec") { b = Bin.Rec; return true; }
            return id == "bin_res";
        }
    }

    /// <summary>The canal clean-up: which pieces are still in the canal, and how they were sorted.</summary>
    public sealed class CanalCleanup
    {
        public enum State { InCanal, Held, Binned }

        readonly State[] state = new State[Waste.Count];
        public int Binned { get; private set; }
        public int WrongTries { get; private set; }
        public int Held { get; private set; } = -1;

        /// <summary>Pieces still lying in the canal: these are what block the water.</summary>
        public int Left
        {
            get { int n = 0; foreach (var s in state) if (s == State.InCanal) n++; return n; }
        }

        public State StateOf(int piece) { return state[piece]; }

        /// <summary>Takes a piece out of the canal. Only one is held at a time: one already held goes back where it was.</summary>
        public bool PickUp(int piece)
        {
            if (piece < 0 || piece >= state.Length || state[piece] != State.InCanal) return false;
            if (Held >= 0) state[Held] = State.InCanal;
            state[piece] = State.Held; Held = piece;
            return true;
        }

        /// <summary>Puts the held piece back (into the canal) without sorting it.</summary>
        public void PutBack()
        {
            if (Held >= 0) { state[Held] = State.InCanal; Held = -1; }
        }

        /// <summary>Drops the held piece in a bin. Returns false, and keeps the piece in hand, if it is the wrong bin.</summary>
        public bool DropIn(Bin bin)
        {
            if (Held < 0) return false;
            if (Waste.Piece(Held).Bin != bin) { WrongTries++; return false; }
            state[Held] = State.Binned; Held = -1; Binned++;
            return true;
        }
    }
}
