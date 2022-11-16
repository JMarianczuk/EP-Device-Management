library(RSQLite)
library(ggplot2)
library(optparse)
library(stringr)

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
battery_names <- dbGetQuery(con, "select battery from distinct_battery")[,1]#"10 kWh [10 kW]"

energy_in_name <-       "avg received energy [kWh]"
energy_out_name <-      "avg sent energy [kWh]"
energy_missed_name <-   "avg forfeited energy [kWh]"
wasted_name <-          "avg wasted energy [kWh]"
count_name <-           "share of successful passes"
# power_in_name <-        "avg power guards (in)"
# power_out_name <-       "avg power guards (out)"
# capacity_empty_name <-  "avg capacity guards (out)"
# capacity_full_name <-   "avg capacity guards (in)"
oscillation_name <-     "avg oscillation guards"
battery_min_name <-     "avg min battery SoC [kWh]"
battery_max_name <-     "avg max battery SoC [kWh]"
battery_avg_name <-     "avg battery SoC [kWh]"

get_min_energy <- function(data = "", battery = "") {
    query <- paste(
        "select",
        "min(totalIncoming_kwh)",
        "from",
        "data_stat",
        get_where(data = data, battery = battery))
    dbGetQuery(con, query)[1, 1]
}

get_query <- function(by, filter, reduce = FALSE, offset = 0) {
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
    query <- paste_if(
        count_query,
        outer_query("avg_energy_in", energy_in_name),
        if (reduce) "" else outer_query("avg_energy_out", energy_out_name),
        if (reduce) "" else outer_query("avg_gen_missed", energy_missed_name),
        if (reduce) "" else outer_query(paste("avg_sum - ", offset), wasted_name),
        # outer_query("avg_powerGuard_in", power_in_name),
        # outer_query("avg_powerGuard_out", power_out_name),
        # outer_query("avg_capacityGuard_empty", capacity_empty_name),
        # outer_query("avg_capacityGuard_full", capacity_full_name),
        if (reduce) "" else outer_query("avg_oscillationGuard", oscillation_name),
        outer_query("avg_min_battery", battery_min_name),
        outer_query("avg_max_battery", battery_max_name),
        outer_query("avg_avg_battery", battery_avg_name),
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
    filter_name = "",
    custom_colours = FALSE,
    custom_breaks = c(),
    reduce = FALSE,
    offset = 0) {
    res <- dbGetQuery(con, get_query(by = by, filter = filter, reduce = reduce, offset = offset))
    res$facet <- factor(
        res$facet,
        levels = c(
            count_name,
            energy_in_name,
            energy_out_name,
            energy_missed_name,
            wasted_name,
            # power_in_name,
            # power_out_name,
            # capacity_full_name,
            # capacity_empty_name,
            oscillation_name,
            battery_min_name,
            battery_max_name,
            battery_avg_name))
    thisplot <- ggplot(res, aesthetics) +
        geom_line() +
        expand_limits(y = 0) +
        facet_grid(rows = vars(facet), scales = "free_y", switch = "both") +
        labs(
            # title = paste_if(
            #     paste("success rate and average metrics for each", x_name, "and", group_name),
            #     filter_name,
            #     sep = "\n"),
            x = x_title) +
        theme(
            # plot.title = element_text(hjust = 0.5),
            legend.position = "bottom",
            axis.title.y = element_blank())

    if (length(highlight_x) != 0) {
        thisplot <- thisplot +
            geom_vline(xintercept = highlight_x, linetype = "dashed")
    }
    if (custom_colours == TRUE) {
        group <- by[length(by)]
        group_values_colours <- get_group_colours(group, res, con)
        group_values_linetypes <- get_group_linetypes(group, res, con)

        thisplot <- thisplot +
            # scale_colour_manual(
            #     group,
            #     values = group_values_colours,
            #     labels = label_wrap_gen(width = 29)) +
            # theme(legend.key.size = unit(0.8, 'cm'))
            scale_colour_manual(
                group,
                values = group_values_colours)
    }
    if (length(custom_breaks) != 0) {
        thisplot <- thisplot +
            scale_x_continuous(breaks = custom_breaks)
    }

    ggsave(filename, thisplot, width = 30, height = if (reduce) 25 else 40, units = "cm")
}

plot_pp <- function(
    first_group,
    group,
    group_name,
    x_title,
    x_name,
    highlight_x,
    custom_colours = FALSE) {
    plot_int <- function(
        by,
        filtered = c(),
        where = "",
        filter_name = "",
        h_x = highlight_x,
        reduce = FALSE,
        offset = 0) {
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
              ".pdf"),
            h_x,
            filter = where,
            filter_name = filter_name,
            custom_colours = custom_colours,
            reduce = reduce,
            offset = offset)
    }
    by <- c(first_group, group)
    preprocess_counts(by)
    plot_int(by)
    if (group != "battery") {
        by <- c(first_group, "battery", group)
        preprocess_counts(by)
        for (b_name in battery_names) {
            plot_int(
                by,
                c(b_name),
                get_where(battery = b_name),
                filter_name = paste("for the", b_name, "battery"),
                h_x = head(highlight_x, -1))
        }
    }
    if (group != "data") {
        by <- c(first_group, "data", group)
        preprocess_counts(by)
        for (d_name in data_names) {
            plot_int(
                by,
                c(d_name),
                get_where(data = d_name),
                filter_name = paste("for the", d_name, "data set"),
                reduce = str_ends(d_name, "noSolar"),
                offset = get_min_energy(data = d_name))
        }
    }
    if (group != "battery" && group != "data") {
        by <- c(first_group, "battery", "data", group)
        preprocess_counts(by)
        for (b_name in battery_names) {
            for (d_name in data_names) {
                plot_int(
                    by,
                    c(b_name, d_name),
                    get_where(battery = b_name, data = d_name),
                    filter_name = paste("for the", d_name, "data set and the", b_name, "battery"),
                    h_x = head(highlight_x, -1),
                    reduce = str_ends(d_name, "noSolar"),
                    offset = get_min_energy(battery = b_name, data = d_name))
            }
        }
    }
}

