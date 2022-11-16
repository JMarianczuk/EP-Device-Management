library(RSQLite)
library(ggplot2)
library(optparse)
library(stringr)
library(showtext)

option_list <- list(
    make_option("--strategy", type = "character", default = "")
)
opt_parser <- OptionParser(option_list = option_list)
options <- parse_args(opt_parser)

source("r_helpers/sql_helpers.r")
source("r_helpers/string_helpers.r")
source("r_helpers/array_helpers.r")
source("r_helpers/font_helpers.r")

source("preprocessing/successful_counts.r")
source("preprocessing/distinct_values.r")
source("preprocessing/energy_stat.r")

load_fonts()
showtext_auto()

con <- create_db_connection()

preprocess_successful_counts(c("strategy", "configuration"), "sc", con)
preprocess_successful_counts(c("strategy", "configuration", "data"), "scd", con)
preprocess_successful_counts(c("strategy", "configuration", "data", "battery"), "scdb", con)
preprocess_successful_counts(c("strategy", "guardConfiguration", "data", "battery"), "sgdb", con)
preprocess_successful_counts(c("strategy", "configuration", "data", "timeStep"), "scdt", con)
preprocess_successful_counts(c("strategy", "configuration", "data", "battery", "timeStep"), "scdbt", con)
preprocess_successful_counts(c("strategy", "guardConfiguration", "data", "battery", "timeStep"), "sgdbt", con)

preprocess_energy_stats(con)
preprocess_distinct(con)

data_configurations <- dbGetQuery(con, "select data from distinct_data")[,1]
battery_configurations <- dbGetQuery(con, "select battery from distinct_battery")[,1]
time_steps <- dbGetQuery(con, "select timeStep from distinct_timeStep")[,1]
strategy_names <- dbGetQuery(con, "select strategy from distinct_strategy")[,1]

number_of_packet_sizes <- dbGetQuery(con, "select count(packetSize) from distinct_packetSize")[1, 1]
number_of_probabilities <- dbGetQuery(con, "select count(probability) from distinct_probability")[1, 1]
number_of_batteries <- dbGetQuery(con, "select count(battery) from distinct_battery")[1, 1]
number_of_combinations <- number_of_batteries * number_of_packet_sizes * number_of_probabilities

get_short <- function(strat_name = "", data_name = "", battery_name = "", timestep_name = "", config_type = "configuration") {
    res <- ""
    if (strat_name != "") {
        res <- paste0(res, "s")
    }
    if (config_type == "configuration") {
        res <- paste0(res, "c")
    } else if (config_type == "guardConfiguration") {
        res <- paste0(res, "g")
    }
    if (data_name != "") {
        res <- paste0(res, "d")
    }
    if (battery_name != "") {
        res <- paste0(res, "b")
    }
    if (timestep_name != "") {
        res <- paste0(res, "t")
    }
    res
}

get_query_ <- function(where, table) {
    query <- paste(
        "select",
        "*",
        "from",
        table,
        where)
    query
}

get_query <- function(data_name = "", battery_name = "", strat_name = "", timestep_name = "", config_type = "configuration", filter = "") {
    where <- get_where(
        success = "True",
        data = data_name,
        battery = battery_name,
        strategy = strat_name,
        timeStep = timestep_name,
        topTen = 1,
        and = filter)
    table <- paste0("simulation_with_counts_", get_short(strat_name, data_name, battery_name, timestep_name, config_type))
    get_query_(where, table)
}

get_max_query_ <- function(where, table, value) {
    query <- paste(
        "select",
        paste0("max(", value, ") as max_value"), #the stat table only contains max for each configuration, get the max of those here
        "from",
        table,
        where)
    query
}

get_min_query_ <- function(where, table) {
    query <- paste(
        "select",
        "min(totalIncoming_kwh)",
        "from",
        table,
        where)
    query
}

with_without_switch <- function(data) {
    res <- ""
    if (endsWith(data, "noSolar")) {
        res <- substring(data, 1, 4)
    } else {
        res <- paste(data, "noSolar")
    }
    res
}

