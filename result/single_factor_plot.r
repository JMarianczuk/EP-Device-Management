library(RSQLite)
library(ggplot2)
library(optparse)

option_list <- list(
    make_option(c("--legend"), type = "logical", default = FALSE)
)
opt_parser <- OptionParser(option_list = option_list)
options <- parse_args(opt_parser)

source("r_helpers/sql_helpers.r")
source("r_helpers/string_helpers.r")
source("r_helpers/array_helpers.r")
source("r_helpers/ggplot_helpers.r")

con <- create_db_connection()

# y axis is energy_in
# x axis is some parameter like packet size, probability, configuration
# color / shape is data / strategy

#first plot: probability vs. energy_in by strategy
energy_name <- "average incoming energy [kWh]"
count_name <- "share of successful control passes"

get_query <- function(by) {
    where <- paste("where", 'success="True"')
    query <- paste(
        "select",
        "*, avg(energy_kwh_in) as avg_energy_in, count(*) as count",
        "from",
        "simulation",
        where,
        "group by",
        paste(by, collapse = ", ")
    )
    totalQuery <- paste(
        "select",
        "*, count(*) as totalCount",
        "from",
        "simulation",
        "group by",
        paste(by, collapse = ", ")
    )
    query <- paste(
        "select",
        "*,",
        "avg_energy_in as value,",
        paste('"', energy_name, '" as facet', sep = ""),
        "from",
        "(",
        query,
        ")",

        "UNION",

        "select",
        "succ.*,",
        "cast(succ.count as REAL) / total.totalCount as value,",
        paste('"', count_name, '" as facet', sep = ""),
        "from",
        paste(
            "(",
                "(",
                query,
                ") succ,",
                "(",
                totalQuery,
                ") total",
            ")"),
        "where",
        paste("succ.", by, "=", "total.", by, sep = "", collapse = " and ")
    )
    # print(query)
    query
}

do_plot <- function(by, aesthetics, x_title, filename) {
    res <- dbGetQuery(con, get_query(by = by))
    res$facet <- factor(res$facet, levels = c(count_name, energy_name))
    # print(res)
    thisplot <- ggplot(res, aesthetics) +
        geom_line() +
        geom_point() +
        facet_grid(rows = vars(facet), scales = "free_y", switch = "both") +
        labs(x = x_title) +
        theme(axis.title.y = element_blank())

    ggsave(filename, thisplot, width = 30, height = 12, units = "cm")
}

plot_packetSize <- function(group) {
    do_plot(
        c("packetSize", group),
        aes(
            x = packetSize,
            y = value,
            color = .data[[group]],
            shape = .data[[group]],
            group = .data[[group]]),
        "packet size",
        paste("single_factor_packetSize_", group, ".pdf", sep = ""))
}

plot_probability <- function(group) {
    do_plot(
        c("probability", group),
        aes(
            x = reorder(probability, probability_num_value(probability)),
            y = value,
            color = .data[[group]],
            shape = .data[[group]],
            group = .data[[group]]),
        "probability",
        paste("single_factor_probability_", group, ".pdf", sep = ""))
}

for (g in c("strategy", "data", "configuration", "battery")) {
    plot_packetSize(g)
    plot_probability(g)
}