library(RSQLite)

source("preprocessing/preprocessing.r")
source("preprocessing/distinct_values.r")

source("r_helpers/array_helpers.r")
source("r_helpers/functional_helpers.r")
source("r_helpers/sql_helpers.r")
source("r_helpers/pareto.r")

successful_counts_table_name <- function(name) {
    table_name <- paste0("successful_counts_", name)
    table_name
}

calculate_top_ten_ordering <- function(con, by, table_name, config_name) {
    query <- paste(
        "select",
        "ROW_NUMBER() OVER (",
        "PARTITION BY", paste(vector_without(by, config_name), collapse = ","),
        "ORDER BY count DESC",
        ") orderNumber,",
        "ROWID as rowId",
        "FROM",
        table_name,
        get_where(topTen = 1))
    res <- dbGetQuery(con, query)
    for (i in seq_len(nrow(res))) {
        row <- res[i,]
        query <- paste(
            "update",
            table_name,
            "SET",
            "ordering =", row$orderNumber,
            "where",
            "ROWID =", row$rowId)
        update_res <- dbExecute(con, query)
    }
}

cross_calculate_top_ten_ordering <- function(con, by, table_name) {
    query <- paste(
        "select",
        paste(
            paste(by, collapse = ","),
            "ordering",
            sep = ","),
        "FROM",
        table_name,
        get_where(topTen = 1, and = "data LIKE '%noSolar'"))
    res <- dbGetQuery(con, query)
    for (i in 1:nrow(res)) {
        row <- res[i,]
        where <- get_where(
            topTen = 1,
            strategy = row$strategy,
            configuration = row$configuration,
            data = substr(row$data, 1, 4))
        if ("battery" %in% by) {
            where <- get_where(
                where = where,
                battery = row$battery)
        }
        if ("timeStep" %in% by) {
            where <- get_where(
                where = where,
                timeStep = row$timeStep)
        }
        query <- paste(
            "UPDATE",
            table_name,
            "SET ordering =", row$ordering,
            where)
        update_res <- dbExecute(con, query)
    }
}

internal_calculate_top_ten <- function(
    con,
    by,
    table_name,
    config_name,
    values) {
    top_x <- 200
    cartesian_execute(
        values,
        execute_func = function(where) {
            wasted_offset <- 0
            data_name <- ""
            if ("data" %in% by) {
                data_name <- extract_where(where, "data")
            }
            battery_name <- ""
            if ("battery" %in% by) {
                battery_name <- extract_where(where, "battery")
            }
            if (data_name != "" || battery_name != "") {
                min_energy_query <- paste(
                    "select",
                    "min(totalIncoming_Kwh)",
                    "from",
                    "data_stat",
                    get_where(data = data_name, battery = battery_name))
                wasted_offset <- dbGetQuery(con, min_energy_query)[1, 1]
            }
            query <- paste(
                "select",
                paste(
                    paste(by, collapse = ","),
                    "energy_kwh_in",
                    paste("energy_kwh_in + generationMissed_kwh -", wasted_offset, "as energy_kwh_wasted"),
                    sep = ","),
                "from",
                "simulation",
                where)
            res <- dbGetQuery(con, query)
            formula_by <- paste(by, collapse = " + ")
            energy_in_formula <- as.formula(paste("energy_kwh_in ~", formula_by))
            wasted_energy_formula <- as.formula(paste("energy_kwh_wasted ~", formula_by))
            count <- setNames(
                aggregate(energy_in_formula, data = res, FUN = length),
                c(by, "count"))
            min_energy_in <- setNames(
                aggregate(energy_in_formula, data = res, FUN = min),
                c(by, "min_energy_in"))
            median_energy_in <- setNames(
                aggregate(energy_in_formula, data = res, FUN = median),
                c(by, "median_energy_in"))
            average_energy_in <- setNames(
                aggregate(energy_in_formula, data = res, FUN = mean),
                c(by, "average_energy_in"))
            min_wasted <- setNames(
                aggregate(wasted_energy_formula, data = res, FUN = min),
                c(by, "min_wasted"))
            median_wasted <- setNames(
                aggregate(wasted_energy_formula, data = res, FUN = median),
                c(by, "median_wasted"))
            average_wasted <- setNames(
                aggregate(wasted_energy_formula, data = res, FUN = mean),
                c(by, "average_wasted"))
            aggregate_res <- Reduce(
                function(left, right) merge(left, right, all = TRUE, by = by),
                list(
                    count,
                    min_energy_in,
                    median_energy_in,
                    average_energy_in,
                    min_wasted,
                    median_wasted,
                    average_wasted
                ))
            pareto <- calculate_pareto_set(aggregate_res, function(left, right) {
                c(
                    compare_individual(left$count, right$count),
                    compare_individual(left$min_energy_in, right$min_energy_in, greater_value_dominates = FALSE),
                    compare_individual(left$median_energy_in, right$median_energy_in, greater_value_dominates = FALSE),
                    compare_individual(left$average_energy_in, right$average_energy_in, greater_value_dominates = FALSE),
                    compare_individual(left$min_wasted, right$min_wasted, greater_value_dominates = FALSE),
                    compare_individual(left$median_wasted, right$median_wasted, greater_value_dominates = FALSE),
                    compare_individual(left$average_wasted, right$average_wasted, greater_value_dominates = FALSE)
                )
            })
            for (pareto_index in seq_len(nrow(pareto))) {
                query <- paste(
                    "UPDATE",
                    table_name,
                    "SET",
                    "topTen=1",
                    if (config_name == "configuration") {
                        get_where(where, configuration = pareto[pareto_index,]$configuration)
                    } else if (config_name == "guardConfiguration") {
                        get_where(where, guardConfiguration = pareto[pareto_index,]$guardConfiguration)
                    }
                )
                res <- dbExecute(con, query)
            }
            # if (!is.na(res)) {
            #     query <- paste(
            #         "UPDATE",
            #         table_name,
            #         "SET",
            #         "topTen=1",
            #         append_where(where, paste("count >=", res))
            #     )
            #     res <- dbExecute(con, query)
            # }
        },
        accumulator_func = function(group, where, index) {
            local_where <- paste0(by[index], '="', group, '"')
            where <- append_where(where, local_where)
            where
        },
        get_where(success = "True"),
        filter = function(index) {
            skip_index <- by[index] == config_name
            skip_index
        }
    )
}