top_two_strategies <- c("Probabilistic Range Control + Direction + Estimation", "Probabilistic Range Control + Direction + Estimation + NoOutgoing")

get_strategy_top_two <- function(strategy_name) {
    if (strategy_name == "") {
        ""
    } else if (strategy_name %in% top_two_strategies) {
        paste0("strategy in (", paste0(quoted(top_two_strategies), collapse = ","), ")")
    } else {
        paste0("strategy =", quoted(strategy_name))
    }
}

get_max_query <- function(data_name = "", battery_name = "", strat_name = "", timestep_name = "", config_type = "configuration", filter = "", value = "maximum_in") {
    where <- get_where(
        success = "True",
        # strategy = strat_name,
        battery = battery_name,
        timeStep = timestep_name,
        and = get_strategy_top_two(strat_name))
    succ_count_table_name <- paste0(
        "successful_counts_",
        get_short(strat_name, data_name, battery_name, timestep_name, config_type = config_type))
    inner_where <- get_where(
        quote_values = FALSE,
        success = "outer_query.success",
        strategy = "outer_query.strategy",
        battery = "outer_query.battery",
        timeStep = "outer_query.timeStep",
        topTen = 1)
    table <- paste0("energy_", get_short(strat_name, data_name, battery_name, timestep_name, config_type = config_type), "_stat outer_query")
    combine_where <- function(where, inner_where) {
        append_where(where, paste(config_type, "in (select", config_type, "from", succ_count_table_name, inner_where, ")"))
    }
    if (data_name != "") {
        where <- append_where(where, paste0("data in (", quoted(data_name), ",", quoted(with_without_switch(data_name)), ")"))
    }
        query <- get_max_query_(combine_where(where, inner_where), table, value = value)
        query
}

get_min_query <- function(data_name, battery_name) {
    where <- get_where(
        data = data_name,
        battery = battery_name)
    get_min_query_(where, "data_stat")
}

get_min_query2 <- function() {
    get_min_query_("", "data_stat")
}

get_number_of_configurations <- function(strat_name, data_name = "", battery_name = "", timestep_name = "", config_type = "configuration", filter = "") {
    where <- get_where(
        strategy = strat_name,
        data = data_name,
        battery = battery_name,
        timeStep = timestep_name,
        topTen = 1)
    table_name <- paste0("successful_counts_", get_short(strat_name, data_name, battery_name, timestep_name, config_type = config_type))
    query <- paste(
        "SELECT",
        "count(*)",
        "FROM",
        table_name,
        where)
    res <- dbGetQuery(con, query)[1, 1]
    res
}

switch_other_config_type <- function(config_type) {
    if (config_type == "configuration") {
        "guardConfiguration"
    } else if (config_type == "guardConfiguration") {
        "configuration"
    } else {
        break
    }
}

get_number_of_combinations <- function(strat_name, data_name = "", battery_name = "", timestep_name = "", config_type = "configuration", filter ="") {
    other_configuration_count <- get_number_of_configurations(strat_name, data_name, battery_name, timestep_name, switch_other_config_type(config_type), filter)

    number_of_combinations * other_configuration_count
}

get_plot_height <- function(number_of_configurations) {
    plot_height <- 6 + number_of_configurations / 3
    plot_height
}

expand_right <- function(upper_limit) {
    upper_limit * 1.05
}

