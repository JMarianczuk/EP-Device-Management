library(RSQLite)

source("preprocessing/preprocessing.r")

preprocess_distinct_single <- function(con, name) {
    table_name <- paste0("distinct_", name)
    query <- paste(
        "CREATE TABLE IF NOT EXISTS",
        table_name,
        "AS",
        "SELECT",
        paste(
            name,
            "max(success) as success",
            sep = ","),
        "from simulation",
        "group by",
        name)
    res <- dbExecute(con, query)
    res <- insert_preprocessed_table_name(con, table_name)
}

preprocess_distinct_dual <- function(con, left_column, right_column) {
    table_name <- paste0("distinct_", left_column, "_", right_column)
    query <- paste(
        "CREATE TABLE IF NOT EXISTS",
        table_name,
        "AS",
        "SELECT",
        paste(
            left_column,
            right_column,
            "max(success) as success",
            sep = ","),
        "from",
        "simulation",
        "group by",
        paste(left_column, right_column, sep = ","))
    res <- dbExecute(con, query)
    res <- insert_preprocessed_table_name(con, table_name)
}

preprocess_distinct <- function(con) {
    for (column in c(
        "data",
        "timeStep",
        "strategy",
        "packetSize",
        "probability",
        "battery")) {
        preprocess_distinct_single(con, column)
    }
    preprocess_distinct_dual(con, "strategy", "configuration")
}