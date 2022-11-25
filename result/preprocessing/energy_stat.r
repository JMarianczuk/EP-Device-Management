library(RSQLite)

source("preprocessing/preprocessing.r")
source("r_helpers/sql_helpers.r")

preprocess_energy_stats_single <- function(con, by, short) {
    table_name <- paste0("energy_", short, "_stat")
    query <- paste(
        "CREATE TABLE IF NOT EXISTS",
        table_name,
        "AS",
        "SELECT",
        paste(
            paste(by, collapse = ","),
            "success",
            "min(energy_kwh_in) as minimum_in",
            "max(energy_kwh_in) as maximum_in",
            "avg(energy_kwh_in) as average_in",
            "min(energy_kwh_out) as minimum_out",
            "max(energy_kwh_out) as maximum_out",
            "avg(energy_kwh_out) as average_out",
            "min(generationMissed_kwh) as minimum_missed",
            "max(generationMissed_kwh) as maximum_missed",
            "avg(generationMissed_kwh) as average_missed",
            sep = ","
        ),
        "FROM",
        "simulation",
        "GROUP BY",
        paste(by, collapse = ","),
        ",success")
    res <- dbExecute(con, query)
    res <- insert_preprocessed_table_name(con, table_name)

}

preprocess_energy_stats <- function(con) {
    by <- c("strategy", "configuration")
    preprocess_energy_stats_single(con, by, "sc")
    by <- append(by, "data")
    preprocess_energy_stats_single(con, by, "scd")
    by_t <- append(by, "timeStep")
    preprocess_energy_stats_single(con, by_t, "scdt")
    by_b <- append(by, "battery")
    preprocess_energy_stats_single(con, by_b, "scdb")
    by <- append(by_b, "timeStep")
    preprocess_energy_stats_single(con, by, "scdbt")

    by <- c("strategy", "guardConfiguration", "data", "battery")
    preprocess_energy_stats_single(con, by, "sgdb")
    by <- append(by, "timeStep")
    preprocess_energy_stats_single(con, by, "sgdbt")

    by <- c("configuration", "battery", "timeStep")
    preprocess_energy_stats_single(con, by, "cbt")
    by <- c("configuration", "data", "battery", "timeStep")
    preprocess_energy_stats_single(con, by, "cdbt")
    by <- c("guardConfiguration", "battery", "timeStep")
    preprocess_energy_stats_single(con, by, "gbt")
    by <- c("guardConfiguration", "data", "battery", "timeStep")
    preprocess_energy_stats_single(con, by, "gdbt")

}