do_plot_int <- function(
    data_name,
    battery_name,
    strat_name,
    timestep_name,
    y_name = "configuration",
    title,
    file_name,
    filter = "",
    filter_name = "") {
    res <- dbGetQuery(con, get_query(data_name, battery_name, strat_name, timestep_name, config_type = y_name, filter = filter))
    if (nrow(res) == 0) {
        return()
    }
    # filter_name <- paste(
    #     "for",
    #     paste_if(
    #         if (data_name != "") paste("the", data_name, "data set") else "",
    #         if (battery_name != "") paste("the", battery_name, "battery") else "",
    #         filter_name,
    #         sep = " and "))

    query <- get_max_query(data_name, battery_name, strat_name, timestep_name, config_type = y_name, filter = filter)
    max_energy <- dbGetQuery(con, query)[1, 1]
    min_energy <- dbGetQuery(con, get_min_query(data_name, battery_name))[1, 1]
    min_energy_2 <- dbGetQuery(con, get_min_query(data_name, "No Battery"))[1, 1]
    min_energy_3 <- dbGetQuery(con, get_min_query(with_without_switch(data_name), battery_name))[1, 1]
    number_of_configurations <- get_number_of_configurations(
        strat_name,
        data_name = data_name,
        battery_name = battery_name,
        timestep_name = timestep_name,
        config_type = y_name,
        filter = filter)
    strat_with_direction <- if (str_contains(strat_name, "Direction")) TRUE else FALSE
    data_has_solar <- !str_ends(data_name, "noSolar")
    draw_second_vertical_line <- data_has_solar
    min_energy_breaks <- if (draw_second_vertical_line) {
        if (!is.na(min_energy_3)) {
            c(min_energy, min_energy_2, min_energy_3)
        } else {
            c(min_energy, min_energy_2)
        }
    } else {
        min_energy
    }
    this_number_of_combinations <- get_number_of_combinations(
        strat_name,
        data_name = data_name,
        battery_name = battery_name,
        timestep_name = timestep_name,
        config_type = y_name,
        filter = filter)
    thisplot <- ggplot(
        res,
        aes(
            x = energy_kwh_in,
            y = reorder(paste(pad_left(.data[[y_name]], data = "[0.7, 1.0] (0.04)"), "x", format_percent(count / this_number_of_combinations)), -ordering)
        )) +
        geom_boxplot() +
        expand_limits(x = c(0, expand_right(max_energy))) +
        scale_x_continuous(
            expand = c(0, 0),
            sec.axis = sec_axis(~., breaks = min_energy_breaks, labels = function(x) {round(x, digits = 1)})) +
        labs(
            # title = paste(
            #     paste("Required incoming energy for each", strat_name, "configuration"),
            #     "Ordered by descending number of successful control passes",
            #     filter_name,
            #     sep = "\n"),
            x = "Total received energy [kWh]",
            y = "Configuration x Successful share") +
        theme(
            # plot.title = element_text(hjust = 0.5),
            # plot.title.position = "plot",
            axis.text.y = element_text(family = "Lucida Console", face = "bold")
        ) +
        geom_vline(xintercept = min_energy, colour = "blue", linetype = "dashed")
    if (draw_second_vertical_line) {
        thisplot <- thisplot +
            geom_vline(xintercept = min_energy_2, colour = "dimgray", linetype = "dashed")
        if (!is.na(min_energy_3)) {
            thisplot <- thisplot +
                geom_vline(xintercept = min_energy_3, colour = "lightblue", linetype = "dashed")
        }
    }
    ggsave(file_name, thisplot, width = 25, height = get_plot_height(number_of_configurations), units = "cm")

    if (data_has_solar) {
        max_energy <- dbGetQuery(
            con,
            get_max_query(data_name, battery_name, strat_name, timestep_name, config_type = y_name, filter = filter, value = "maximum_missed + maximum_in")
        )[1,1] - min_energy
        otherplot <- ggplot(
            res,
            aes(
                x = energy_kwh_in + generationMissed_kwh - min_incoming,
                y = reorder(paste(pad_left(.data[[y_name]], data = "[0.7, 1.0] (0.04)"), "x", format_percent(count / this_number_of_combinations)), -ordering)
            )) +
            geom_boxplot() +
            geom_point(aes(x = mean(energy_kwh_in + generationMissed_kwh - min_incoming))) +
            expand_limits(x = c(0, expand_right(max_energy))) +
            scale_x_continuous(expand = c(0, 0)) +
            # scale_x_continuous(limits = c(0, max_energy)) +
            labs(
                # title = paste(
                #     paste("Wasted energy for each", strat_name, "configuration"),
                #     "Ordered by descending number of successful control passes",
                #     filter_name,
                #     sep = "\n"),
                x = "Total wasted energy [kWh]",
                y = "Configuration x Successful share") +
            theme(
                # plot.title = element_text(hjust = 0.5),
                # plot.title.position = "plot",
                axis.text.y = element_text(family = "Lucida Console", face = "bold")
            )

        ggsave(str_replace(file_name, "\\.", "_wasted."), otherplot, width = 25, height = get_plot_height(number_of_configurations), units = "cm")
    }
}

