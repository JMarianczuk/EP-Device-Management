library(RSQLite)
library(ggplot2)
library(optparse)

option_list <- list(
    make_option("--strategy", type = "character", default = "")
)
opt_parser <- OptionParser(option_list = option_list)
options <- parse_args(opt_parser)

source("r_helpers/sql_helpers.r")
source("r_helpers/string_helpers.r")
source("r_helpers/array_helpers.r")

source("preprocessing/successful_counts.r")
source("preprocessing/distinct_values.r")
source("preprocessing/energy_stat.r")

con <- create_db_connection()

preprocess_successful_counts(c("strategy", "configuration"), "sc", con)
preprocess_successful_counts(c("strategy", "configuration", "data"), "scd", con)
preprocess_successful_counts(c("strategy", "configuration", "data", "battery"), "scdb", con)
preprocess_successful_counts(c("strategy", "configuration", "data", "timeStep"), "scdt", con)
preprocess_successful_counts(c("strategy", "configuration", "data", "battery", "timeStep"), "scdbt", con)

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

get_short <- function(strat_name = "", data_name = "", battery_name = "", timestep_name = "") {
    res <- ""
    if (strat_name != "") {
        res <- paste0(res, "sc")
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

get_query <- function(data_name = "", battery_name = "", strat_name = "", timestep_name = "") {
    where <- get_where(
        success = "True",
        data = data_name,
        battery = battery_name,
        strategy = strat_name,
        timeStep = timestep_name,
        topTen = 1)
    table <- paste0("simulation_with_counts_", get_short(strat_name, data_name, battery_name, timestep_name))
    get_query_(where, table)
}

get_max_query_ <- function(where, table) {
    query <- paste(
        "select",
        "max(maximum_in)", #the stat table only contains max for each configuration, get the max of those here
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

get_max_query <- function(data_name = "", battery_name = "", strat_name = "", timestep_name = "") {
    where <- get_where(
        success = "True",
        strategy = strat_name,
        battery = battery_name,
        timeStep = timestep_name)
    if (data_name != "") {
        where <- append_where(
            where,
            paste(
                "(",
                paste0('data="', data_name, '"'),
                "OR",
                paste0('data="', with_without_switch(data_name), '"'),
                ")"))
    }
    succ_count_table_name <- paste0(
        "successful_counts_",
        get_short(strat_name, data_name, battery_name, timestep_name))
    inner_where <- get_where(
        success = "True",
        strategy = strat_name,
        data = data_name,
        battery = battery_name,
        timeStep = timestep_name,
        topTen = 1)
    where <- append_where(
        where,
        paste("configuration in (select configuration from", succ_count_table_name, inner_where, ")"))
    table <- paste0("energy_", get_short(strat_name, data_name, battery_name, timestep_name), "_stat")
    get_max_query_(where, table)
}

get_min_query <- function(data_name, battery_name, timestep_name) {
    where <- get_where(
        data = data_name,
        battery = battery_name)
    get_min_query_(where, "data_stat")
}

get_min_query2 <- function() {
    get_min_query_("", "data_stat")
}

get_number_of_configurations <- function(strat_name, data_name = "", battery_name = "", timestep_name = "") {
    where <- get_where(
        strategy = strat_name,
        data = data_name,
        battery = battery_name,
        timeStep = timestep_name,
        topTen = 1
    )
    table_name <- paste0("successful_counts_", get_short(strat_name, data_name, battery_name, timestep_name))
    query <- paste(
        "SELECT",
        "count(*)",
        "FROM",
        table_name,
        where
    )
    res <- dbGetQuery(con, query)[1, 1]
    res
}

get_plot_height <- function(number_of_configurations) {
    plot_height <- 10 + number_of_configurations / 4
    plot_height
}

do_plot <- function(data_name, battery_name, strat_name, timestep_name, title, file_name) {
    res <- dbGetQuery(con, get_query(data_name, battery_name, strat_name, timestep_name))
    max_energy <- dbGetQuery(con, get_max_query(data_name, battery_name, strat_name, timestep_name))[1, 1]
    min_energy <- dbGetQuery(con, get_min_query(data_name, battery_name, timestep_name))[1, 1]
    number_of_configurations <- get_number_of_configurations(
        strat_name,
        data_name = data_name,
        battery_name = battery_name,
        timestep_name = timestep_name)

    thisplot <- ggplot(res, aes(
        x = energy_kwh_in,
        y = reorder(paste(configuration, "x", format_percent(count / number_of_combinations)), -ordering)
    )) +
    geom_boxplot() +
    scale_x_continuous(
        limits = c(0, max_energy),
        sec.axis = sec_axis(~., breaks = c(round(min_energy, digits = 1)))) +
    labs(
        title = "Required energy for each configuration with descending number of successful control passes",
        x = "Total energy received [kWh]",
        y = "Configuration x Successful share") +
    geom_vline(xintercept = min_energy, color = "blue", linetype = "dashed")


    ggsave(file_name, thisplot, width = 25, height = get_plot_height(number_of_configurations), units = "cm")
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

do_plot3 <- function(strat_name, title, file_name) {
    res <- dbGetQuery(con, get_query(strat_name = strat_name))
    max_energy <- dbGetQuery(con, get_max_query(strat_name = strat_name))[1, 1]
    ncomb <- number_of_combinations * length(data_configurations)
    number_of_configurations <- get_number_of_configurations(strat_name)

    thisplot <- ggplot(res, aes(
        x = energy_kwh_in,
        y = reorder(paste(configuration, "x", format_percent(count / ncomb)), -ordering)
    )) +
    geom_boxplot() +
    scale_x_continuous(
        limits = c(0, max_energy)) +
    labs(
        title = "Required energy for each configuration with descending number of successful control passes",
        x = "Total energy received [kWh]",
        y = "Configuration x successful share")

    ggsave(file_name, thisplot, width = 25, height = get_plot_height(number_of_configurations), units = "cm")
}

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
    if (exists_successful(strat_name = strat_name)) {
        do_plot3(
            strat_name,
            paste(
                "simulating",
                strat_name),
            paste0(
                "plots/boxplot_",
                normalize(strat_name),
                ".pdf"))
    }
    for (data_name in data_configurations) {
        battery_name <- "10 kWh [10 kW]"
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