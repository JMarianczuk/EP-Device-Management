library(RSQLite)

preprocess_distinct_single <- function(con, name) {
    query <- paste(
        "CREATE TABLE IF NOT EXISTS",
        paste("distinct_", name, sep = ""),
        "AS",
        "SELECT DISTINCT",
        name,
        "from simulation")
    res <- dbExecute(con, query)
}

preprocess_distinct_dual <- function(con, left_column, right_column) {
    query <- paste(
        "CREATE TABLE IF NOT EXISTS",
        paste("distinct_", left_column, "_", right_column, sep = ""),
        "AS",
        "SELECT DISTINCT",
        paste(left_column, right_column, sep = ","),
        "from",
        "simulation")
    res <- dbExecute(con, query)
}

preprocess_distinct <- function(con) {
    for (column in c("data", "timeStep", "strategy", "packetSize", "probability", "battery")) {
        preprocess_distinct_single(con, column)
    }
    preprocess_distinct_dual(con, "strategy", "configuration")
}