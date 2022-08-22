library(ggplot2)
library(optparse)
library(RSQLite)

con <- dbConnect(SQLite(), "results.sqlite")
some_res <- dbExecute(con, "PRAGMA case_sensitive_like=ON;")

get_query <- function(data_name) {
    data_where <- paste('data="', data_name, '"', sep="")
    query <- paste(
        "select",
        "*, count(*) as size",
        "from simulation",
        'where success="True" and', data_where,
        "group by strategy, probability, packetSize")
    query
}

do_plot <- function(data_name, title, file_name) {
    res <- dbGetQuery(con, get_query(data_name))
    num_value <- function(text) {
        only_number <- substring(text, first = 1, last = nchar(text) - 2)
        numeric <- as.numeric(only_number)
        numeric
    }

    thisplot <- ggplot(res, aes(
        x = reorder(probability, num_value(probability)),
        y = packetSize,
        colour = strategy,
        size = size)
        ) +
        geom_jitter() +
        labs(x = "probability", title = title, size = "number of successful control passes")

    ggsave(file_name, thisplot, width=25, height=10, units="cm")
}

do_plot("Res1", "simulating residential 1", "plot_res1.pdf")
do_plot("Res1 noSolar", "simulating residential 1 without solar", "plot_res1_noSolar.pdf")
