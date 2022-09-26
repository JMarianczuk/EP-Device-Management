library(RSQLite)
library(ggplot2)

con <- dbConnect(SQLite(), "results.sqlite")
some_res <- dbExecute(con, "PRAGMA case_sensitive_like=ON;")

data_configurations <- dbGetQuery(con, "select data from simulation group by data")
time_steps <- dbGetQuery(con, "select timeStep from simulation group by timeStep")
strategy_names <- dbGetQuery(con, "select strategy from simulation group by strategy")

get_where <- function(data_name, strat_name, timestep_name) {
    success_where <- 'success="True"'
    data_where <- paste('data="', data_name, '"', sep = "")
    strat_where <- paste('strategy="', strat_name, '"', sep = "")
    timestep_where <- paste('timeStep="', timestep_name, '"', sep = "")
    where <- paste("where", success_where, "and", data_where, "and", strat_where, "and", timestep_where)
    where
}

get_where2 <- function(data_name, strat_name) {
    success_where <- 'success="True"'
    data_where <- paste('data="', data_name, '"', sep = "")
    strat_where <- paste('strategy="', strat_name, '"', sep = "")
    where <- paste("where", success_where, "and", data_where, "and", strat_where)
    where
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

get_query <- function(data_name, strat_name, timestep_name) {
    where <- get_where(data_name, strat_name, timestep_name)
    get_query_(where, "simulation_with_counts_scdt")
}

get_query2 <- function(data_name, strat_name) {
    where <- get_where2(data_name, strat_name)
    get_query_(where, "simulation_with_counts_scd")
}

get_max_query_ <- function(where, table) {
    query <- paste(
        "select",
        "max(maximum)", #the stat table only contains max for each configuration, get the max of those here
        "from",
        table,
        where)
    query
}

get_max_query <- function(data_name, strat_name, timestep_name) {
    where <- get_where(data_name, strat_name, timestep_name)
    get_max_query_(where, "energy_scdt_stat")
}

get_max_query2 <- function(data_name, strat_name) {
    where <- get_where2(data_name, strat_name)
    get_max_query_(where, "energy_scd_stat")
}

do_plot <- function(data_name, strat_name, timestep_name, title, file_name) {
    res <- dbGetQuery(con, get_query(data_name, strat_name, timestep_name))
    max_energy <- dbGetQuery(con, get_max_query(data_name, strat_name, timestep_name))

    thisplot <- ggplot(res, aes(
        x = energy_kwh,
        y = reorder(paste(configuration, "x", count), count)
    )) +
    geom_boxplot() +
    scale_x_continuous(limits = c(0, max_energy[1, 1])) +
    labs(
        title = "Required energy for each configuration with descending number of successful control passes",
        x = "Total energy exchanged [kWh]",
        y = "Configuration x Successful passes")

    ggsave(file_name, thisplot, width = 25, height = 20, units = "cm")
}

do_plot2 <- function(data_name, strat_name, title, file_name) {
    res <- dbGetQuery(con, get_query2(data_name, strat_name))
    max_energy <- dbGetQuery(con, get_max_query2(data_name, strat_name))

    thisplot <- ggplot(res, aes(
        x = energy_kwh,
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

normalize <- function(text) {
    normalized <- gsub(":", "-", text)
    normalized
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
    where <- get_where2(data_name, strat_name)
    exists_successful_(where)
}

for (strat in 1:nrow(strategy_names)) {
    strat_name <- strategy_names[strat, 1]
    for (data in 1:nrow(data_configurations)) {
        data_name <- data_configurations[data, 1]
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
        if (exists_successful2(data_name, strat_name)) {
            do_plot2(
                data_name,
                strat_name,
                paste(
                    "simulating",
                    data_name,
                    "with",
                    strat_name),
                paste(
                    "boxplot_",
                    normalize(data_name),
                    "-",
                    normalize(strat_name),
                    ".pdf",
                    sep = ""))
        }
    }
}