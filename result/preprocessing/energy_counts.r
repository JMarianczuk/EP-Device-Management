library(RSQLite)

source("preprocessing/preprocessing.r")
source("preprocessing/successful_counts.r")

source("r_helpers/sql_helpers.r")

energy_count_table_name <- function(by) {
    by <- unique(by)
    order(by)
    table_name <- paste0("energy_count_", paste(by, collapse = "_"))
    table_name
}

get_energy_count_query <- function(
    by,
    select_by,
    table_name,
    select_table_name,
    filter) {
    query <- paste(
        "CREATE TABLE IF NOT EXISTS",
        table_name,
        "AS",
        "SELECT",
        paste(
            paste(select_by, collapse = ","),
            "avg(energy_kwh_in) as avg_energy_in",
            "avg(energy_kwh_out) as avg_energy_out",
            "avg(generationMissed_kwh) as avg_gen_missed",
            "avg(energy_kwh_in + generationMissed_kwh) as avg_sum",
            "avg(incomingPowerGuards) as avg_powerGuard_in",
            "avg(outgoingPowerGuards) as avg_powerGuard_out",
            "avg(emptyCapacityGuards) as avg_capacityGuard_empty",
            "avg(fullCapacityGuards) as avg_capacityGuard_full",
            "avg(oscillationGuards) as avg_oscillationGuard",
            "avg(batteryMin) as avg_min_battery",
            "avg(batteryMax) as avg_max_battery",
            "avg(batteryAvg) as avg_avg_battery",
            "count(*) as count",
            sep = ","),
        "FROM",
        select_table_name,
        filter,
        "GROUP BY",
        paste(by, collapse = ","))
    query
}

preprocess_energy_counts <- function(
    by,
    con,
    select_by = c(),
    filter = "") {
    by <- unique(by)
    if (length(select_by) == 0) {
        select_by <- by
    }
    table_name <- energy_count_table_name(by)
    where <- get_where(success = "True")
    if (filter != "") {
        where <- append_where(where, filter)
    }
    query <- get_energy_count_query(
        by,
        select_by,
        table_name,
        "simulation",
        where)
    res <- dbExecute(con, query)
    res <- insert_preprocessed_table_name(con, table_name)
}

preprocess_filtered_energy_counts <- function(by, con) {
    by <- unique(by)
    table_name <- paste0(energy_count_table_name(by), "_tt")
    sc_by <- c("strategy", "configuration")
    sc_name <- "sc"
    if ("data" %in% by) {
        sc_by <- c(sc_by, "data")
        sc_name <- "scd"
    }
    sc_table_name <- paste0("simulation_with_counts_", sc_name)
    preprocess_successful_counts(sc_by, sc_name, con)
    where <- get_where(
        success = "True",
        topTen = 1)
    query <- get_energy_count_query(by, table_name, sc_table_name, where)
    res <- dbExecute(con, query)
    res <- insert_preprocessed_table_name(con, table_name)
}

total_count_table_name <- function(by) {
    by <- unique(by)
    order(by)
    table_name <- paste0("total_count_", paste(by, collapse = "_"))
    table_name
}

get_total_count_query <- function(
    by,
    select_by,
    table_name,
    select_table_name,
    filter) {
    query <- paste(
        "CREATE TABLE IF NOT EXISTS",
        table_name,
        "AS",
        "SELECT",
        paste(
            paste(select_by, collapse = ","),
            "count(*) as totalCount",
            sep = ","),
        "FROM",
        select_table_name,
        filter,
        "GROUP BY",
        paste(by, collapse = ","))
}

preprocess_total_counts <- function(
    by,
    con,
    select_by = c(),
    filter = "") {
    by <- unique(by)
    if (length(select_by) == 0) {
        select_by <- by
    }
    table_name <- total_count_table_name(by)
    where <- ""
    if (filter != "") {
        where <- append_where(where, filter)
    }
    query <- get_total_count_query(
        by,
        select_by,
        table_name,
        "simulation s",
        where)
    res <- dbExecute(con, query)
    res <- insert_preprocessed_table_name(con, table_name)
}

preprocess_filtered_total_counts <- function(by, con) {
    by <- unique(by)
    table_name <- paste0(total_count_table_name(by), "_tt")
    sc_by <- c("strategy", "configuration")
    sc_name <- "sc"
    if ("data" %in% by) {
        sc_by <- c(sc_by, "data")
        sc_name <- "scd"
    }
    preprocess_successful_counts(sc_by, sc_name, con)
    view_name <- create_top_ten_successful_view(con, sc_by, sc_name)
    where <- paste(
        "JOIN",
        view_name, "c",
        "ON",
        "s.strategy = c.strategy",
        "AND",
        "s.configuration = c.configuration")
    query <- get_total_count_query(by, table_name, "simulation s", where)
    res <- dbExecute(con, query)
    insert_preprocessed_table_name(con, table_name)
}

create_top_ten_successful_view <- function(con, sc_by, sc_name) {
    view_name <- paste0("topTen_successful_", sc_name)
    where <- get_where(topTen = 1)
    query <- paste(
        "CREATE VIEW IF NOT EXISTS",
        view_name,
        "AS",
        "SELECT",
        paste(sc_by, collapse = ","),
        "FROM",
        paste0("successful_counts_", sc_name),
        where)
    res <- dbExecute(con, query)
    insert_preprocessed_view_name(con, view_name)
    view_name
}