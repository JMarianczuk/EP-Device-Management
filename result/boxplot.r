library(RSQLite)
library(ggplot2)
library(optparse)
library(stringr)
library(showtext)

option_list <- list(
    make_option("--strategy", type = "character", default = "")
)
opt_parser <- OptionParser(option_list = option_list)
options <- parse_args(opt_parser)

source("r_helpers/sql_helpers.r")
source("r_helpers/string_helpers.r")
source("r_helpers/array_helpers.r")
source("r_helpers/font_helpers.r")

source("preprocessing/successful_counts.r")
source("preprocessing/distinct_values.r")
source("preprocessing/energy_stat.r")

load_fonts()
showtext_auto()

con <- create_db_connection()

cat("preprocessing successful counts ")
preprocess_successful_counts(c("strategy", "data"), "sd", con, filter = "where guardConfiguration in ('p(0.0, 9.0) c(0.7, 0.5)', 'p(0.0) c(0.5)')")
cat("sd")
preprocess_successful_counts(c("strategy", "data", "timeStep"), "sdt", con, filter = "where guardConfiguration in ('p(0.0, 9.0) c(0.7, 0.5)', 'p(0.0) c(0.5)')")
cat("-sdt")
preprocess_successful_counts(c("strategy", "battery"), "sb", con, filter = "where guardConfiguration in ('p(0.0, 9.0) c(0.7, 0.5)', 'p(0.0) c(0.5)')")
cat("-sb")
preprocess_successful_counts(c("strategy", "battery", "timeStep"), "sbt", con, filter = "where guardConfiguration in ('p(0.0, 9.0) c(0.7, 0.5)', 'p(0.0) c(0.5)')")
cat("-sbt")
preprocess_successful_counts(c("strategy", "data", "battery"), "sdb", con, filter = "where guardConfiguration in ('p(0.0, 9.0) c(0.7, 0.5)', 'p(0.0) c(0.5)')")
cat("-sdb")
preprocess_successful_counts(c("strategy", "data", "battery", "timeStep"), "sdbt", con, filter = "where guardConfiguration in ('p(0.0, 9.0) c(0.7, 0.5)', 'p(0.0) c(0.5)')")
cat("-sdbt")

preprocess_successful_counts(c("strategy", "configuration", "data", "battery", "timeStep"), "scdbtr", con, filter = "where guardConfiguration in ('p(0.0, 9.0) c(0.7, 0.5)', 'p(0.0) c(0.5)')")
cat("-scdbtr")

preprocess_successful_counts(c("strategy", "configuration"), "sc", con)
cat("-sc")
preprocess_successful_counts(c("strategy", "configuration", "data"), "scd", con)
cat("-scd")
preprocess_successful_counts(c("strategy", "configuration", "data", "battery"), "scdb", con)
cat("-scdb")
preprocess_successful_counts(c("strategy", "guardConfiguration", "data", "battery"), "sgdb", con)
cat("-sgdb")
preprocess_successful_counts(c("strategy", "configuration", "data", "timeStep"), "scdt", con)
cat("-scdt")
preprocess_successful_counts(c("strategy", "configuration", "data", "battery", "timeStep"), "scdbt", con)
cat("-scdbt")
preprocess_successful_counts(c("strategy", "guardConfiguration", "data", "battery", "timeStep"), "sgdbt", con)
cat("-sgdbt")
preprocess_successful_counts(c("configuration", "battery", "timeStep"), "cbt", con, filter = "where strategy not like '%NoSend'")
cat("-cbt")
preprocess_successful_counts(c("configuration", "data", "battery", "timeStep"), "cdbt", con, filter = "where strategy not like '%NoSend'")
cat("-cdbt")
preprocess_successful_counts(c("guardConfiguration", "battery", "timeStep"), "gbt", con, filter = "where strategy not like '%NoSend'")
cat("-gbt")
preprocess_successful_counts(c("guardConfiguration", "data", "battery", "timeStep"), "gdbt", con, filter = "where strategy not like '%NoSend'")
cat("-gdbt")
cat("\n")

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

