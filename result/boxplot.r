library(RSQLite)
library(ggplot2)

source("r_helpers/sql_helpers.r")
source("r_helpers/string_helpers.r")
source("r_helpers/array_helpers.r")

con <- create_db_connection()

data_configurations <- dbGetQuery(con, "select data from simulation group by data")
time_steps <- dbGetQuery(con, "select timeStep from simulation group by timeStep")
strategy_names <- dbGetQuery(con, "select strategy from simulation group by strategy")

number_of_packet_sizes <- dbGetQuery(con, "select count(distinct(packetSize)) from simulation")[1, 1]
number_of_probabilities <- dbGetQuery(con, "select count(distinct(probability)) from simulation")[1, 1]
number_of_batteries <- dbGetQuery(con, "select count(distinct(battery)) from simulation")[1, 1]
number_of_combinations <- number_of_batteries * number_of_packet_sizes * number_of_probabilities

get_query_ <- function(where, table) {
    query <- paste(
        "select",
        "*",
        "from",
        table,
        where)
    query
}

get_query <- function(data_name, strat_name, timestep_name) {
    where <- get_where(data_name, strat_name, timestep_name)
    get_query_(where, "simulation_with_counts_scdt")
}

get_query2 <- function(data_name, strat_name) {
    where <- get_where(data_name, strat_name)
    get_query_(where, "simulation_with_counts_scd")
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

get_max_query <- function(data_name, strat_name, timestep_name) {
    where <- get_where(data_name, strat_name, timestep_name)
    get_max_query_(where, "energy_scdt_stat")
}

get_min_query <- function(data_name, timestep_name) {
    where <- paste('where data ="', data_name, '"', sep = "")
    get_min_query_(where, "data_stat")
}

get_max_query2 <- function(data_name, strat_name) {
    where <- get_where2(data_name, strat_name)
    get_max_query_(where, "energy_scd_stat")
}

do_plot <- function(data_name, strat_name, timestep_name, title, file_name) {
    res <- dbGetQuery(con, get_query(data_name, strat_name, timestep_name))
    max_energy <- dbGetQuery(con, get_max_query(data_name, strat_name, timestep_name))[1, 1]
    min_energy <- dbGetQuery(con, get_min_query(data_name, timestep_name))[1, 1]

    thisplot <- ggplot(res, aes(
        x = energy_kwh_in,
        y = reorder(paste(configuration, "x", count, "/", number_of_combinations), count)
    )) +
    geom_boxplot() +
    scale_x_continuous(limits = c(0, max_energy),
        sec.axis = sec_axis(~., breaks = c(round(min_energy, digits = 1)))) +
    labs(
        title = "Required energy for each configuration with descending number of successful control passes",
        x = "Total energy received [kWh]",
        y = "Configuration x Successful passes / total passes") +
    geom_vline(xintercept = min_energy, color = "blue", linetype = "dashed")
    # geom_vline(xintercept = min_energy[1, 1] + min_energy[1, 2], color = "blue", linetype = "dashed")

    ggsave(file_name, thisplot, width = 25, height = 20, units = "cm")
}

do_plot2 <- function(data_name, strat_name, title, file_name) {
    res <- dbGetQuery(con, get_query2(data_name, strat_name))
    max_energy <- dbGetQuery(con, get_max_query2(data_name, strat_name))

    thisplot <- ggplot(res, aes(
        x = energy_kwh_in,
        y = reorder(paste(configuration, "x", count), count)
    )) +
    geom_boxplot() +
    scale_x_continuous(limits = c(0, max_energy[1, 1])) +
    labs(
        title = "total energy transferred",
        x = "Total energy exchanged [kWh]",
        y = "Configuration x Successful passes")

    ggsave(file_name, thisplot, width = 25, height = 20, units = "cm")
}

exists_successful_ <- function(where) {
    query <- paste("select count(*) as num from simulation", where)
    res <- dbGetQuery(con, query)
    number <- res[1, 1]
    exists <- number != 0
    exists
}

exists_successful <- function(data_name, strat_name, timestep_name) {
    where <- get_where(data_name, strat_name, timestep_name)
    exists_successful_(where)
}

exists_successful2 <- function(data_name, strat_name) {
    where <- get_where(data_name, strat_name)
    exists_successful_(where)
}

for (data in 1:nrow(data_configurations)) {
    data_name <- data_configurations[data, 1]
    for (strat in 1:nrow(strategy_names)) {
        strat_name <- strategy_names[strat, 1]
        for (timestep in 1:nrow(time_steps)) {
            timestep_name <- time_steps[timestep, 1]
            if (exists_successful(data_name, strat_name, timestep_name)) {
                do_plot(
                    data_name,
                    strat_name,
                    timestep_name,
                    paste(
                        "simulating",
                        data_name,
                        "with",
                        strat_name,
                        ".",
                        timestep_name),
                    paste(
                        "boxplot_",
                        normalize(data_name),
                        "_",
                        normalize(strat_name),
                        "-",
                        normalize(timestep_name),
                        ".pdf",
                        sep = ""))
            }
        }
        # if (exists_successful2(data_name, strat_name)) {
        #     do_plot2(
        #         data_name,
        #         strat_name,
        #         paste(
        #             "simulating",
        #             data_name,
        #             "with",
        #             strat_name),
        #         paste(
        #             "boxplot_",
        #             normalize(data_name),
        #             "-",
        #             normalize(strat_name),
        #             ".pdf",
        #             sep = ""))
        # }
    }
}