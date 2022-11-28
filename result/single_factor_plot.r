library(RSQLite)
library(ggplot2)
library(optparse)
library(stringr)
library(R.utils)

option_list <- list(
    make_option("--legend", type = "logical", default = FALSE)
)
opt_parser <- OptionParser(option_list = option_list)
options <- parse_args(opt_parser)

source("preprocessing/energy_counts.r")
source("preprocessing/distinct_values.r")

source("r_helpers/sql_helpers.r")
source("r_helpers/string_helpers.r")
source("r_helpers/array_helpers.r")
source("r_helpers/ggplot_helpers.r")
source("r_helpers/colour_helpers.r")

con <- create_db_connection()

# y axis is energy_in
# x axis is some parameter like packet size, probability, configuration
# color / shape is data / strategy

#first plot: probability vs. energy_in by strategy

preprocess_distinct(con)

data_names <- dbGetQuery(con, "select data from distinct_data")[,1]
battery_names <- dbGetQuery(con, "select battery from distinct_battery")[,1]
fails <- dbGetQuery(con, "select fail from distinct_fail")[,1]

energy_in_name <-       "received\nenergy\n[kWh]"
energy_out_name <-      "sent\nenergy\n[kWh]"
energy_curtailed_name <- "curtailed\nenergy\n[kWh]"
wasted_name <-          "wasted\nenergy\n[kWh]"
count_name <-           "success\nrate"
power_in_name <-        "receive\npower\nguards"
power_out_name <-       "send\npower\nguards"
# capacity_empty_name <-  "capacity guards (out)"
# capacity_full_name <-   "capacity guards (in)"
# oscillation_name <-     "oscillation guards"
discharge_power_fail_name <- "fail rate\ndischarge\npower\nexceeded"
charge_power_fail_name <- "fail rate\ncharge\npower\nexceeded"
battery_empty_fail_name <- "fail rate\nbattery\ndischarged\ncompletely"
battery_min_name <-     "minimum\nSoC\n[kWh]"
battery_max_name <-     "maximum\nSoC\n[kWh]"
battery_avg_name <-     "average\nSoC\n[kWh]"

get_min_energy <- function(data = "", battery = "") {
    query <- paste(
        "select",
        "min(totalIncoming_kwh)",
        "from",
        "data_stat",
        get_where(data = data, battery = battery))
    dbGetQuery(con, query)[1, 1]
}

get_query <- function(by, filter, reduce = FALSE, offset = 0, with_fails = c()) {
    query <- paste(
        "select",
        "*",
        "from",
        energy_count_table_name(by),
        filter
    )
    outer_query <- function(alias, name) {
        paste(
            "select",
            paste(by, collapse = ","),
            ",",
            paste(alias, "as value,"),
            paste0('"', name, '" as facet'),
            "from",
            "(",
            query,
            ")")
    }
    total_query <- paste(
        "select",
        "*",
        "from",
        total_count_table_name(by),
        filter
    )
    count_query <- paste(
        "select",
        paste0("succ.", by, collapse = ","),
        ",",
        "cast(succ.count as REAL) / total.totalCount as value,",
        paste0('"', count_name, '" as facet'),
        "from",
        paste(
            "(",
                "(",
                query,
                ") succ,",
                "(",
                total_query,
                ") total",
            ")"),
        "where",
        paste0("succ.", by, "=", "total.", by, collapse = " and "))
    fails_query <- function(fail_name, name) {
        paste(
            "select",
            paste(
                paste(by, collapse = ","),
                paste("cast(", fail_name, "as REAL) / totalCount as value"),
                paste0("'", name, "' as facet"),
                sep = ","),
            "from",
            "(",
            total_query,
            ")")
    }
    query <- paste_if(
        count_query,
        outer_query("avg_energy_in", energy_in_name),
        if (reduce) "" else outer_query("avg_energy_out", energy_out_name),
        if (reduce) "" else outer_query("avg_curtailed_energy", energy_curtailed_name),
        outer_query(paste("avg_sum - ", offset), wasted_name),
        outer_query("avg_powerGuard_in", power_in_name),
        if (reduce) "" else outer_query("avg_powerGuard_out", power_out_name),
        # outer_query("avg_capacityGuard_empty", capacity_empty_name),
        # outer_query("avg_capacityGuard_full", capacity_full_name),
        # if (reduce) "" else outer_query("avg_oscillationGuard", oscillation_name),
        outer_query("avg_min_battery", battery_min_name),
        outer_query("avg_max_battery", battery_max_name),
        outer_query("avg_avg_battery", battery_avg_name),
        if (discharge_power_fail_name %in% with_fails) fails_query("ExceedDischargePower", discharge_power_fail_name) else "",
        if (charge_power_fail_name %in% with_fails) fails_query("ExceedChargePower", charge_power_fail_name) else "",
        if (battery_empty_fail_name %in% with_fails) fails_query("BelowZero", battery_empty_fail_name) else "",
        sep = " UNION "
    )
    query
}

