/**
* Name: TANGHALAN
* The street and the drill of TANGHALAN VR as an agent-based model:
*   - a flood on a grid of one-metre cells: rain, a canal blocked by trash, a culvert that lets water out;
*   - an earthquake's shaking over time, and an aftershock while people walk out;
*   - the neighbors (agents) who leave their houses and walk to the evacuation center, the covered court.
* "TANGHALAN VR.gaml" links this model to the Unity app through the SIMPLE plugin. The numbers are the same as
* in Unity (Assets/TANGHALAN/Scripts/Core: StreetPlan, FloodModel, QuakeSchedule, Evacuation) and in
* Tools/flood-reference/flood.py: change them in all three places.
*
* Places are given in TANGHALAN's web coordinates (metres; x east, z south; y up) and drawn in GAMA at
* (x + 30, z + 25): GAMA's world is 46 m by 36 m with its origin at the north-west corner.
* Author: TANGHALAN VR
* Tags: flood, canal, solid waste, earthquake, evacuation, SIMPLE
*/
model TANGHALAN

global {
	geometry shape <- rectangle(46, 36);
	float offset_x <- 30.0;
	float offset_y <- 25.0;

	// ---------- the street's grid (Unity: StreetPlan) ----------
	int nx <- 14;
	int nz <- 25;
	int ncell <- nx * nz;
	float grid_x0 <- -27.0;
	float grid_z0 <- -17.0;
	int canal_i <- 6;
	int door_i <- 3;
	int door_j <- 10;

	// ---------- the flood (Unity: FloodModel) ----------
	float dt <- 2.0;               // seconds of the storm in one step
	float rain_seconds <- 7200.0;  // two hours of rain
	float k_land <- 0.20;
	float k_canal <- 0.90;
	int canal_passes <- 12;
	float upstream_area <- 1300.0; // the land upstream whose runoff reaches the canal, m2
	float outlet_flow <- 0.046;    // what the clear culvert lets out, m3/s
	float outlet_min <- 0.03;
	float outlet_per_piece <- 0.0693;
	list<float> rain_minute <- [0.0, 10.0, 30.0, 70.0, 100.0, 120.0];
	list<float> rain_rate <- [10.0, 20.0, 85.0, 85.0, 20.0, 0.0];
	// how much faster than real time the storm runs when a visitor watches it
	float storm_speed <- 112.0;

	list<float> ground <- [];
	list<float> water <- [];
	list<float> delta <- [];
	list<float> out_sum <- [];
	list<int> canal_a <- [];
	list<int> canal_b <- [];
	list<int> land_a <- [];
	list<int> land_b <- [];

	int pieces_left <- 14 min: 0 max: 14;
	bool storm_running <- false;
	float storm_time <- 0.0;
	float storm_accum <- 0.0;
	float total_in <- 0.0;
	float total_out <- 0.0;
	float peak_door <- 0.0;

	// ---------- the earthquake (Unity: QuakeSchedule) ----------
	float ramp_up <- 3.0;
	float strong <- 13.0;
	float ramp_down <- 8.0;
	float aftershock_after <- 25.0;
	float aftershock_duration <- 6.0;
	float aftershock_peak <- 0.45;
	bool quake_running <- false;
	float quake_time <- 0.0;
	bool evacuating <- false;
	float evac_time <- 0.0;
	bool after_running <- false;
	float after_time <- 0.0;
	float intensity <- 0.0;

	// the clock: the shaking and the walk run in real time, whatever the length of a GAMA cycle
	float last_machine_time <- -1.0;
	float real_dt <- 0.0;

	init {
		loop j from: 0 to: nz - 1 {
			loop i from: 0 to: nx - 1 {
				ground << ground_at(i, j);
				water << 0.0;
				delta << 0.0;
				out_sum << 0.0;
			}
		}
		loop j from: 0 to: nz - 2 {
			canal_a << j * nx + canal_i;
			canal_b << (j + 1) * nx + canal_i;
		}
		loop j from: 0 to: nz - 1 {
			loop i from: 0 to: nx - 1 {
				int c <- j * nx + i;
				if (i + 1 < nx) { land_a << c; land_b << c + 1; }
				if (j + 1 < nz and i != canal_i) { land_a << c; land_b << c + nx; }
			}
		}
		do create_neighbors;
	}

	// a point given in web coordinates, as a GAMA location
	point at_gama(float x, float z) {
		return {x + offset_x, z + offset_y};
	}

	float ground_at(int i, int j) {
		if (i <= 5 and j >= 20) { return 0.0; }       // the covered court
		if (i <= 3) { return -0.40; }                   // the yards
		if (i <= 5) { return -0.45; }                   // the west sidewalk
		if (i = canal_i) { return -1.35 - 0.20 * (24.5 - j) / 25.0; } // the canal's bed, lower to the north
		if (i <= 11) { return -0.60; }                  // the road
		return 0.0;                                     // the museum's terrace
	}

	float rain_at(float t) {
		float m <- t / 60.0;
		if (m <= rain_minute[0]) { return rain_rate[0]; }
		loop k from: 1 to: length(rain_minute) - 1 {
			if (m <= rain_minute[k]) {
				return rain_rate[k - 1] + (rain_rate[k] - rain_rate[k - 1]) * (m - rain_minute[k - 1]) / (rain_minute[k] - rain_minute[k - 1]);
			}
		}
		return 0.0;
	}

	float outlet_open {
		return max([outlet_min, 1.0 - outlet_per_piece * pieces_left]);
	}

	float door_depth {
		return water[door_j * nx + door_i];
	}

	// moves water between each pair of neighbouring cells, all from the same starting state
	action exchange (list<int> pa, list<int> pb, float k) {
		list<int> srcs <- [];
		list<int> dsts <- [];
		list<float> qs <- [];
		loop p from: 0 to: length(pa) - 1 {
			int c <- pa[p];
			int e <- pb[p];
			float ha <- ground[c] + water[c];
			float hb <- ground[e] + water[e];
			int src <- c;
			int dst <- e;
			float dh <- ha - hb;
			if (hb > ha) { src <- e; dst <- c; dh <- hb - ha; }
			if (dh > 1.0e-9) {
				float sill <- max([ground[c], ground[e]]);
				float avail <- ground[src] + water[src] - sill;
				if (avail > 0.0) {
					float f <- k * min([dh / 2.0, avail, water[src]]);
					srcs << src;
					dsts << dst;
					qs << f;
					out_sum[src] <- out_sum[src] + f;
				}
			}
		}
		if (!empty(qs)) {
			loop p from: 0 to: length(qs) - 1 {
				int s <- srcs[p];
				float f <- qs[p];
				if (out_sum[s] > water[s]) { f <- f * (water[s] / out_sum[s]); }
				delta[s] <- delta[s] - f;
				delta[dsts[p]] <- delta[dsts[p]] + f;
			}
		}
		loop c from: 0 to: ncell - 1 {
			water[c] <- max([0.0, water[c] + delta[c]]);
			delta[c] <- 0.0;
			out_sum[c] <- 0.0;
		}
	}

	// one step of dt seconds of the storm
	action storm_step {
		float add <- rain_at(storm_time) / 3600000.0 * dt;
		loop c from: 0 to: ncell - 1 { water[c] <- water[c] + add; }
		int south <- (nz - 1) * nx + canal_i;
		water[south] <- water[south] + upstream_area * add;
		total_in <- total_in + upstream_area * add + add * ncell;
		loop times: canal_passes { do exchange(canal_a, canal_b, k_canal); }
		do exchange(land_a, land_b, k_land);
		float o <- min([outlet_flow * outlet_open() * dt, water[canal_i]]);
		water[canal_i] <- water[canal_i] - o;
		total_out <- total_out + o;
		storm_time <- storm_time + dt;
		peak_door <- max([peak_door, door_depth()]);
	}

	action start_storm (int left) {
		pieces_left <- left;
		loop c from: 0 to: ncell - 1 { water[c] <- 0.0; }
		storm_time <- 0.0; storm_accum <- 0.0; total_in <- 0.0; total_out <- 0.0; peak_door <- 0.0;
		storm_running <- true;
	}

	action stop_storm {
		storm_running <- false;
		loop c from: 0 to: ncell - 1 { water[c] <- 0.0; }
	}

	reflex clock {
		float now <- gama.machine_time;
		real_dt <- last_machine_time < 0 ? 0.0 : min([0.5, (now - last_machine_time) / 1000.0]);
		last_machine_time <- now;
	}

	reflex run_storm when: storm_running {
		storm_accum <- storm_accum + real_dt * storm_speed;
		int n <- 0;
		loop while: storm_accum >= dt and storm_running and n < 60 {
			do storm_step;
			storm_accum <- storm_accum - dt;
			n <- n + 1;
			if (storm_time >= rain_seconds + 1800.0) { storm_running <- false; do storm_ended; }
		}
	}

	// told to the players by TANGHALAN VR.gaml
	action storm_ended { }

	// ---------- the earthquake ----------

	float quake_main (float t) {
		if (t < 0) { return 0.0; }
		if (t < ramp_up) { return 0.25 + 0.75 * t / ramp_up; }
		if (t < ramp_up + strong) { return 1.0; }
		if (t < ramp_up + strong + ramp_down) { return 1.0 - (t - ramp_up - strong) / ramp_down; }
		return 0.0;
	}

	float quake_after (float t) {
		if (t < 0 or t >= aftershock_duration) { return 0.0; }
		float k <- t < 1 ? t : (t > aftershock_duration - 2 ? (aftershock_duration - t) / 2 : 1.0);
		return aftershock_peak * k;
	}

	action start_quake {
		quake_running <- true; quake_time <- 0.0;
		evacuating <- false; after_running <- false;
		ask evacuee { do reset; }
	}

	action start_evacuation {
		evacuating <- true; evac_time <- 0.0;
		ask evacuee { started <- true; }
	}

	reflex run_quake when: quake_running {
		quake_time <- quake_time + real_dt;
		float i <- quake_main(quake_time);
		if (evacuating) {
			evac_time <- evac_time + real_dt;
			if (!after_running and evac_time >= aftershock_after) { after_running <- true; after_time <- 0.0; }
			if (after_running) { after_time <- after_time + real_dt; i <- quake_after(after_time); }
			if (after_running and after_time > aftershock_duration + 2 and empty(evacuee where !each.arrived)) { quake_running <- false; }
		}
		intensity <- i;
	}

	// ---------- the neighbors (Unity: Evacuation.Crowd) ----------

	action create_neighbors {
		list<float> houses <- [-12.6, -6.6, -0.6];
		list<point> spots <- [{-25.9, 0.0, 5.2}, {-25.1, 0.0, 5.4}, {-26.0, 0.0, 6.3}, {-25.2, 0.0, 6.5},
			{-24.3, 0.0, 6.0}, {-24.4, 0.0, 7.1}, {-23.4, 0.0, 6.6}, {-25.5, 0.0, 7.4}];
		// the shirts' colors, as in Unity (Tanghalan.Crowd)
		list<string> shirts <- ["c8782a", "e2588a", "2f74d0", "4caf50", "7a53c6", "f2b300", "2fa37a", "e2382b"];
		list<rgb> colors <- [rgb(200, 120, 42), rgb(226, 88, 138), rgb(47, 116, 208), rgb(76, 175, 80), rgb(122, 83, 198), rgb(242, 179, 0), rgb(47, 163, 122), rgb(226, 56, 43)];
		int k <- 0;
		loop h from: 0 to: 2 {
			float z <- houses[h];
			loop m from: 0 to: 1 {
				float side <- m = 0 ? -0.25 : 0.25;
				create evacuee {
					name <- "evacuee" + k;
					adult <- m = 0;
					shirt <- shirts[k];
				color <- colors[k];
					color <- colors[k];
					delay <- 1.0 + h * 2.5 + m * 0.8;
					speed_ms <- m = 0 ? 1.05 : 1.15;
					// web coordinates: {x, height, z}
					route <- [{-23.6, -0.4, z + side}, {-22.8, -0.42, z + side}, {-22.3 + side, -0.45, z + 0.6},
						{-22.3 + side, -0.45, 2.1}, {-22.3 + side, -0.15, 2.85}, {-22.8, 0.0, 3.6}, spots[k]];
					do reset;
				}
				k <- k + 1;
			}
		}
		loop m from: 0 to: 1 {
			float side <- m = 0 ? -0.3 : 0.3;
			create evacuee {
				name <- "evacuee" + k;
				adult <- false;
				shirt <- shirts[k];
				color <- colors[k];
				delay <- 3.0 + m * 0.6;
				speed_ms <- 1.2;
				route <- [{-22.0 + side, -0.45, -16.3}, {-22.0 + side, -0.45, 2.1}, {-22.0 + side, -0.15, 2.85}, {-22.6, 0.0, 3.6}, spots[k]];
				do reset;
			}
			k <- k + 1;
		}
	}
}

