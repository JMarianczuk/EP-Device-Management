library(RSQLite)

preprocess_successful_counts <- function(by, name, con) {
    query <- paste(
        "CREATE TABLE IF NOT EXISTS",
        paste("successful_counts_", name, sep = ""),
        "AS",
        "SELECT",
        paste(
            paste(by, collapse = ","),
            "success",
            "count(*) as count",
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

    query <- paste(
        "CREATE TEMPORARY VIEW IF NOT EXISTS",
        paste("simulation_with_counts_", name, sep = ""),
        "AS",
        "SELECT",
        paste(
            "s.*",
            "c.count",
            sep = ","),
        "FROM",
        "simulation s",
        "JOIN",
        paste("successful_counts_", name, " c", sep = ""),
        "ON",
        paste(
            "s.", by, " = c.", by, sep = "", collapse = " AND "),
        "AND s.success = c.success",
        ";")
    res <- dbExecute(con, query)

    # query <- paste(
    #     "CREATE TABLE IF NOT EXISTS",
    #     paste("simulation_with_counts_", name, sep = ""),
    #     "AS",
    #     "SELECT",
    #     paste(
    #         "s.*",
    #         "c.count",
    #         sep = ","),
    #     "FROM",
    #     "simulation s",
    #     "JOIN",
    #     paste("successful_counts_", name, " c", sep = ""),
    #     "ON",
    #     paste(
    #         "s.", by, " = c.", by, sep = "", collapse = " AND "),
    #     "AND s.success = c.success",
    #     ";")
    # dbExecute(con, query)
}