preprocess_counts <- function(
    by,
    select_by = c(),
    filter = "") {
    preprocess_energy_counts(by, con, select_by = select_by, filter = filter)
    preprocess_total_counts(by, con, select_by = select_by, filter = filter)
}

add_line_breaks <- function(list) {
    str_replace(list, "\\+", "\n\\+")
}

do_plot <- function(
    by,
    aesthetics,
    x_title,
    x_name,
    group_name,
    filename,
    highlight_x = c(),
    filter = "",
    custom_colours = FALSE,
    custom_breaks = waiver(),
    custom_minor_breaks = waiver(),
    custom_expand_x = c(),
    reduce = FALSE,
    legend_reduce = FALSE,
    offset = 0,
    with_fails = c(),
    box = FALSE) {
    query <- get_query(by = by, filter = filter, reduce = reduce, offset = offset, with_fails = with_fails)
    res <- dbGetQuery(con, query)
    if (nrow(res) == 0) return()
    res$facet <- factor(
        res$facet,
        levels = c(
            count_name,
            discharge_power_fail_name,
            charge_power_fail_name,
            battery_empty_fail_name,
            energy_in_name,
            energy_out_name,
            energy_curtailed_name,
            wasted_name,
            power_in_name,
            power_out_name,
            # capacity_full_name,
            # capacity_empty_name,
            # oscillation_name,
            battery_min_name,
            battery_max_name,
            battery_avg_name))
    thisplot <- ggplot(res, aesthetics) +
        (if (box) geom_boxplot() else geom_line()) +
        (if (length(custom_expand_x) != 0)
            expand_limits(x = custom_expand_x, y = 0)
        else
            expand_limits(y = 0))+
        facet_grid(rows = vars(facet), scales = "free_y", switch = "both") +
        scale_x_continuous(
            breaks = custom_breaks,
            minor_breaks = custom_minor_breaks,
            sec.axis = dup_axis(name = waiver())) +
        labs(
            # title = paste_if(
            #     paste("success rate and average metrics for each", x_name, "and", group_name),
            #     filter_name,
            #     sep = "\n"),
            x = x_title,
            colour = capitalize(group_name)) +
        theme(
            # plot.title = element_text(hjust = 0.5),
            legend.position = "bottom",
            axis.title.y = element_blank(),
            strip.text.y.left = element_text(angle=0),
            strip.placement = "outside",
            strip.background = element_rect())

    if (length(highlight_x) != 0) {
        thisplot <- thisplot +
            geom_vline(xintercept = highlight_x, linetype = "dashed")
    }

    group <- by[length(by)]
    group_values_colours <- get_group_colours(group, res, con)
    # group_values_linetypes <- get_group_linetypes(group, res, con)

    thisplot <- thisplot +
        # scale_colour_manual(
        #     group,
        #     values = group_values_colours,
        #     labels = label_wrap_gen(width = 29)) +
        # theme(legend.key.size = unit(0.8, 'cm'))
        scale_colour_manual(
            capitalize(group_name),
            values = group_values_colours)
    if (custom_colours) {
        thisplot <- thisplot +
        guides(colour = guide_legend(nrow = if (legend_reduce) 1 else 3)) +
        theme(
            legend.margin = margin(l = -2.5, unit = "cm")
        )
    }

    ggsave(
        filename,
        thisplot,
        width = 16,
        height = (if (reduce) 15 else 22) + length(with_fails) * 2,
        units = "cm")
}

