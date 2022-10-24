library(RSQLite)

preprocess_energy_counts <- function(x, group, con) {
    query <- paste(
        "CREATE TABLE IF NOT EXISTS",
        paste("energy_count_", x, "_", group, sep = ""),
        "AS",
        "SELECT",
        paste(x,
            group,
            "avg(energy_kwh_in) as avg_energy_in",
            "avg(incomingPowerGuards) as avg_powerGuard_in",
            "avg(outgoingPowerGuards) as avg_powerGuard_out",
            "avg(emptyCapacityGuards) as avg_capacityGuard_empty",
            "avg(fullCapacityGuards) as avg_capacityGuard_full",
            "avg(oscillationGuards) as avg_oscillationGuard",
            "count(*) as count",
            sep = ","),
        "FROM",
        "simulation",
        'where success="True"',
        "GROUP BY",
        paste(x, group, sep = ","))
    res <- dbExecute(con, query)
}

preprocess_total_counts <- function(x, group, con) {
    query <- paste(
        "CREATE TABLE IF NOT EXISTS",
        paste("total_count_", x, "_", group, sep = ""),
        "AS",
        "SELECT",
        paste(
            x,
            group,
            "count(*) as totalCount",
            sep = ","),
        "FROM",
        "simulation",
        "GROUP BY",
        paste(x, group, sep = ","))
    res <- dbExecute(con, query)
}