// a neighbor walking to the evacuation center
species evacuee {
	bool adult;
	string shirt;
	rgb color;
	float delay;
	float speed_ms;
	list<point> route;    // web coordinates {x, height, z}
	point web;            // where the neighbor is, in web coordinates
	float heading <- 90.0; // degrees; 0 faces north, 90 east
	int next_point <- 1;
	float t <- 0.0;
	bool started <- false;
	bool arrived <- false;

	action reset {
		web <- route[0]; next_point <- 1; t <- 0.0; started <- false; arrived <- false; heading <- 90.0;
		location <- {web.x + offset_x, web.z + offset_y};
	}

	reflex walk when: started and !arrived {
		t <- t + real_dt;
		if (t >= delay) {
			float go <- speed_ms * real_dt;
			loop while: go > 0 and next_point < length(route) {
				point a <- web;
				point b <- route[next_point];
				float d <- sqrt((b.x - a.x) ^ 2 + (b.z - a.z) ^ 2);
				if (d < 1.0e-6) {
					web <- b; next_point <- next_point + 1;
				} else {
					heading <- atan2(b.x - a.x, -(b.z - a.z));
					if (go >= d) {
						web <- b; go <- go - d; next_point <- next_point + 1;
					} else {
						float f <- go / d;
						web <- {a.x + (b.x - a.x) * f, a.y + (b.y - a.y) * f, a.z + (b.z - a.z) * f};
						go <- 0.0;
					}
				}
			}
			if (next_point >= length(route)) { arrived <- true; heading <- 0.0; }
		}
		location <- {web.x + offset_x, web.z + offset_y};
	}

	aspect default {
		draw circle(adult ? 0.35 : 0.25) color: color border: #black;
	}
}