plot_packetSize <- function(group, group_name, custom_colours = FALSE) {
    plot_pp("packetSize", group, group_name,"packet size [kWh]", "packet size",
            c(3, 4) / 3, custom_colours = custom_colours)
}

plot_probability <- function(group, group_name, custom_colours = FALSE) {
    plot_pp("probability", group, group_name,"probability", "probability",
            c(), custom_colours = custom_colours)
}

plot_by_range <- function(group, group_name, select, range_name, custom_colours = FALSE) {
    plot_int <- function(
        by,
        filtered = c(),
        where = "",
        filter_name = "",
        offset = 0) {
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
                ".pdf"),
            filter = where,
            filter_name = filter_name,
            custom_colours = custom_colours,
            offset = offset,
            custom_breaks = seq(0, 1, 0.2))
    }
    preprocess_counts_int <- function(by) {
        select_by <- by
        select_by[1] <- paste(select, "as", range_name)
        preprocess_counts(by, select_by)
    }
    by <- c(range_name, group)
    preprocess_counts_int(by)
    plot_int(by)
    if (group != "battery") {
        by <- c(range_name, "battery", group)
        preprocess_counts_int(by)
        for (b_name in battery_names) {
            plot_int(
                by,
                c(b_name),
                get_where(battery = b_name),
                filter_name = paste("for the", b_name, "battery"))
        }
    }
    if (group != "data") {
        by <- c(range_name, "data", group)
        preprocess_counts_int(by)
        for (d_name in data_names) {
            plot_int(
                by,
                c(d_name),
                get_where(data = d_name),
                filter_name = paste("for the", d_name, "data set"),
                offset = get_min_energy(data = d_name))
        }
    }
    if (group != "battery" && group != "data") {
        by <- c(range_name, "battery", "data", group)
        preprocess_counts_int(by)
        for (b_name in battery_names) {
            for (d_name in data_names) {
                plot_int(
                    by,
                    c(b_name, d_name),
                    get_where(battery = b_name, data = d_name),
                    filter_name = paste("for the", d_name, "data set and the", b_name, "battery"),
                    offset = get_min_energy(battery = b_name, data = d_name)
                )
            }
        }
    }
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
        "cast(substr(configuration, 8, 3) as NUMERIC)",
        "upper",
        custom_colours)
}

for (g in c(
    "strategy",
    "data",
    # "configuration",
    "battery")) {
    custom_colours <- g == "strategy"

    plot_packetSize(g, g, custom_colours = custom_colours)
    plot_probability(g, g, custom_colours = custom_colours)

    plot_lower_range(g, g, custom_colours = custom_colours)
    plot_upper_range(g, g, custom_colours = custom_colours)
}
plot_packetSize("probability", "probability")
plot_probability("packetSize", "packet size")

# plot_lower_range("probability")
# plot_lower_range("packetSize")
# plot_upper_range("probability")
# plot_upper_range("packetSize")