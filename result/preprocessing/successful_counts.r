library(RSQLite)

source("preprocessing/preprocessing.r")
source("preprocessing/distinct_values.r")
source("r_helpers/array_helpers.r")
source("r_helpers/functional_helpers.r")
source("r_helpers/sql_helpers.r")

successful_counts_table_name <- function(name) {
    table_name <- paste0("successful_counts_", name)
    table_name
}

calculate_top_ten_ordering <- function(con, by, table_name) {
    query <- paste(
        "select",
        "ROW_NUMBER() OVER (",
        "PARTITION BY", paste(vector_without(by, "configuration"), collapse = ","),
        "ORDER BY count DESC",
        ") orderNumber,",
        "ROWID as rowId",
        "FROM",
        table_name,
        get_where(topTen = 1))
    res <- dbGetQuery(con, query)
    for (i in 1:nrow(res)) {
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
    values) {
    top_x <- 12
    cartesian_execute(
        values,
        execute_func = function(where) {
            query <- paste(
                "select min(count)",
                "from",
                "(",
                "select * from",
                table_name,
                where,
                "ORDER BY",
                "count DESC",
                "LIMIT",
                top_x,
                ")")
            res <- dbGetQuery(con, query)[1,1]
            if (!is.na(res)) {
                query <- paste(
                    "UPDATE",
                    table_name,
                    "SET",
                    "topTen=1",
                    append_where(where, paste("count >=", res))
                )
                res <- dbExecute(con, query)
            }
        },
        accumulator_func = function(group, where, index) {
            local_where <- paste0(by[index], '="', group, '"')
            where <- append_where(where, local_where)
            where
        },
        get_where(success = "True"),
        filter = function(index) {
            skip_index <- by[index] == "configuration"
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

calculate_top_ten <- function(con, by, name) {
    table_name <- successful_counts_table_name(name)
    has_been_calculated <- dbGetQuery(con, paste("select exists(select 1 from", table_name, "where topTen=1);"))[1,1]
    if (has_been_calculated == 0) {
        values <- c()
        for (group in by) {
            if (group != "configuration") {
                query <- paste0("SELECT * FROM distinct_", group)
                res <- dbGetQuery(con, query)
                res <- res[,1]
                values <- c(values, list(res))
            } else {
                values <- c(values, list("no_config"))
            }
        }
        internal_calculate_top_ten(con, by, table_name, values)
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
    calculate_top_ten(con, by, name)
    cross_calculate <- ("strategy" %in% by && "configuration" %in% by && "data" %in% by)
    if (cross_calculate) {
        cross_calculate_top_ten(con, by, name)
    }
    calculate_top_ten_ordering(con, by, table_name)
    if (cross_calculate) {
        cross_calculate_top_ten_ordering(con, by, table_name)
    }

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
            sep = ","),
        "FROM",
        "simulation s",
        "JOIN",
        paste0("successful_counts_", name, " c"),
        "ON",
        paste0("s.", by, " = c.", by, collapse = " AND "),
        "AND s.success = c.success",
        ";")
    res <- dbExecute(con, query)
    insert_preprocessed_view_name(con, view_name)
}