experiment TANGHALAN type: gui {
	parameter "Pieces of trash left in the canal" var: pieces_left;
	user_command "Run the storm" { ask world { do start_storm(pieces_left); } }
	user_command "Start the earthquake" { ask world { do start_quake; } }
	user_command "Start the evacuation" { ask world { do start_evacuation; } }

	output {
		display street type: 2d {
			graphics "street" {
				// the museum, the court, the houses, the bins: an outline to find one's way
				draw rectangle(26, 29) at: {0.0 + offset_x, -5.5 + offset_y} color: rgb(243, 234, 217) border: #grey;
				draw "TANGHALAN" at: {-3.0 + offset_x, -5.5 + offset_y} color: #grey font: font("Helvetica", 18, #bold);
				draw rectangle(6, 5) at: {-24.0 + offset_x, 5.5 + offset_y} color: rgb(95, 174, 123, 120) border: #darkgreen;
				draw "Evacuation center" at: {-26.5 + offset_x, 8.5 + offset_y} color: #darkgreen font: font("Helvetica", 12, #bold);
				loop z over: [-12.6, -6.6, -0.6] {
					draw rectangle(3.3, 4.2) at: {-25.15 + offset_x, z + offset_y} color: rgb(240, 225, 195) border: #grey;
				}
				// the water, cell by cell
				loop j from: 0 to: nz - 1 {
					loop i from: 0 to: nx - 1 {
						float w <- water[j * nx + i];
						rgb col <- i = canal_i ? rgb(107, 94, 75) : (i >= 7 and i <= 11 ? rgb(91, 94, 99) : (i >= 12 ? rgb(217, 212, 199) : rgb(143, 174, 106)));
						if (w > 0.004) { col <- blend(rgb(40, 110, 200), col, min([1.0, 0.35 + w])); }
						draw square(1.0) at: {grid_x0 + i + 0.5 + offset_x, grid_z0 + j + 0.5 + offset_y} color: col;
					}
				}
				draw "Trash in the canal: " + pieces_left + "   Culvert open: " + int(outlet_open() * 100) + "%" at: {1, 1.5} color: #black font: font("Helvetica", 12, #plain);
				draw "Storm: " + int(storm_time / 60) + " min, rain " + int(rain_at(storm_time)) + " mm/h, water at Rico's door " + int(door_depth() * 100) + " cm" at: {1, 3} color: #black font: font("Helvetica", 12, #plain);
				draw "Shaking: " + int(intensity * 100) + "%" at: {1, 4.5} color: (intensity > 0.05 ? #red : #black) font: font("Helvetica", 12, #bold);
			}
			species evacuee;
		}
	}
}
