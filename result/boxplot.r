library(RSQLite)
library(ggplot2)

con <- dbConnect(SQLite(), "results.sqlite")
some_res <- dbExecute(con, "PRAGMA case_sensitive_like=ON;")

data_configurations <- dbGetQuery(con, "select data from simulation group by data")
time_steps <- dbGetQuery(con, "select timeStep from simulation group by timeStep")
strategy_names <- dbGetQuery(con, "select strategy from simulation group by strategy")

get_query <- function(data_name, strat_name, timestep_name) {
    success_where <- 'success="True"'
    data_where <- paste('data="', data_name, '"', sep = "")
    strat_where <- paste('strategy="', strat_name, '"', sep = "")
    timestep_where <- paste('timestep="', timestep_name, '"', sep = "")
    where <- paste("where", success_where, "and", data_where, "and", strat_where, "and", timestep_where)
    query <- paste(
        "select",
        "*",
        "from simulation_with_counts",
        where)
    query
}

do_plot <- function(data_name, strat_name, timestep_name, title, file_name) {
    res <- dbGetQuery(con, get_query(data_name, strat_name, timestep_name))

    thisplot <- ggplot(res, aes(
        x = packets,
        y = reorder(paste(configuration, "x", count), count)
    )) +
    geom_boxplot() +
    labs(title = "Required packet transfers for each configuration with descending number of successful control passes over all packet sizes and probabilities", x = "Number of exchanged packets", y = "Configuration x Successful passes")

    ggsave(file_name, thisplot, width = 30, height = 25, units = "cm")
}

normalize <- function(text) {
    normalized <- gsub(":", "-", text)
    normalized
}

exists_successful <- function(data_name, strat_name, timestep_name) {
    success_where <- 'success="True"'
    data_where <- paste('data="', data_name, '"', sep = "")
    strat_where <- paste('strategy="', strat_name, '"', sep = "")
    timestep_where <- paste('timestep="', timestep_name, '"', sep = "")
    where <- paste("where", success_where, "and", data_where, "and", strat_where, "and", timestep_where)
    query <- paste("select count(*) as num from simulation", where)
    res <- dbGetQuery(con, query)
    number <- res[1, 1]
    exists <- number != 0
    exists
}

for (data in 1:nrow(data_configurations)) {
    for (strat in 1:nrow(strategy_names)) {
        for (timestep in 1:nrow(time_steps)) {
            data_name <- data_configurations[data, 1]
            strat_name <- strategy_names[strat, 1]
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
    }
}