library(RSQLite)
library(ggplot2)

source("r_helpers/sql_helpers.r")
source("r_helpers/string_helpers.r")
source("r_helpers/array_helpers.r")

con <- create_db_connection()

data_configurations <- dbGetQuery(con, "select data from simulation group by data")[,1]
time_steps <- dbGetQuery(con, "select timeStep from simulation group by timeStep")[,1]
strategy_names <- dbGetQuery(con, "select strategy from simulation group by strategy")[,1]

number_of_packet_sizes <- dbGetQuery(con, "select count(distinct(packetSize)) from simulation")[1, 1]
number_of_probabilities <- dbGetQuery(con, "select count(distinct(probability)) from simulation")[1, 1]
number_of_batteries <- dbGetQuery(con, "select count(distinct(battery)) from simulation")[1, 1]
number_of_combinations <- number_of_batteries * number_of_packet_sizes * number_of_probabilities


exists_successful <- function(strat_name) {
    where <- get_where(
        success = "True",
        strategy = strat_name)
    query <- paste("select count(*) as num from simulation", where)
    res <- dbGetQuery(con, query)
    number <- res[1, 1]
    exists <- number != 0
    exists
}

get_query <- function(strat_name) {
    where <- get_where(
        success = "True",
        strategy = strat_name)
    query <- paste(
        "select",
        "*,",
        "cast(substr(configuration, 2, 3) as NUMERIC) as lower,",
        "cast(substr(configuration, 8, 3) as NUMERIC) as upper",
        "from",
        "successful_counts_sc",
        where)
    query
}

do_plot <- function(strat_name, title, file_name) {
    res <- dbGetQuery(con, get_query(strat_name))

    thisplot <- ggplot(res, aes(
        x = upper,
        y = lower,
        size = count,
        # colour = data,
        # shape = data)
        colour = count)
        ) +
        # geom_jitter() +
        geom_point() +
        scale_x_continuous(breaks = c(.1, .2, .3, .4, .5, .6, .7, .8, .9)) +
        scale_y_continuous(breaks = c(.1, .2, .3, .4, .5, .6, .7, .8, .9))
        labs(x = "upper", y = "lower")

    ggsave(file_name, thisplot, width = 25, height = 10, units = "cm")
}

for (strat_name in strategy_names) {
    if (exists_successful(strat_name)) {
        do_plot(
            strat_name,
            paste(
                "simulating",
                strat_name),
            paste(
                "plots/configplot_",
                normalize(strat_name),
                ".pdf"))
    }
}