do_plot_with_filters <- function(
    preprocess_int,
    plot_int,
    first_group,
    group,
    highlight_x = c(),
    get_reduce = function(d) { FALSE }) {

    get_hx <- function(b_name) {
        if (length(highlight_x) == 0)
                c()
            else if (b_name == "A")
                highlight_x[1]
            else
                highlight_x[2]
    }
    gc <- c("p(0.0, 9.0) c(0.7, 0.5)")
    gc_name <- "p9kW"
    gc_group <- "guardConfiguration"
    top_strat <- "strategy in ('PEM Control', 'PRC + EST', 'PRC + DIR + EST')"
    top_strat_name <- "TopStrat"


    by <- c(first_group, group)
    preprocess_int(by)
    plot_int(by)
    if (group == "strategy") {
        plot_int(by, top_strat_name, get_where(and = top_strat), legend_reduce = TRUE)
    }
    if (group != gc_group) {
        by <- c(first_group, gc_group, group)
        preprocess_int(by)
        plot_int(by, c(gc_name), get_where(guardConfiguration = gc))
        if (group == "strategy") {
            plot_int(
                by,
                c(gc_name, top_strat_name),
                get_where(guardConfiguration = gc, and = top_strat),
                legend_reduce = TRUE)
        }
    }
    if (group != "battery") {
        by <- c(first_group, "battery", group)
        preprocess_int(by)
        for (b_name in battery_names) {
            plot_int(
                by,
                c(b_name),
                get_where(battery = b_name),
                h_x = get_hx(b_name))
            if (group == "strategy") {
                plot_int(
                    by,
                    c(b_name, top_strat_name),
                    get_where(battery = b_name, and = top_strat),
                    h_x = get_hx(b_name),
                    legend_reduce = TRUE)
            }
        }
    }
    if (group != "battery" && group != gc_group) {
        by <- c(first_group, "battery", gc_group, group)
        preprocess_int(by)
        for (b_name in battery_names) {
            plot_int(
                by,
                c(b_name, gc_name),
                get_where(battery = b_name, guardConfiguration = gc),
                h_x = get_hx(b_name))
            if (group == "strategy") {
                plot_int(
                    by,
                    c(b_name, gc_name, top_strat_name),
                    get_where(battery = b_name, guardConfiguration = gc, and = top_strat),
                    h_x = get_hx(b_name),
                    legend_reduce = TRUE)
            }
        }
    }
    if (group != "data") {
        by <- c(first_group, "data", group)
        preprocess_int(by)
        for (d_name in data_names) {
            plot_int(
                by,
                c(d_name),
                get_where(data = d_name),
                reduce = get_reduce(d_name),
                offset = get_min_energy(data = d_name))
            if (group == "strategy") {
                plot_int(
                    by,
                    c(d_name, top_strat_name),
                    get_where(data = d_name, and = top_strat),
                    reduce = get_reduce(d_name),
                    offset = get_min_energy(data = d_name),
                    legend_reduce = TRUE)
            }
        }
    }
    if (group != "data" && group != gc_group) {
        by <- c(first_group, "data", gc_group, group)
        preprocess_int(by)
        for (d_name in data_names) {
            plot_int(
                by,
                c(d_name, gc_name),
                get_where(data = d_name, guardConfiguration = gc),
                reduce = get_reduce(d_name),
                offset = get_min_energy(data = d_name))
            if (group == "strategy") {
                plot_int(
                    by,
                    c(d_name, gc_name, top_strat_name),
                    get_where(data = d_name, guardConfiguration = gc, and = top_strat),
                    reduce = get_reduce(d_name),
                    offset = get_min_energy(data = d_name),
                    legend_reduce = TRUE)
            }
        }
    }
    if (group != "battery" && group != "data") {
        by <- c(first_group, "battery", "data", group)
        preprocess_int(by)
        for (b_name in battery_names) {
            for (d_name in data_names) {
                plot_int(
                    by,
                    c(b_name, d_name),
                    get_where(battery = b_name, data = d_name),
                    h_x = get_hx(b_name),
                    reduce = get_reduce(d_name),
                    offset = get_min_energy(battery = b_name, data = d_name))
                if (group == "strategy") {
                    plot_int(
                        by,
                        c(b_name, d_name, top_strat_name),
                        get_where(battery = b_name, data = d_name, and = top_strat),
                        h_x = get_hx(b_name),
                        reduce = get_reduce(d_name),
                        offset = get_min_energy(battery = b_name, data = d_name),
                        legend_reduce = TRUE)
                }
            }
        }
    }
    if (group != "battery" && group != "data" && group != gc_group) {
        by <- c(first_group, "battery", "data", gc_group, group)
        preprocess_int(by)
        for (b_name in battery_names) {
            for (d_name in data_names) {
                plot_int(
                    by,
                    c(b_name, d_name, gc_name),
                    get_where(battery = b_name, data = d_name, guardConfiguration = gc),
                    h_x = get_hx(b_name),
                    reduce = get_reduce(d_name),
                    offset = get_min_energy(battery = b_name, data = d_name))
                if (group == "strategy") {
                    plot_int(
                        by,
                        c(b_name, d_name, gc_name, top_strat_name),
                        get_where(battery = b_name, data = d_name, guardConfiguration = gc, and = top_strat),
                        h_x = get_hx(b_name),
                        reduce = get_reduce(d_name),
                        offset = get_min_energy(battery = b_name, data = d_name),
                        legend_reduce = TRUE)
                }
            }
        }
    }
}

