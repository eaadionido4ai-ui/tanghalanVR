/**
* Name: TANGHALAN VR
* Links the TANGHALAN model (TANGHALAN.gaml) to the TANGHALAN VR app in Unity, through the SIMPLE plugin and
* the SIMPLE WebPlatform. GAMA runs the storm, the earthquake and the neighbors, and sends them to the
* headsets; the headsets send back what the visitors do (Unity: Assets/TANGHALAN/Scripts/Runtime/
* TanghalanSimulationManager.cs and Scenario.cs).
*
* The earthquake drill comes after the go bag. Each headset tells GAMA when its go bag is zipped. When the bag
* of every headset is zipped, GAMA waits a few seconds, then starts the drill for all of them, so that the
* class drills together: one earthquake, one aftershock, the same neighbors on the street. A teacher can
* start it at any time with the "Start the earthquake drill" button. A headset that comes to its drill later,
* when that earthquake is over, gets an earthquake of its own.
*
* What GAMA sends to Unity (send_message), each a map whose "tanghalan" entry says what it is:
*   flood       t: seconds of the storm, rain: mm/h x 10, d: the water depth of each cell in mm (row by row from the north)
*   storm_end   the storm is over; door_mm: the deepest water at Rico's door, in mm
*   storm_busy  GAMA is running another headset's storm: this headset runs its own
*   class       every two seconds: ready, how many go bags are zipped; players, how many headsets
*   drill       the earthquake drill starts now
*   quake       i: how strongly the ground shakes x 1000
*   people      p: for each neighbor x, y, z (web coordinates, mm), heading (degrees), arrived (0 or 1)
* The flood goes to the headset that asked for the storm, the shaking and the neighbors to the headsets in the
* drill, and the count of the class to every headset.
* What Unity asks GAMA to do (actions of unity_linker, every argument a string):
*   tg_hello(id, lang), tg_event(id, kind, detail), tg_canal(id, left), tg_storm(id, left), tg_storm_stop(id),
*   tg_bag(id, stars), tg_quake(id), tg_evacuate(id), tg_arrived(id, stars, walk, duck, cover, hold)
* Everything the visitors do is also written to results/tanghalan_log.csv (with the headset's id only).
* Author: TANGHALAN VR
* Tags: Unity, SIMPLE, flood, earthquake drill, evacuation
*/
model TANGHALAN_VR

import "TANGHALAN.gaml"

global {
	string log_file <- "../results/tanghalan_log.csv";

	// ---------- the class's earthquake drill (Unity: DrillStart) ----------
	// seconds between the last go bag being zipped and the drill: the time to read the stars
	float drill_delay <- 8.0;
	// headsets whose shaking starts within this many seconds of the first one share its earthquake
	float join_window <- 5.0;
	// an earthquake that nobody finishes stops counting as running after this many seconds
	float drill_timeout <- 600.0;
	// seconds between two counts of the class sent to the headsets
	float count_every <- 2.0;

	action storm_ended {
		ask unity_linker { do tell_some([storm_player], ["tanghalan"::"storm_end", "door_mm"::int(peak_door * 1000)]); }
	}
}

