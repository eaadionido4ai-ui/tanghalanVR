/**
* Name: TANGHALAN VR
* Links the TANGHALAN model (TANGHALAN.gaml) to the TANGHALAN VR app in Unity, through the SIMPLE plugin and
* the SIMPLE WebPlatform. GAMA runs the storm, the earthquake and the neighbors, and sends them to each
* headset; the headsets send back what the visitors do (Unity: Assets/TANGHALAN/Scripts/Runtime/
* TanghalanSimulationManager.cs and Scenario.cs). A teacher can start the earthquake drill for every
* headset at once with the "Start the earthquake drill" button.
*
* What GAMA sends to Unity (send_message), each a map whose "tanghalan" entry says what it is:
*   flood       t: seconds of the storm, rain: mm/h x 10, d: the water depth of each cell in mm (row by row from the north)
*   storm_end   the storm is over
*   quake       i: how strongly the ground shakes x 1000
*   people      p: for each neighbor x, y, z (web coordinates, mm), heading (degrees), arrived (0 or 1)
*   drill       a teacher started the drill
* What Unity asks GAMA to do (actions of unity_linker, every argument a string):
*   tg_hello(id, lang), tg_event(id, kind, detail), tg_canal(id, left), tg_storm(id, left), tg_storm_stop(id),
*   tg_quake(id), tg_evacuate(id), tg_arrived(id, stars, walk, duck, cover, hold)
* Everything the visitors do is also written to results/tanghalan_log.csv (with the headset's id only).
* Author: TANGHALAN VR
* Tags: Unity, SIMPLE, flood, earthquake drill, evacuation
*/
model TANGHALAN_VR

import "TANGHALAN.gaml"

global {
	string log_file <- "../results/tanghalan_log.csv";

	action storm_ended {
		ask unity_linker { do tell_players(["tanghalan"::"storm_end", "door_mm"::int(peak_door * 1000)]); }
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
	bool storm_was_running <- false;

	action tell_players (map<string, unknown> mes) {
		if (!empty(unity_player)) {
			do send_message players: unity_player as list mes: mes;
		}
	}

	action write_log (string id, string kind, string detail) {
		save [cycle, string(#now), id, kind, detail] to: log_file format: "csv" rewrite: false header: false;
	}

	// ---------- what Unity asks ----------

	action tg_hello (string id, string lang) {
		do write_log(id, "visit", lang);
	}

	action tg_event (string id, string kind, string detail) {
		do write_log(id, kind, detail);
	}

	action tg_canal (string id, string left) {
		pieces_left <- int(left);
		do write_log(id, "canal_left", left);
	}

	action tg_storm (string id, string left) {
		ask world { do start_storm(int(left)); }
		do write_log(id, "storm", left);
	}

	action tg_storm_stop (string id) {
		ask world { do stop_storm; }
	}

	action tg_quake (string id) {
		ask world { do start_quake; }
		do write_log(id, "quake", "");
	}

	action tg_evacuate (string id) {
		ask world { do start_evacuation; }
		do write_log(id, "evacuate", "");
	}

	action tg_arrived (string id, string stars, string walk, string duck, string cover, string hold) {
		do write_log(id, "arrived", "stars " + stars + ", walk " + walk + " s, duck " + duck + " s, cover " + cover + " s, hold " + hold + "%");
	}

	// ---------- what GAMA sends ----------

	reflex send_flood when: storm_running {
		list<int> mm <- [];
		loop c from: 0 to: ncell - 1 { mm << int(round(water[c] * 1000)); }
		do tell_players(["tanghalan"::"flood", "t"::int(storm_time), "rain"::int(rain_at(storm_time) * 10), "d"::mm]);
	}

	reflex send_quake when: quake_running {
		do tell_players(["tanghalan"::"quake", "i"::int(intensity * 1000)]);
	}

	reflex send_people when: evacuating and !empty(evacuee where !each.arrived) {
		list<int> p <- [];
		loop e over: evacuee {
			p << int(e.web.x * 1000); p << int(e.web.y * 1000); p << int(e.web.z * 1000);
			p << int(e.heading); p << (e.arrived ? 1 : 0);
		}
		do tell_players(["tanghalan"::"people", "p"::p]);
	}
}

species unity_player parent: abstract_unity_player {
	float player_size <- 1.0;
	rgb color <- #red;
	float cone_distance <- 6.0;
	float cone_amplitude <- 90.0;
	float player_rotation <- 90.0;
	bool to_display <- true;

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
		ask unity_linker { do tell_players(["tanghalan"::"drill"]); }
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
		}
	}
}