plot_pp <- function(
    first_group,
    group,
    group_name,
    x_title,
    x_name,
    highlight_x,
    custom_breaks = waiver(),
    custom_minor_breaks = waiver(),
    custom_expand_x = c(),
    custom_colours = FALSE,
    with_fails = c(),
    box = FALSE) {
    plot_int <- function(
        by,
        filtered = c(),
        where = "",
        h_x = highlight_x,
        reduce = FALSE,
        offset = 0,
        legend_reduce = FALSE) {
        preprocess_counts(by)
        do_plot(
            by,
            aes(
                x = .data[[first_group]],
                y = value,
                color = .data[[group]],
                group = .data[[group]]),
            x_title,
            x_name,
            group_name,
            paste0(
              "plots/single_factor_",
              first_group,
              "_",
              if (length(filtered) == 0) "" else paste0(paste(filtered, collapse = "_"), "_"),
              group,
              if (box) "_box" else "",
              ".pdf"),
            h_x,
            custom_breaks = custom_breaks,
            custom_minor_breaks = custom_minor_breaks,
            custom_expand_x = custom_expand_x,
            filter = get_where(
                where,
                and = if (length(h_x) == 0) "" else paste(first_group, "<=", max(h_x) + 0.02)),
            custom_colours = custom_colours,
            reduce = reduce,
            legend_reduce = legend_reduce,
            offset = offset,
            with_fails = with_fails,
            box = box)
    }
    do_plot_with_filters(
        preprocess_counts,
        plot_int,
        first_group,
        group,
        highlight_x = highlight_x,
        get_reduce = function(d_name) { str_ends(d_name, "L") })
}

plot_packetSize <- function(group, group_name, custom_colours = FALSE, box = FALSE) {
    plot_pp("packetSize", group, group_name,"packet size [kWh]", "packet size",
            c(3, 4) / 3, custom_breaks = seq(from = 0, to = 1.4, by = 0.1), custom_colours = custom_colours,
            with_fails = c(charge_power_fail_name, battery_empty_fail_name), box = box)
}

plot_probability <- function(group, group_name, custom_colours = FALSE) {
    plot_pp("probability", group, group_name,"probability", "probability",
            c(), custom_breaks = seq(from = 0, to = 1, by = 0.1),
            custom_minor_breaks = seq(0, 1, 0.1), custom_expand_x = 0.1, custom_colours = custom_colours)
}

