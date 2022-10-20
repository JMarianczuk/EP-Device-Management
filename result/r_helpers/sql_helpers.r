library(RSQLite)

get_where <- function(data_name = "", strat_name = "", timestep_name = "", success = "True") {
    success_where <- paste('success="', success, '"', sep = "")
    data_where <- if (data_name != "") paste('data="', data_name, '"', sep = "") else ""
    strat_where <- if (strat_name != "") paste('strategy="', strat_name, '"', sep = "") else ""
    timestep_where <- if (timestep_name != "") paste('timestep="', timestep_name, '"', sep = "") else ""
    where <- paste("where", success_where)
    if (data_where != "") {
        where <- paste(where, "and", data_where)
    }
    if (strat_where != "") {
        where <- paste(where, "and", strat_where)
    }
    if (timestep_where != "") {
        where <- paste(where, "and", timestep_where)
    }
    where
}

create_db_connection <- function() {
    con <- dbConnect(SQLite(), "results.sqlite")
    some_res <- dbExecute(con, "PRAGMA case_sensitive_like=ON;")
    con
}