do_plot <- function(data_name, battery_name, strat_name, timestep_name, title, file_name) {
    do_plot_int(data_name, battery_name, strat_name, timestep_name, "configuration", title, file_name)
    do_plot_int(data_name, battery_name, strat_name, timestep_name, "guardConfiguration", title, str_replace(file_name, "\\.", "_guardConfig."))

}

# do_plot2 <- function(data_name, strat_name, title, file_name) {
#     res <- dbGetQuery(con, get_query(data_name, strat_name))
#     max_energy <- dbGetQuery(con, get_max_query(data_name, strat_name))
#     number_of_configurations <- get_number_of_configurations(strat_name)

#     thisplot <- ggplot(res, aes(
#         x = energy_kwh_in,
#         y = reorder(paste(configuration, "x", count), count)
#     )) +
#     geom_boxplot() +
#     scale_x_continuous(limits = c(0, max_energy[1, 1])) +
#     labs(
#         title = "total energy transferred",
#         x = "Total energy exchanged [kWh]",
#         y = "Configuration x Successful passes")

#     ggsave(file_name, thisplot, width = 25, height = get_plot_height(number_of_combinations), units = "cm")
# }

# do_plot3 <- function(strat_name, title, file_name) {
#     res <- dbGetQuery(con, get_query(strat_name = strat_name))
#     max_energy <- dbGetQuery(con, get_max_query(strat_name = strat_name))[1, 1]
#     ncomb <- number_of_combinations * length(data_configurations)
#     number_of_configurations <- get_number_of_configurations(strat_name)
#
#     thisplot <- ggplot(res, aes(
#         x = energy_kwh_in,
#         y = reorder(paste(configuration, "x", format_percent(count / ncomb)), -ordering)
#     )) +
#     geom_boxplot() +
#     scale_x_continuous(
#         limits = c(0, max_energy)) +
#     labs(
#         title = "Required energy for each configuration with descending number of successful control passes",
#         x = "Total energy received [kWh]",
#         y = "Configuration x successful share")
#
#     ggsave(file_name, thisplot, width = 25, height = get_plot_height(number_of_configurations), units = "cm")
# }

exists_successful_ <- function(where) {
    query <- paste("select count(*) as num from simulation", where)
    res <- dbGetQuery(con, query)
    number <- res[1, 1]
    exists <- number != 0
    exists
}

exists_successful <- function(data_name = "", battery_name = "", strat_name = "", timestep_name = "") {
    where <- get_where(
        success = "True",
        data = data_name,
        battery = battery_name,
        strategy = strat_name,
        timeStep = timestep_name)
    exists_successful_(where)
}

for (strat_name in strategy_names) {
    if (options$strategy != "" && options$strategy != strat_name) {
        next
    }
    # if (exists_successful(strat_name = strat_name)) {
    #     do_plot3(
    #         strat_name,
    #         paste(
    #             "simulating",
    #             strat_name),
    #         paste0(
    #             "plots/boxplot_",
    #             normalize(strat_name),
    #             ".pdf"))
    # }
    for (data_name in data_configurations) {
        # battery_name <- "10 kWh [10 kW]"
        for (battery_name in battery_configurations) {
            for (timestep_name in time_steps) {
                if (exists_successful(data_name, battery_name, strat_name, timestep_name)) {
                    do_plot(
                        data_name,
                        battery_name,
                        strat_name,
                        timestep_name,
                        paste(
                            "simulating",
                            data_name,
                            "with",
                            strat_name,
                            ".",
                            timestep_name),
                        paste0(
                            "plots/boxplot_",
                            normalize(data_name),
                            "_",
                            normalize(battery_name),
                            "_",
                            normalize(strat_name),
                            "-",
                            normalize(timestep_name),
                            ".pdf"))
                }
            }
        }
    }
}