library(ggplot2)
library(optparse)
library(RSQLite)
library(stringr)

source("r_helpers/sql_helpers.r")
source("r_helpers/string_helpers.r")
source("r_helpers/array_helpers.r")
source("r_helpers/colour_helpers.r")

source("preprocessing/successful_counts.r")

con <- create_db_connection()

preprocess_successful_counts(
    by = c("packetSize", "probability", "battery", "data", "timestep"),
    "ppbdt",
    con)
preprocess_successful_counts(
    by = c("strategy", "packetSize", "probability", "battery", "data", "timestep"),
    "sppbdt",
    con)

data_configurations <- dbGetQuery(con, "select data from simulation group by data")[,1]
batteries <- dbGetQuery(con, "select battery from simulation group by battery")[,1]
time_steps <- dbGetQuery(con, "select timeStep from simulation group by timeStep")[,1]

get_query <- function(data_name, battery_name, timestep_name) {
    where <- get_where(
        success = "True",
        data = data_name,
        battery = battery_name,
        timeStep = timestep_name
    )
    query <- paste(
        "select",
        "*",
        "from successful_counts_sppbdt",
        where)
    query
}

get_query2 <- function(data_name, battery_name, timestep_name) {
    where <- get_where(
        success = "True",
        data = data_name,
        battery = battery_name,
        timeStep = timestep_name
    )
    query <- paste(
        "select",
        "*",
        "from successful_counts_ppbdt",
        where)
    query
}

do_plot <- function(data_name, battery_name, timestep_name, title, file_name) {
    res <- dbGetQuery(con, get_query(data_name, battery_name, timestep_name))
    group_values_colours <- get_group_colours("strategy", res, con)

    thisplot <- ggplot(res, aes(
        x = probability,
        y = packetSize,
        colour = strategy,
        shape = strategy,
        size = count)
        ) +
        geom_jitter() +
        # geom_point() +
        labs(x = "probability", title = title, size = "number of successful control passes") +
        scale_colour_manual("strategy", values = group_values_colours)

    ggsave(file_name, thisplot, width = 25, height = 10, units = "cm")
}

normalize <- function(text) {
    normalized <- gsub(":", "-", text)
    normalized
}

for (data_name in data_configurations) {
    for (battery_name in batteries) {
        for (timestep_name in time_steps) {
            do_plot(
                data_name,
                battery_name,
                timestep_name,
                paste(
                    "simulating",
                    data_name,
                    "with",
                    battery_name,
                    ".",
                    timestep_name),
                paste0(
                    "plots/plot_",
                    normalize(data_name),
                    "_",
                    normalize(battery_name),
                    "_",
                    normalize(timestep_name),
                    ".pdf"))
        }
    }
}
