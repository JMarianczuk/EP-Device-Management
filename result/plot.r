library(ggplot2)
library(optparse)
library(RSQLite)
library(stringr)

con <- dbConnect(SQLite(), "results.sqlite")
some_res <- dbExecute(con, "PRAGMA case_sensitive_like=ON;")

data_configurations <- dbGetQuery(con, "select data from simulation group by data")
batteries <- dbGetQuery(con, "select battery from simulation group by battery")
time_steps <- dbGetQuery(con, "select timeStep from simulation group by timeStep")

get_query <- function(data_name, battery_name, timestep_name) {
    success_where <- 'success="True"'
    data_where <- paste('data="', data_name, '"', sep = "")
    battery_where <- paste('battery="', battery_name, '"', sep = "")
    timestep_where <- paste('timeStep="', timestep_name, '"', sep = "")
    where <- paste("where", success_where, "and", data_where, "and", battery_where, "and", timestep_where)
    query <- paste(
        "select",
        "*, count(*) as size",
        "from simulation",
        where,
        "group by strategy, probability, packetSize")
    query
}

do_plot <- function(data_name, battery_name, timestep_name, title, file_name) {
    res <- dbGetQuery(con, get_query(data_name, battery_name, timestep_name))
    num_value <- function(text) {
        only_number <- substring(text, first = 1, last = nchar(text) - 2)
        numeric <- as.numeric(only_number)
        numeric
    }

    thisplot <- ggplot(res, aes(
        x = reorder(probability, num_value(probability)),
        y = packetSize,
        colour = strategy,
        shape = strategy,
        size = size)
        ) +
        geom_jitter() +
        labs(x = "probability", title = title, size = "number of successful control passes")

    ggsave(file_name, thisplot, width = 25, height = 10, units = "cm")
}

normalize <- function(text) {
    normalized <- gsub(":", "-", text)
    normalized
}

for (data in 1:nrow(data_configurations)) {
    for (battery in 1:nrow(batteries)) {
        for (timestep in 1:nrow(time_steps)) {
            data_name <- data_configurations[data, 1]
            battery_name <- batteries[battery, 1]
            timestep_name <- time_steps[timestep, 1]

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
                paste(
                    "plot_",
                    normalize(data_name),
                    "_",
                    normalize(battery_name),
                    "_",
                    normalize(timestep_name),
                    ".pdf",
                    sep = ""))
        }
    }
}
