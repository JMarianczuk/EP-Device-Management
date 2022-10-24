library(RSQLite)
library(ggplot2)
library(optparse)

option_list <- list(
    make_option(c("--legend"), type = "logical", default = FALSE)
)
opt_parser <- OptionParser(option_list = option_list)
options <- parse_args(opt_parser)

source("r_helpers/sql_helpers.r")
source("preprocessing/energy_counts.r")
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
power_in_name <- "avg no. of power guards for (incoming)"
power_out_name <- "avg no. of power guards for (outgoing)"
capacity_empty_name <- "avg no. of capacity guards (outgoing)"
capacity_full_name <- "avg no. of capacity guards (incoming)"
oscillation_name <- "avg no. of oscillation guards"

get_query <- function(by) {
    # other_query <- function(field, alias) {
    #     paste(
    #         "SELECT",
    #         paste("*, avg(", field, ") as ", alias, sep = ""),
    #         "FROM",
    #         "simulation",
    #         where,
    #         "group by",
    #         paste(by, collapse = ","))
    # }

    # other_outer_query <- function(inner_query, alias, name) {
    #     paste(
    #         "select",
    #         paste(by, collapse = ","),
    #         ", 0 as count, 0 as avg_energy_in,",
    #         paste(alias, "as value,"),
    #         paste('"', name, '" as facet', sep = ""),
    #         "from",
    #         "(",
    #         inner_query,
    #         ")")
    # }
    # where <- paste("where", 'success="True"')#, "and", 'battery="10 kWh [10 kWh]"')
    energy_count_table <- paste("energy_count_", paste(by, collapse = "_"), sep = "")
    # power_in_count_query <- other_query("incomingPowerGuards", "avg_power")
    # power_out_count_query <- other_query("outgoingPowerGuards", "avg_power")
    # capacity_empty_count_query <- other_query("emptyCapacityGuards", "avg_capacity")
    # capacity_full_count_query <- other_query("fullCapacityGuards", "avg_capacity")
    # oscillation_count_query <- other_query("oscillationGuards", "avg_oscillation")
    query <- paste(
        "select",
        "*",
        "from",
        energy_count_table
    )
    outer_query <- function(alias, name) {
        paste(
            "select",
            paste(by, collapse = ","),
            ",",
            paste(alias, "as value,"),
            paste('"', name, '" as facet', sep = ""),
            "from",
            "(",
            query,
            ")")
    }
    total_count_table <- paste("total_count_", paste(by, collapse = "_"), sep = "")
    total_query <- paste(
        "select",
        "*",
        "from",
        total_count_table
    )
    count_query <- paste(
        "select",
        paste("succ.", by, sep = "", collapse = ","),
        ",",
        "cast(succ.count as REAL) / total.totalCount as value,",
        paste('"', count_name, '" as facet', sep = ""),
        "from",
        paste(
            "(",
                "(",
                query,
                ") succ,",
                "(",
                total_query,
                ") total",
            ")"),
        "where",
        paste("succ.", by, "=", "total.", by, sep = "", collapse = " and "))
    query <- paste(
        outer_query("avg_energy_in", energy_name),
        count_query,
        outer_query("avg_powerGuard_in", power_in_name),
        outer_query("avg_powerGuard_out", power_out_name),
        outer_query("avg_capacityGuard_empty", capacity_empty_name),
        outer_query("avg_capacityGuard_full", capacity_full_name),
        outer_query("avg_oscillationGuard", oscillation_name),
        sep = " UNION "
    )
    query
}

do_plot <- function(by, aesthetics, x_title, filename, highlight_x = c()) {
    res <- dbGetQuery(con, get_query(by = by))
    res$facet <- factor(
        res$facet,
        levels = c(
            count_name,
            energy_name,
            power_in_name,
            power_out_name,
            capacity_full_name,
            capacity_empty_name,
            oscillation_name))

    thisplot <- ggplot(res, aesthetics) +
        geom_line() +
        # geom_point() +
        facet_grid(rows = vars(facet), scales = "free_y", switch = "both") +
        labs(x = x_title) +
        theme(axis.title.y = element_blank())

    if (length(highlight_x) != 0) {
        thisplot <- thisplot + geom_vline(xintercept = highlight_x, linetype = "dashed")
    }

    ggsave(filename, thisplot, width = 30, height = 40, units = "cm")
}

plot_packetSize <- function(group) {
    preprocess_energy_counts("packetSize", group, con)
    preprocess_total_counts("packetSize", group, con)
    do_plot(
        c("packetSize", group),
        aes(
            x = packetSize,
            y = value,
            color = .data[[group]],
            # shape = .data[[group]],
            group = .data[[group]]),
        "packet size",
        paste("single_factor_packetSize_", group, ".pdf", sep = ""),
        c(2.5, 3.75))
}

plot_probability <- function(group) {
    preprocess_energy_counts("probability", group, con)
    preprocess_total_counts("probability", group, con)
    do_plot(
        c("probability", group),
        aes(
            x = probability,
            y = value,
            color = .data[[group]],
            # shape = .data[[group]],
            group = .data[[group]]),
        "probability",
        paste("single_factor_probability_", group, ".pdf", sep = ""))
}

for (g in c(
    "strategy",
    "data",
    # "configuration",
    "battery")) {
    plot_packetSize(g)
    plot_probability(g)
}

plot_packetSize("probability")
plot_probability("packetSize")