species unity_linker parent: abstract_unity_linker {
	string player_species <- string(unity_player);
	int min_num_players <- 0;
	int max_num_players <- 6;
	// the visitor starts in the lobby: web (0, 4.8), so GAMA (30, 29.8)
	list<point> init_locations <- [{30.0, 29.8, 0.0}, {30.5, 29.8, 0.0}, {29.5, 29.8, 0.0}, {30.0, 30.4, 0.0}, {30.5, 30.4, 0.0}, {29.5, 30.4, 0.0}];
	// TANGHALAN sends its own messages, not the template's geometries
	bool do_send_world <- false;

	// the headset whose storm is running, and the headsets in the earthquake that is running
	unity_player storm_player <- nil;
	list<unity_player> drill_players <- [];
	// counts down to the class's drill once every go bag is zipped (below zero: not counting)
	float class_timer <- -1.0;
	float count_timer <- 0.0;

	action tell_players (map<string, unknown> mes) {
		if (!empty(unity_player)) {
			do send_message players: unity_player as list mes: mes;
		}
	}

	// sends a message to some of the headsets only
	action tell_some (list<unity_player> who, map<string, unknown> mes) {
		list<unity_player> there <- who where (each != nil and !dead(each));
		if (!empty(there)) {
			do send_message players: there mes: mes;
		}
	}

	action write_log (string id, string kind, string detail) {
		save [cycle, string(#now), id, kind, detail] to: log_file format: "csv" rewrite: false header: false;
	}

	// ---------- what Unity asks ----------

	action tg_hello (string id, string lang) {
		// a new visit: nothing is packed yet
		unity_player p <- unity_player first_with (each.name = id);
		if (p != nil) {
			p.bag_done <- false;
			p.bag_stars <- 0;
			p.drilled <- false;
			p.at_center <- false;
			remove p from: drill_players;
		}
		do write_log(id, "visit", lang);
	}

	action tg_event (string id, string kind, string detail) {
		do write_log(id, kind, detail);
	}

	action tg_canal (string id, string left) {
		pieces_left <- int(left);
		do write_log(id, "canal_left", left);
	}

	// GAMA runs one storm at a time: a headset that asks while another's storm is running runs its own
	action tg_storm (string id, string left) {
		unity_player p <- unity_player first_with (each.name = id);
		if (storm_running and storm_player != nil and !dead(storm_player) and storm_player != p) {
			do tell_some([p], ["tanghalan"::"storm_busy"]);
			do write_log(id, "storm_on_headset", left);
		} else {
			storm_player <- p;
			ask world { do start_storm(int(left)); }
			do write_log(id, "storm", left);
		}
	}

	action tg_storm_stop (string id) {
		if (storm_player != nil and !dead(storm_player) and storm_player.name = id) {
			ask world { do stop_storm; }
		}
	}

	// the headset's go bag is zipped, and it waits for the earthquake drill (it says so again every few seconds)
	action tg_bag (string id, string stars) {
		unity_player p <- unity_player first_with (each.name = id);
		if (p != nil) {
			if (!p.bag_done or p.bag_stars != int(stars)) {
				do write_log(id, "bag_zipped", stars + " stars");
			}
			p.bag_done <- true;
			p.bag_stars <- int(stars);
			p.drilled <- false;
		}
	}

	// the shaking starts on a headset
	action tg_quake (string id) {
		unity_player p <- unity_player first_with (each.name = id);
		if (p != nil) {
			p.drilled <- true;
			p.at_center <- false;
			// an earthquake is busy while another headset is still in it
			list<unity_player> others <- drill_players where (each != nil and !dead(each) and each != p and !each.at_center);
			bool busy <- quake_running and quake_time < drill_timeout and !empty(others);
			if (!busy) {
				// a new earthquake, for this headset
				drill_players <- [p];
				ask world { do start_quake; }
			} else if (quake_time <= join_window) {
				// the class's earthquake has just begun: this headset shares it
				if (!(drill_players contains p)) { drill_players << p; }
			} else {
				// an earthquake is well under way for other headsets: this one runs its own, and GAMA sends it nothing
				remove p from: drill_players;
			}
		}
		do write_log(id, "quake", "");
	}

	// a headset starts to walk out: the neighbors leave their houses (once for the headsets that share the earthquake)
	action tg_evacuate (string id) {
		unity_player p <- unity_player first_with (each.name = id);
		if (p != nil and (drill_players contains p) and quake_running and !evacuating) {
			ask world { do start_evacuation; }
		}
		do write_log(id, "evacuate", "");
	}

	action tg_arrived (string id, string stars, string walk, string duck, string cover, string hold) {
		unity_player p <- unity_player first_with (each.name = id);
		if (p != nil) { p.at_center <- true; }
		do write_log(id, "arrived", "stars " + stars + ", walk " + walk + " s, duck " + duck + " s, cover " + cover + " s, hold " + hold + "%");
	}

	// ---------- the class's drill ----------

	// the drill starts now on these headsets
	action call_drill (list<unity_player> who) {
		ask who { drilled <- true; }
		class_timer <- -1.0;
		do tell_some(who, ["tanghalan"::"drill"]);
	}

	// the teacher's button: the drill starts now on every headset that has not had it yet
	action teacher_starts_drill {
		do call_drill(unity_player where (!each.drilled));
		do write_log("teacher", "drill", "");
	}

	// when the go bag of every headset is zipped, the drill comes for all of them together
	reflex class_drill when: !empty(unity_player) {
		list<unity_player> to_go <- unity_player where (!each.drilled);
		list<unity_player> zipped <- to_go where (each.bag_done);
		if (!empty(to_go) and length(zipped) = length(to_go)) {
			if (class_timer < 0) { class_timer <- drill_delay; }
			class_timer <- class_timer - real_dt;
			if (class_timer <= 0) { do call_drill(zipped); }
		} else {
			class_timer <- -1.0;
		}
		// the count of the class, for the headsets that wait
		count_timer <- count_timer - real_dt;
		if (count_timer <= 0) {
			count_timer <- count_every;
			do tell_players(["tanghalan"::"class", "ready"::length(unity_player where (each.bag_done or each.drilled)), "players"::length(unity_player)]);
		}
	}

	// ---------- what GAMA sends ----------

	reflex send_flood when: storm_running and storm_player != nil {
		list<int> mm <- [];
		loop c from: 0 to: ncell - 1 { mm << int(round(water[c] * 1000)); }
		do tell_some([storm_player], ["tanghalan"::"flood", "t"::int(storm_time), "rain"::int(rain_at(storm_time) * 10), "d"::mm]);
	}

	reflex send_quake when: quake_running {
		do tell_some(drill_players, ["tanghalan"::"quake", "i"::int(intensity * 1000)]);
	}

	reflex send_people when: evacuating and !empty(evacuee where !each.arrived) {
		list<int> p <- [];
		loop e over: evacuee {
			p << int(e.web.x * 1000); p << int(e.web.y * 1000); p << int(e.web.z * 1000);
			p << int(e.heading); p << (e.arrived ? 1 : 0);
		}
		do tell_some(drill_players, ["tanghalan"::"people", "p"::p]);
	}
}

species unity_player parent: abstract_unity_player {
	float player_size <- 1.0;
	rgb color <- #red;
	float cone_distance <- 6.0;
	float cone_amplitude <- 90.0;
	float player_rotation <- 90.0;
	bool to_display <- true;

	// where this headset is in the visit
	bool bag_done <- false;   // its go bag is zipped
	int bag_stars <- 0;
	bool drilled <- false;    // the earthquake drill has started for it
	bool at_center <- false;  // it has reached the evacuation center

	aspect default {
		if to_display {
			if selected { draw circle(player_size) color: rgb(#blue, 0.5); }
			draw circle(player_size / 2.0) color: color;
			draw player_perception_cone() color: rgb(color, 0.5);
		}
	}
}

experiment vr_xp parent: TANGHALAN autorun: false type: unity {
	float minimum_cycle_duration <- 0.1;
	string unity_linker_species <- string(unity_linker);
	list<string> displays_to_hide <- ["street"];

	user_command "Start the earthquake drill" {
		ask unity_linker { do teacher_starts_drill; }
	}

	action create_player (string id) {
		ask unity_linker { do create_player(id); }
	}

	action remove_player (string id_input) {
		if (not empty(unity_player)) {
			ask first(unity_player where (each.name = id_input)) { do die; }
		}
	}

	output {
		display street_vr parent: street {
			species unity_player;
			graphics "class" {
				draw "Go bags zipped: " + length(unity_player where (each.bag_done or each.drilled)) + " of " + length(unity_player) + " headsets" at: {1, 6} color: #black font: font("Helvetica", 12, #plain);
			}
		}
	}
}