cross_calculate_top_ten_update <- function(con, table_name, strategy, data_from, data_to, timestep = "", battery = "") {
    common_where <- get_where(
        success = "True",
        strategy = strategy,
        timeStep = timestep,
        battery = battery)
    inner_where <- get_where(
        where = common_where,
        topTen = 1,
        data = data_from)

    outer_where <- get_where(
        where = common_where,
        data = data_to,
        and = paste("configuration in ( select configuration from", table_name, inner_where, ")"))
    query <- paste(
        "UPDATE",
        table_name,
        "SET topTen=1",
        outer_where)
    res <- dbExecute(con, query)
}

cross_calculate_top_ten <- function(con, by, name) {
    table_name <- successful_counts_table_name(name)
    time_steps <- c("")
    if ("timeStep" %in% by) {
        time_steps <- dbGetQuery(con, "select * from distinct_timeStep")[,1]
    }
    batteries <- c("")
    if ("battery" %in% by) {
        batteries <- dbGetQuery(con, "select * from distinct_battery")[,1]
    }
    for (data in c("Res1", "Res4")) {
        for (strategy in dbGetQuery(con, "select * from distinct_strategy")[,1]) {
            for (battery in batteries) {
                for (timestep in time_steps) {
                    cross_calculate_top_ten_update(
                        con, table_name, strategy, data, paste(data, "noSolar"), timestep = timestep, battery = battery)
                    cross_calculate_top_ten_update(
                        con, table_name, strategy, paste(data, "noSolar"), data, timestep = timestep, battery = battery)
                }
            }
        }
    }
}

calculate_top_ten <- function(con, by, name, config_name) {
    table_name <- successful_counts_table_name(name)
    query <- paste(
        "select exists(select 1 from",
        table_name,
        get_where(topTen = 1),
        ") as has_been_calculated;")
    has_been_calculated <- dbGetQuery(con, query)[1,1]
    if (has_been_calculated == 0) {
        values <- c()
        for (group in by) {
            if (group != config_name) {
                query <- paste0("SELECT * FROM distinct_", group)
                res <- dbGetQuery(con, query)
                res <- res[,1]
                values <- c(values, list(res))
            } else {
                values <- c(values, list("no_config"))
            }
        }
        internal_calculate_top_ten(con, by, table_name, config_name, values)
    }
}

preprocess_successful_counts <- function(by, name, con) {
    table_name <- successful_counts_table_name(name)
    query <- paste(
        "CREATE TABLE IF NOT EXISTS",
        table_name,
        "AS",
        "SELECT",
        paste(
            paste(by, collapse = ","),
            "success",
            "count(*) as count",
            "0 as topTen",
            "0 as ordering",
            sep = ","),
        "FROM",
        "simulation",
        "GROUP BY",
        paste(
            paste(by, collapse = ","),
            "success",
            sep = ","),
        ";")
    res <- dbExecute(con, query)
    res <- insert_preprocessed_table_name(con, table_name)

    view_name <- paste0("simulation_with_counts_", name)
    query <- paste(
        "CREATE VIEW IF NOT EXISTS",
        view_name,
        "AS",
        "SELECT",
        paste(
            "s.*",
            "c.count",
            "c.topTen",
            "c.ordering",
            "d.totalIncoming_kWh as min_incoming",
            sep = ","),
        "FROM",
        "simulation s",
        "JOIN",
        paste0("successful_counts_", name, " c"),
        "ON",
        paste0("s.", by, " = c.", by, collapse = " AND "),
        "AND s.success = c.success",
        "JOIN",
        "data_stat d",
        "ON",
        paste("d.data = s.data", "d.battery = s.battery", sep = " AND "),
        ";")
    res <- dbExecute(con, query)
    insert_preprocessed_view_name(con, view_name)

    preprocess_distinct(con)
    config_name <- ""
    if ("configuration" %in% by) {
        config_name <- "configuration"
    }
    if ("guardConfiguration" %in% by) {
        config_name <- "guardConfiguration"
    }
    if (config_name != "") {
        calculate_top_ten(con, by, name, config_name)
        cross_calculate <- ("strategy" %in% by && "configuration" %in% by && "data" %in% by)
        cross_calculate <- FALSE #such a big difference, no need
        if (cross_calculate) {
            cross_calculate_top_ten(con, by, name)
        }
        calculate_top_ten_ordering(con, by, table_name, config_name)
        if (cross_calculate) {
            cross_calculate_top_ten_ordering(con, by, table_name)
        }
    }
}