get_short <- function(strat_name = "", data_name = "", battery_name = "", timestep_name = "", config_type = "configuration") {
    res <- ""
    if (strat_name != "") {
        res <- paste0(res, "s")
    }
    if (config_type == "configuration") {
        res <- paste0(res, "c")
    } else if (config_type == "guardConfiguration") {
        res <- paste0(res, "g")
    } else if (config_type == "configuration+reduced") {
        res <- paste0(res, "c")
    } else if (config_type == "strategy") {
        res <- paste0(res, "s")
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
    if (config_type == "configuration+reduced") {
        res <- paste0(res, "r")
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

get_query <- function(data_name = "", battery_name = "", strat_name = "", timestep_name = "", config_type = "configuration", filter = "", plot_all = FALSE) {
    where <- get_where(
        success = "True",
        data = data_name,
        battery = battery_name,
        strategy = strat_name,
        timeStep = timestep_name,
        topTen = if (plot_all) "" else 1,
        and = filter)
    table <- paste0("simulation_with_counts_", get_short(strat_name, data_name, battery_name, timestep_name, config_type))
    get_query_(where, table)
}

get_max_query_ <- function(where, table, value) {
    query <- paste(
        "select",
        paste0("max(", value, ") as max_value"), #the stat table only contains max for each configuration, get the max of those here
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
    if (endsWith(data, "L")) {
        res <- paste0(data, "G")
    } else {
        res <- substring(data, 1, 3)
    }
    res
}

top_two_strategies <- c("PRC + DIR + EST", "PEM Control")

get_strategy_top_two <- function(strategy_name) {
    if (strategy_name == "") {
        ""
    } else if (strategy_name %in% top_two_strategies) {
        paste0("strategy in (", paste0(quoted(top_two_strategies), collapse = ","), ")")
    } else {
        paste0("strategy =", quoted(strategy_name))
    }
}

get_max_query <- function(
    data_name = "",
    battery_name = "",
    strat_name = "",
    timestep_name = "",
    config_type = "configuration",
    filter = "",
    value = "maximum_in",
    plot_all = FALSE) {
    where <- get_where(
        success = "True",
        # strategy = strat_name,
        battery = battery_name,
        timeStep = timestep_name,
        and = get_strategy_top_two(strat_name))
    succ_count_table_name <- paste0(
        "successful_counts_",
        get_short(strat_name, data_name, battery_name, timestep_name, config_type = config_type))
    inner_where <- get_where(
        quote_values = FALSE,
        success = "outer_query.success",
        strategy = if (strat_name != "") "outer_query.strategy" else "",
        battery = if (battery_name != "") "outer_query.battery" else "",
        timeStep = if (timestep_name != "") "outer_query.timeStep" else "",
        topTen = if (plot_all) "" else 1)
    table <- paste0("energy_", get_short(strat_name, data_name, battery_name, timestep_name, config_type = config_type), "_stat outer_query")
    combine_where <- function(where, inner_where) {
        append_where(where, paste(config_type, "in (select", config_type, "from", succ_count_table_name, inner_where, ")"))
    }
    if (data_name != "") {
        where <- append_where(where, paste0("data in (", quoted(data_name), ",", quoted(with_without_switch(data_name)), ")"))
    }
        query <- get_max_query_(combine_where(where, inner_where), table, value = value)
        query
}

get_min_query <- function(data_name, battery_name) {
    where <- get_where(
        data = data_name,
        battery = battery_name)
    get_min_query_(where, "data_stat")
}

get_min_query2 <- function() {
    get_min_query_("", "data_stat")
}

get_number_of_configurations <- function(strat_name, data_name = "", battery_name = "", timestep_name = "", config_type = "configuration", filter = "", plot_all = FALSE) {
    where <- get_where(
        strategy = strat_name,
        data = data_name,
        battery = battery_name,
        timeStep = timestep_name,
        topTen = if (plot_all) "" else 1)
    table_name <- paste0("successful_counts_", get_short(strat_name, data_name, battery_name, timestep_name, config_type = config_type))
    query <- paste(
        "SELECT",
        "count(*)",
        "FROM",
        table_name,
        where)
    res <- dbGetQuery(con, query)[1, 1]
    res
}

switch_other_config_type <- function(config_type) {
    if (config_type == "configuration") {
        "guardConfiguration"
    } else if (config_type == "guardConfiguration") {
        "configuration"
    } else if (config_type == "strategy") {
        ""
    } else {
        break
    }
}

get_number_of_combinations <- function(strat_name, data_name = "", battery_name = "", timestep_name = "", config_type = "configuration", filter ="") {
    other_configuration_count <- get_number_of_configurations(strat_name, data_name, battery_name, timestep_name, switch_other_config_type(config_type), filter)

    number_of_combinations * other_configuration_count
}

get_plot_height <- function(number_of_configurations) {
    plot_height <- 2.5 + number_of_configurations / 3
    plot_height
}

expand_right <- function(upper_limit) {
    upper_limit * 1.05
}

do_plot_int <- function(
    data_name,
    battery_name,
    strat_name,
    timestep_name,
    y_name = "configuration",
    y_title = "Configuration",
    file_name,
    filter = "",
    filter_name = "",
    plot_all = FALSE) {
    query <- get_query(data_name, battery_name, strat_name, timestep_name, config_type = y_name, filter = filter, plot_all = plot_all)
    res <- dbGetQuery(con, query)
    if (nrow(res) == 0) {
        return()
    }

    # max_query <- get_max_query(data_name, battery_name, strat_name, timestep_name, config_type = y_name, filter = filter, plot_all = plot_all)
    # max_energy <- dbGetQuery(con, max_query)[1, 1]
    max_energy <- max(res$energy_kwh_in)
    min_energy <- dbGetQuery(con, get_min_query(data_name, battery_name))[1, 1]
    min_energy_2 <- dbGetQuery(con, get_min_query(data_name, "No Battery"))[1, 1]
    min_energy_3 <- dbGetQuery(con, get_min_query(with_without_switch(data_name), battery_name))[1, 1]
    number_of_configurations <- get_number_of_configurations(
        strat_name,
        data_name = data_name,
        battery_name = battery_name,
        timestep_name = timestep_name,
        config_type = y_name,
        filter = filter,
        plot_all = plot_all)
    strat_with_direction <- if (str_contains(strat_name, "Direction")) TRUE else FALSE
    data_has_solar <- data_name != "" && !str_ends(data_name, "L")
    draw_second_vertical_line <- data_has_solar
    min_energy_breaks <- if (draw_second_vertical_line) {
        if (!is.na(min_energy_3)) {
            c(min_energy, min_energy_2, min_energy_3)
        } else {
            c(min_energy, min_energy_2)
        }
    } else {
        min_energy
    }
    if (y_name == "configuration+reduced") {
        y_name <- "configuration"
    }
    max_y_length <- max(str_length(res[[y_name]]))
    plot_title <- paste0(pad_left(y_title, length = max_y_length), " x Success Rate")
    thisplot <- ggplot(
        res,
        aes(
            x = energy_kwh_in,
            y = reorder(
                paste(
                    pad_left(.data[[y_name]], data = y_title),
                    # .data[[y_name]],
                    "x",
                    format_percent(count / totalCount)),
                if (plot_all) count / totalCount else - ordering)
        )) +
        geom_boxplot() +
        expand_limits(x = c(0, expand_right(max_energy))) +
        scale_x_continuous(
            expand = c(0, 0),
            sec.axis = sec_axis(~., breaks = min_energy_breaks, labels = function(x) {round(x, digits = 1)})) +
        labs(
            x = "Total received energy [kWh]",
            title = plot_title) +
        theme(
            plot.title.position = "plot",
            plot.title = element_text(
                family = "Lucida Console",
                size = 9,
                hjust = 0,
                vjust = 0,
                margin = margin(0, 0, -0.2, 0, unit = "cm")),
            axis.text.y = element_text(family = "Lucida Console", face = "bold", size = 9),
            axis.title.y = element_blank()#element_text(angle = 0, hjust = -1.5, vjust = 1)
        )
    if (!is.na(min_energy) && min_energy != 0) {
        thisplot <- thisplot +
            geom_vline(xintercept = min_energy, colour = "blue", linetype = "dashed")
    }
    if (draw_second_vertical_line) {
        thisplot <- thisplot +
            geom_vline(xintercept = min_energy_2, colour = "dimgray", linetype = "dashed")
        if (!is.na(min_energy_3)) {
            thisplot <- thisplot +
                geom_vline(xintercept = min_energy_3, colour = "lightblue", linetype = "dashed")
        }
    }
    ggsave(file_name, thisplot, width = 25, height = get_plot_height(number_of_configurations), units = "cm")

    if (data_has_solar || data_name == "" || TRUE) {
        max_energy <- max(res$energy_kwh_in + res$generationMissed_kwh - res$min_incoming)
        otherplot <- ggplot(
            res,
            aes(
                x = energy_kwh_in + generationMissed_kwh - min_incoming,
                y = reorder(
                    paste(pad_left(.data[[y_name]], data = y_title), "x", format_percent(count / totalCount)),
                    if (plot_all) count / totalCount else - ordering)
            )) +
            geom_boxplot() +
            # geom_point(aes(x = mean(energy_kwh_in + generationMissed_kwh - min_incoming))) +
            expand_limits(x = c(0, expand_right(max_energy))) +
            scale_x_continuous(expand = c(0, 0)) +
            # scale_x_continuous(limits = c(0, max_energy)) +
            labs(
                x = "Total wasted energy [kWh]",
                title = plot_title) +
            theme(
                plot.title.position = "plot",
                plot.title = element_text(
                    family = "Lucida Console",
                    size = 9,
                    hjust = 0,
                    vjust = 0),
                axis.text.y = element_text(family = "Lucida Console", size = 9, face = "bold"),
                axis.title.y = element_blank()
            )

        ggsave(str_replace(file_name, "\\.", "_wasted."), otherplot, width = 25, height = get_plot_height(number_of_configurations), units = "cm")
    }
}

do_plot <- function(data_name, battery_name, strat_name, timestep_name, file_name) {
    do_plot_int(data_name, battery_name, strat_name, timestep_name, "configuration", "Configuration", file_name)
    do_plot_int(data_name, battery_name, strat_name, timestep_name, "guardConfiguration", "Guard Configuration", str_replace(file_name, "\\.", "_guardConfig."))
}

do_plot_guards <- function(data_name, battery_name, timestep_name, file_name, filter = "") {
    do_plot_int(data_name, battery_name, strat_name = "", timestep_name, "guardConfiguration", "Guard Configuration", file_name, filter = filter)
}

do_plot_strategies <- function(data_name, battery_name, timestep_name, file_name, filter = "") {
    do_plot_int(data_name, battery_name, strat_name = "", timestep_name, "strategy", "Strategy", file_name, filter = filter, plot_all = TRUE)
}

do_plot_reduced <- function(data_name, battery_name, strat_name, timestep_name, file_name, filter = "", plot_all = FALSE) {
    do_plot_int(data_name, battery_name, strat_name, timestep_name, "configuration+reduced", "Configuration", file_name, filter = filter, plot_all = plot_all)
}

exists_successful_ <- function(where) {
    query <- paste("select exists(select 1 from simulation", where, ")")
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


for (battery_name in battery_configurations) {
    for (timestep_name in time_steps) {
        for (data_name in data_configurations) {
            for (strat_name in strategy_names) {
                if (options$strategy != "" && options$strategy != strat_name) {
                    next
                }
                if (exists_successful(data_name, battery_name, strat_name, timestep_name)) {
                    do_plot(
                        data_name,
                        battery_name,
                        strat_name,
                        timestep_name,
                        paste0(
                            "plots/boxplot_",
                            normalize(battery_name),
                            "_",
                            normalize(data_name),
                            "_",
                            normalize(strat_name),
                            "_",
                            normalize(timestep_name),
                            ".pdf"))
                    do_plot_reduced(
                        data_name,
                        battery_name,
                        strat_name,
                        timestep_name,
                        paste0(
                            "plots/boxplot_",
                            normalize(battery_name),
                            "_9kW_",
                            normalize(data_name),
                            "_",
                            normalize(strat_name),
                            "_",
                            normalize(timestep_name),
                            ".pdf"),
                        filter = "guardConfiguration in ('p(0.0, 9.0) c(0.7, 0.5)', 'p(0.0) c(0.5)')")
                    do_plot_reduced(
                        data_name,
                        battery_name,
                        strat_name,
                        timestep_name,
                        paste0(
                            "plots/boxplot_",
                            normalize(battery_name),
                            "_9kW_",
                            normalize(data_name),
                            "_",
                            normalize(strat_name),
                            "_",
                            normalize(timestep_name),
                            "_all",
                            ".pdf"),
                        filter = "guardConfiguration in ('p(0.0, 9.0) c(0.7, 0.5)', 'p(0.0) c(0.5)')",
                        plot_all = TRUE)
                }
            }
            if (exists_successful(data_name = data_name, battery_name = battery_name, timestep_name = timestep_name)) {
                do_plot_guards(
                    data_name,
                    battery_name,
                    timestep_name,
                    paste0(
                        "plots/boxplot_",
                        "guards_",
                        normalize(battery_name),
                        "_",
                        normalize(data_name),
                        "_",
                        normalize(timestep_name),
                        ".pdf"),
                    filter = "strategy not like '%NoSend'")
                do_plot_strategies(
                    data_name,
                    battery_name,
                    timestep_name,
                    paste0(
                        "plots/boxplot_",
                        normalize(battery_name),
                        "_",
                        normalize(data_name),
                        "_",
                        normalize(timestep_name),
                        ".pdf"),
                    filter = "guardConfiguration in ('p(0.0, 9.0) c(0.7, 0.5)', 'p(0.0) c(0.5)')")
            }
        }
        if (exists_successful(battery_name = battery_name, timestep_name = timestep_name)) {
            do_plot_guards(
                "",
                battery_name,
                timestep_name,
                paste0(
                    "plots/boxplot_",
                    "guards_",
                    normalize(battery_name),
                    "_",
                    normalize(timestep_name),
                    ".pdf"),
                filter = "strategy not like '%NoSend'")
            do_plot_strategies(
                "",
                battery_name,
                timestep_name,
                paste0(
                    "plots/boxplot_",
                    normalize(battery_name),
                    "_",
                    normalize(timestep_name),
                    ".pdf"),
                filter = "guardConfiguration in ('p(0.0, 9.0) c(0.7, 0.5)', 'p(0.0) c(0.5)')")
        }
    }
}

dbDisconnect(con)