// Everything TANGHALAN VR says, in English and in Filipino. The works' own texts are in MuseumData; the go
// bag items' names and notes are in GoBagItems; the trash is in Waste.
//
// The English follows the TANGHALAN web app where the web app says the same thing. The Filipino was written
// for TANGHALAN VR: please have a Filipino teacher read it over before children use it.
using System.Collections.Generic;

namespace Tanghalan
{
    public static class Strings
    {
        static readonly Dictionary<string, string[]> T = new Dictionary<string, string[]>
        {
            // ---- the start ----
            { "start.title", new[] { "Welcome to TANGHALAN VR!", "Maligayang pagdating sa TANGHALAN VR!" } },
            { "start.body", new[] { "A museum of children's drawings and stories about disasters. Bayani will walk with you.", "Isang museo ng mga guhit at kuwento ng mga bata tungkol sa sakuna. Sasamahan ka ni Bayani." } },
            { "start.lang", new[] { "Language: English", "Wika: Filipino" } },
            { "start.go", new[] { "Start the visit", "Simulan ang pagbisita" } },
            { "start.gama.on", new[] { "Connected to GAMA: the simulations run in GAMA.", "Nakakonekta sa GAMA: sa GAMA tumatakbo ang mga simulation." } },
            { "start.gama.off", new[] { "Not connected to GAMA: TANGHALAN VR runs the simulations itself.", "Hindi nakakonekta sa GAMA: ang TANGHALAN VR mismo ang nagpapatakbo ng mga simulation." } },
            { "start.gama.wait", new[] { "Looking for GAMA...", "Hinahanap ang GAMA..." } },
            { "start.howto", new[] { "Point at something and press the trigger. Push the stick forward to jump, or tilt it to walk.", "Ituro ang isang bagay at pindutin ang trigger. Itulak ang stick pasulong para tumalon, o ikiling para maglakad." } },
            { "start.howto.desktop", new[] { "No headset: walk with W A S D, look with the right mouse button, and click to choose.", "Walang headset: maglakad gamit ang W A S D, tumingin gamit ang kanang button ng mouse, at mag-click para pumili." } },

            // ---- the rooms: what Bayani says when you walk in (the English is the web app's) ----
            { "room.lobby", new[] { "Welcome to TANGHALAN! Walk through any door to explore. Point at a picture or a story stand and press the trigger.", "Maligayang pagdating sa TANGHALAN! Pumasok sa alinmang pinto. Ituro ang isang larawan o kuwento at pindutin ang trigger." } },
            { "room.bulwagan", new[] { "This is the Bulwagan ng Bayanihan, the featured hall. Bayanihan means helping each other.", "Ito ang Bulwagan ng Bayanihan, ang tampok na bulwagan. Ang bayanihan ay pagtutulungan." } },
            { "room.kuwentuhan", new[] { "Kuwentuhan Corner! Stories, poems and comics live here.", "Kuwentuhan Corner! Narito ang mga kuwento, tula at komiks." } },
            { "room.mit", new[] { "Stop Hazards Early: ways to stop dangers before they happen. The door to the street is here.", "Stop Hazards Early: mga paraan para mapigilan ang panganib bago ito mangyari. Narito ang pinto papunta sa kalye." } },
            { "room.prep", new[] { "Get Ready: how families prepare before a disaster.", "Get Ready: kung paano naghahanda ang pamilya bago ang sakuna." } },
            { "room.resp", new[] { "Act Fast: what to do when a disaster strikes.", "Act Fast: ang gagawin kapag may sakuna." } },
            { "room.rec", new[] { "Bounce Back: how we help each other recover.", "Bounce Back: kung paano tayo nagtutulungan para makabangon." } },
            { "room.fireroom", new[] { "The Fire Safety Room. Fire Practice is coming soon to TANGHALAN VR.", "Ang Fire Safety Room. Malapit na ang Fire Practice sa TANGHALAN VR." } },
            { "room.gobagroom", new[] { "Go Bag Room! Take an empty go bag from the packing table, then pack what your family needs.", "Go Bag Room! Kumuha ng walang lamang go bag sa mesang pang-empake, saka i-empake ang kailangan ng pamilya mo." } },

            // ---- the tasks board ----
            { "tasks.title", new[] { "Your visit", "Ang iyong pagbisita" } },
            { "tasks.works", new[] { "Look at {0} works of art and stories ({1} of {0})", "Tumingin ng {0} likha at kuwento ({1} sa {0})" } },
            { "tasks.canal", new[] { "Clear the canal on the street (Stop Hazards Early hall)", "Linisin ang kanal sa kalye (bulwagang Stop Hazards Early)" } },
            { "tasks.gobag", new[] { "Pack a go bag (Go Bag Room)", "Mag-empake ng go bag (Go Bag Room)" } },
            { "tasks.drill", new[] { "Earthquake drill, then go to the evacuation center", "Earthquake drill, at pumunta sa evacuation center" } },
            { "tasks.startdrill", new[] { "Start the earthquake drill", "Simulan ang earthquake drill" } },
            { "tasks.notyet", new[] { "Finish the tasks above first, or ask your teacher.", "Tapusin muna ang mga gawain sa itaas, o magtanong sa guro." } },

            // ---- reading a work ----
            { "work.by", new[] { "by {0}, {1} years old · {2} · {3}", "ni {0}, {1} taong gulang · {2} · {3}" } },
            { "work.tip", new[] { "Bayani's tip", "Paalala ni Bayani" } },
            { "work.page", new[] { "Page {0} of {1}", "Pahina {0} sa {1}" } },
            { "work.sample", new[] { "SAMPLE ENTRY", "HALIMBAWANG LIKHA" } },
            { "btn.close", new[] { "Close", "Isara" } },
            { "btn.next", new[] { "Next", "Susunod" } },
            { "btn.prev", new[] { "Back", "Bumalik" } },
            { "btn.lang", new[] { "Filipino", "English" } },

            // ---- the street and the canal ----
            { "street.arrive", new[] { "This is the street from Rico's story. The canal is full of trash, and heavy rain is coming.", "Ito ang kalye sa kuwento ni Rico. Puno ng basura ang kanal, at may paparating na malakas na ulan." } },
            { "street.howto", new[] { "Point at a piece of trash in the canal and press the trigger to pick it up. Then point at the right bin.", "Ituro ang basura sa kanal at pindutin ang trigger para pulutin. Saka ituro ang tamang basurahan." } },
            { "street.warning", new[] { "RAINFALL WARNING", "BABALA SA ULAN" } },
            { "street.left", new[] { "Trash left in the canal: {0} of {1}", "Basurang natitira sa kanal: {0} sa {1}" } },
            { "street.binned", new[] { "Sorted into the bins: {0}", "Naihiwalay sa basurahan: {0}" } },
            { "street.right", new[] { "Right bin! {0}", "Tamang basurahan! {0}" } },
            { "street.wrong", new[] { "Not that bin. {0} goes in {1}.", "Hindi diyan. Sa {1} ang {0}." } },
            { "street.holding", new[] { "You are holding: {0}. Point at a bin.", "Hawak mo: {0}. Ituro ang basurahan." } },
            { "street.done", new[] { "The canal is clear! Now see what the rain would do.", "Malinis na ang kanal! Tingnan natin ang gagawin ng ulan." } },
            { "street.storm", new[] { "See what the rain would do", "Tingnan ang gagawin ng ulan" } },
            { "street.storm.run", new[] { "Two hours of rain, in a short time. Watch the water rise at Rico's door.", "Dalawang oras na ulan, sa maikling panahon. Panoorin ang pagtaas ng tubig sa pinto ni Rico." } },
            { "street.storm.time", new[] { "Rain: {0} mm an hour · {1} min", "Ulan: {0} mm bawat oras · {1} min" } },
            { "street.storm.door", new[] { "Water at Rico's door: {0} cm", "Tubig sa pinto ni Rico: {0} cm" } },
            { "street.storm.end", new[] { "With {0} pieces left in the canal, the water reached {1} cm at Rico's door. If nobody had cleaned the canal: {2} cm.", "Sa {0} basurang natira sa kanal, umabot sa {1} cm ang tubig sa pinto ni Rico. Kung walang naglinis ng kanal: {2} cm." } },
            { "street.storm.dry", new[] { "With the canal clear, the rain flowed away and Rico's door stayed dry. If nobody had cleaned the canal: {0} cm.", "Dahil malinis ang kanal, dumaloy palayo ang ulan at hindi nabasa ang pinto ni Rico. Kung walang naglinis ng kanal: {0} cm." } },
            { "street.storm.again", new[] { "Watch it again", "Panoorin muli" } },
            { "depth.dry", new[] { "dry", "tuyo" } },
            { "depth.ankle", new[] { "ankle-deep", "hanggang bukung-bukong" } },
            { "depth.knee", new[] { "knee-deep", "hanggang tuhod" } },
            { "depth.waist", new[] { "waist-deep", "hanggang baywang" } },

            // ---- the go bag (the web app's lines) ----
            { "gobag.take", new[] { "Take a go bag", "Kumuha ng go bag" } },
            { "gobag.zip", new[] { "Zip it up", "Isara ang bag" } },
            { "gobag.again", new[] { "Pack again", "Mag-empake muli" } },
            { "gobag.easy", new[] { "Easy: on the tables", "Madali: nasa mesa" } },
            { "gobag.hard", new[] { "Difficult: search the room", "Mahirap: hanapin sa silid" } },
            { "gobag.tip", new[] { "Your bag has limited room, so choose the supplies that matter most.", "Limitado ang laman ng bag mo, kaya piliin ang pinakamahalagang gamit." } },
            { "gobag.start.easy", new[] { "Walk to the tables and point at a thing to look at it. Your bag has limited room, so choose the supplies that matter most.", "Pumunta sa mga mesa at ituro ang isang bagay para tingnan ito. Limitado ang laman ng bag mo, kaya piliin ang pinakamahalagang gamit." } },
            { "gobag.start.hard", new[] { "The supplies are hidden. Open the cabinet, the drawers, the crate and the cooler. Your bag has limited room, so choose what matters most.", "Nakatago ang mga gamit. Buksan ang aparador, ang mga drawer, ang kahon at ang cooler. Limitado ang laman ng bag mo, kaya piliin ang pinakamahalaga." } },
            { "gobag.first", new[] { "Take an empty go bag from the packing table first.", "Kumuha muna ng walang lamang go bag sa mesang pang-empake." } },
            { "gobag.heavy", new[] { "Your bag is getting heavy. Pick what matters most.", "Bumibigat na ang bag mo. Piliin ang pinakamahalaga." } },
            { "gobag.pack", new[] { "Pack it", "Ilagay sa bag" } },
            { "gobag.leave", new[] { "Leave it", "Iwan" } },
            { "gobag.unpack", new[] { "Take it out", "Alisin sa bag" } },
            { "gobag.weight", new[] { "Weight {0} of {1}", "Bigat {0} sa {1}" } },
            { "gobag.itemweight", new[] { "This weighs {0}. Your bag: {1} of {2}.", "Bigat nito: {0}. Ang bag mo: {1} sa {2}." } },
            { "gobag.notzipped", new[] { "Your go bag is not zipped up yet. Come back to the Go Bag Room to finish packing.", "Hindi pa nakasara ang go bag mo. Bumalik sa Go Bag Room para tapusin ang pag-empake." } },
            { "gobag.found", new[] { "You found {0} in {1}. Point at one to look at it.", "May nakita kang {0} sa {1}. Ituro ang isa para tingnan." } },
            { "gobag.empty", new[] { "This one is empty. Keep searching.", "Walang laman ito. Maghanap pa." } },
            { "gobag.open", new[] { "Open {0}", "Buksan ang {0}" } },
            { "gobag.result", new[] { "You packed {0} of {1} useful items.", "Nailagay mo ang {0} sa {1} na kapaki-pakinabang na gamit." } },
            { "gobag.result.wrong", new[] { "You packed {0} of {1} useful items, and {2} that could stay home.", "Nailagay mo ang {0} sa {1} na kapaki-pakinabang na gamit, at {2} na puwedeng iwan sa bahay." } },
            { "gobag.missed", new[] { "You missed:", "Hindi mo nailagay:" } },
            { "gobag.wrongs", new[] { "Could stay home:", "Puwedeng iwan:" } },
            { "gobag.great", new[] { "Great packing!", "Magaling mag-empake!" } },
            { "gobag.good", new[] { "Good packing!", "Mahusay!" } },
            { "gobag.try", new[] { "Nice try!", "Magaling na pagsubok!" } },
            { "gobag.remind", new[] { "Keep your family go bag near the door, and check it with a grown-up every few months.", "Ilagay ang go bag ng pamilya malapit sa pinto, at suriin ito kasama ang isang nakatatanda tuwing ilang buwan." } },
            { "gobag.best", new[] { "Your best", "Pinakamahusay mo" } },
            { "gobag.cabinet", new[] { "the cabinet", "ang aparador" } },
            { "gobag.drawers", new[] { "the drawers", "ang mga drawer" } },
            { "gobag.crate", new[] { "the crate", "ang kahon" } },
            { "gobag.cooler", new[] { "the cooler", "ang cooler" } },
            { "fire.soon", new[] { "Fire Practice is coming soon to TANGHALAN VR. Its rules are ready; the room is not yet built.", "Malapit na ang Fire Practice sa TANGHALAN VR. Handa na ang mga patakaran; hindi pa tapos ang silid." } },

            // ---- the earthquake drill ----
            { "drill.announce", new[] { "Earthquake drill! Look around: where is the nearest sturdy table?", "Earthquake drill! Tumingin sa paligid: nasaan ang pinakamalapit na matibay na mesa?" } },
            { "drill.countdown", new[] { "The drill starts in {0}...", "Magsisimula ang drill sa {0}..." } },
            { "drill.Duck", new[] { "Earthquake! DUCK down low!", "Lindol! YUMUKO!" } },
            { "drill.Cover", new[] { "COVER: get under the sturdy table! Point at it and press the trigger.", "TAKIP: pumailalim sa matibay na mesa! Ituro ito at pindutin ang trigger." } },
            { "drill.Hold", new[] { "HOLD on to the table leg! Squeeze the grip button.", "KUMAPIT sa paa ng mesa! Pisilin ang grip button." } },
            { "drill.GoodStay", new[] { "Good! Stay under the table and hold on until the shaking stops.", "Mahusay! Manatili sa ilalim ng mesa at kumapit hanggang tumigil ang yanig." } },
            { "drill.StayUnderCover", new[] { "Stay under cover until the shaking stops!", "Manatili sa ilalim ng mesa hanggang tumigil ang yanig!" } },
            { "drill.Wait", new[] { "The shaking stopped. Stay a moment more, and look around for danger.", "Tumigil na ang yanig. Maghintay pa sandali, at tumingin sa paligid kung may panganib." } },
            { "drill.WalkOut", new[] { "It is safe to go. Walk out calmly to the evacuation center. Follow the arrows.", "Ligtas nang lumabas. Maglakad nang mahinahon papunta sa evacuation center. Sundan ang mga palaso." } },
            { "drill.KeepWalking", new[] { "Walk, do not run. Stay away from walls, posts and windows.", "Maglakad, huwag tumakbo. Lumayo sa pader, poste at bintana." } },
            { "drill.AftershockDuck", new[] { "Aftershock! Stop, duck, and cover your head with your arms!", "Aftershock! Huminto, yumuko, at takpan ang ulo ng mga braso!" } },
            { "drill.AftershockGood", new[] { "Good! Wait until it stops, then keep walking.", "Mahusay! Hintaying tumigil, saka magpatuloy sa paglakad." } },
            { "drill.Arrived", new[] { "You made it to the evacuation center! Let us check that everyone is here.", "Nakarating ka sa evacuation center! Tingnan natin kung narito ang lahat." } },
            { "drill.duckbtn", new[] { "Press A to duck", "Pindutin ang A para yumuko" } },
            { "drill.headcount", new[] { "Everyone is here: {0} neighbors, Bayani, and you.", "Narito ang lahat: {0} kapitbahay, si Bayani, at ikaw." } },
            { "drill.waiting", new[] { "Waiting for {0} more neighbors...", "Hinihintay pa ang {0} kapitbahay..." } },
            { "drill.intensity", new[] { "Shaking: {0}", "Yanig: {0}" } },

            // ---- the summary at the evacuation center ----
            { "end.title", new[] { "Well done, Champion!", "Magaling, Kampeon!" } },
            { "end.works", new[] { "Works you looked at: {0} of {1}", "Mga likhang tiningnan mo: {0} sa {1}" } },
            { "end.canal", new[] { "Canal: {0} of {1} pieces of trash cleared, {2} sorted right", "Kanal: {0} sa {1} basura ang nalinis, {2} ang tamang naihiwalay" } },
            { "end.canal.none", new[] { "Canal: not cleared this time", "Kanal: hindi nalinis ngayon" } },
            { "end.flood", new[] { "The rain at Rico's door: {0} (if nobody cleaned: {1})", "Ulan sa pinto ni Rico: {0} (kung walang naglinis: {1})" } },
            { "end.gobag", new[] { "Go bag: {0} stars", "Go bag: {0} bituin" } },
            { "end.gobag.none", new[] { "Go bag: not packed this time", "Go bag: hindi nai-empake ngayon" } },
            { "end.gobag.carried", new[] { "You brought your go bag to the evacuation center.", "Dinala mo ang go bag mo sa evacuation center." } },
            { "end.drill", new[] { "Earthquake drill: {0} stars", "Earthquake drill: {0} bituin" } },
            { "end.duck", new[] { "Ducked after {0} s, under cover after {1} s", "Yumuko pagkalipas ng {0} s, nasa ilalim ng mesa pagkalipas ng {1} s" } },
            { "end.nocover", new[] { "You did not get under a table this time.", "Hindi ka nakapailalim sa mesa ngayon." } },
            { "end.hold", new[] { "Held on for {0}% of the shaking", "Kumapit sa {0}% ng yanig" } },
            { "end.left", new[] { "Remember: stay under cover until the shaking stops.", "Tandaan: manatili sa ilalim ng mesa hanggang tumigil ang yanig." } },
            { "end.walk", new[] { "Walked to the evacuation center in {0} s", "Nakarating sa evacuation center sa loob ng {0} s" } },
            { "end.after", new[] { "In the aftershock you ducked and covered your head.", "Sa aftershock, yumuko ka at tinakpan ang ulo." } },
            { "end.after.no", new[] { "In an aftershock: stop, duck and cover your head.", "Kapag may aftershock: huminto, yumuko at takpan ang ulo." } },
            { "end.remember", new[] { "Duck, Cover, Hold. Clean canals. A ready go bag. Sama-sama, handa!", "Yuko, Takip, Kapit. Malinis na kanal. Handang go bag. Sama-sama, handa!" } },
            { "end.again", new[] { "Visit again", "Bumisita muli" } },
        };

        public static string Get(string key, Lang lang)
        {
            string[] v;
            if (!T.TryGetValue(key, out v)) return key;
            return v[lang == Lang.Fil ? 1 : 0];
        }

        public static string Format(string key, Lang lang, params object[] args)
        {
            return string.Format(Get(key, lang), args);
        }

        public static string Hint(DrillHint h, Lang lang)
        {
            return h == DrillHint.None || h == DrillHint.Countdown ? "" : Get("drill." + h, lang);
        }

        /// <summary>How a depth of water reads for a child: dry, ankle-deep, knee-deep or waist-deep.</summary>
        public static string Depth(double metres, Lang lang)
        {
            string word = metres < 0.02 ? "depth.dry" : metres < 0.25 ? "depth.ankle" : metres < 0.6 ? "depth.knee" : "depth.waist";
            if (metres < 0.02) return Get(word, lang);
            return string.Format("{0} cm, {1}", (int)System.Math.Round(metres * 100), Get(word, lang));
        }

        public static IEnumerable<string> Keys { get { return T.Keys; } }

        public static bool Has(string key) { return T.ContainsKey(key); }
    }
}