plot_by_range <- function(group, group_name, select, range_name, custom_colours = FALSE, filter = "", filter_name = "") {
    plot_int <- function(
        by,
        filtered = c(),
        where = "",
        h_x = c(),
        reduce = FALSE,
        offset = 0,
        legend_reduce = FALSE) {
        do_plot(
            by,
            aes(
                x = .data[[range_name]],
                y = value,
                color = .data[[group]],
                group = .data[[group]]),
            x_title = paste(range_name, "configuration range"),
            x_name = paste(range_name, "configuration"),
            group_name = group_name,
            paste0(
                "plots/single_factor_",
                range_name,
                "_",
                if (length(filtered) == 0) "" else paste0(paste(filtered, collapse = "_"), "_"),
                group,
                if (filter_name == "") "" else paste0("_", filter_name),
                ".pdf"),
            filter = get_where(where, and = filter),
            custom_colours = custom_colours,
            offset = offset,
            reduce = reduce,
            legend_reduce = legend_reduce,
            custom_breaks = seq(0, 1, 0.2))
    }
    preprocess_counts_int <- function(by) {
        select_by <- by
        select_by[1] <- paste(select, "as", range_name)
        preprocess_counts(by, select_by)
    }
    do_plot_with_filters(
        preprocess_counts_int,
        plot_int,
        range_name,
        group,
        get_reduce = function(d_name) { str_ends(d_name, "L") })
}

plot_lower_range <- function(group, group_name, custom_colours = FALSE) {
    plot_by_range(
        group,
        group_name,
        "cast(substr(configuration, 2, 3) AS NUMERIC)",
        "lower",
        custom_colours)
}

plot_upper_range <- function(group, group_name, custom_colours = FALSE) {
    plot_by_range(
        group,
        group_name,
        "cast(substr(configuration, 7, 3) as NUMERIC)",
        "upper",
        custom_colours)
}

plot_power_guards_out <- function(group, group_name, custom_colours = FALSE) {
    plot_int <- function(
        by,
        filtered = c(),
        where = "",
        h_x = c(),
        reduce = FALSE,
        offset = 0,
        legend_reduce = FALSE) {
        do_plot(
            by,
            aes(
                x = guardConfiguration,
                y = value,
                colour = .data[[group]],
                group = .data[[group]]),
            x_title = "Send Power Guard Safety Margin [kW]",
            x_name = "send power guard configuration",
            group_name = group_name,
            paste0(
                "plots/single_factor_",
                "power_guard_send_",
                if (length(filtered) == 0) "" else paste0(paste(filtered, collapse = "_"), "_"),
                group,
                ".pdf"),
            filter = where,
            custom_colours = custom_colours,
            custom_breaks = seq(0, 20, 1),
            custom_minor_breaks = seq(0, 20, 1),
            offset = offset,
            reduce = reduce,
            legend_reduce = legend_reduce,
            with_fails = c(discharge_power_fail_name))
    }
    preprocess_counts_int <- function(by) {
        select_by <- by
        select_by[1] <- "cast(substr(guardConfiguration, 8, 3) as NUMERIC) as guardConfiguration"
        preprocess_counts(by, select_by, filter = "length(guardConfiguration) > 18") #small config is 13 long, big one 23
    }
    do_plot_with_filters(
        preprocess_counts_int,
        plot_int,
        "guardConfiguration",
        group,
        get_reduce = function(d_name) { str_ends(d_name, "L") })
}

for (g in c(
    "strategy"
    , "data"
    # , "configuration"
    , "battery"
)) {
    custom_colours <- g == "strategy"

    plot_packetSize(g, g, custom_colours = custom_colours)
    plot_probability(g, g, custom_colours = custom_colours)

    plot_lower_range(g, g, custom_colours = custom_colours)
    plot_upper_range(g, g, custom_colours = custom_colours)

    plot_power_guards_out(g, g, custom_colours = custom_colours)
}
# plot_packetSize("probability", "probability")
# plot_probability("packetSize", "packet size")
#
# plot_lower_range("probability", "probability")
# plot_lower_range("packetSize", "packet size")
# plot_upper_range("probability", "probability")
# plot_upper_range("packetSize", "packet size")

dbDisconnect(con)