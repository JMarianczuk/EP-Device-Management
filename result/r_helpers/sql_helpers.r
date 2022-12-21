library(RSQLite)
library(stringr)

append_where <- function(where, append = "") {
    if (append == "") {
        where
    } else {
        if (where == "") {
            paste("where", append)
        } else {
            paste(where, "and", append)
        }
    }
}

append_and <- function(where, append = "") {
    if (append == "") {
        where
    } else {
        if (where == "") {
            append
        } else {
            paste(where, "and", append)
        }
    }
}

combine_where <- function(where, other_where = "") {
    if (other_where == "") {
        where
    } else {
        if (startsWith(other_where, "where ")) {
            other_where <- substring(other_where, str_length("where ") + 1)
        }
        paste(where, "and", other_where)
    }
}

get_where <- function(
    where = "",
    strategy = "",
    configuration = "",
    data = "",
    guardConfiguration = c(),
    battery = "",
    packetSize = "",
    probability = "",
    timeStep = "",
    success = "",
    fail = "",
    seed = "",
    topTen = "",
    and = "",
    quote_values = TRUE) {
    w <- function(name, value, wrap = if (quote_values) '"' else "") {
        if (length(value) == 0) {
            ""
        } else if (length(value) == 1) {
            if (value != "") {
                paste0(name, "=", wrap, value, wrap)
            } else {
                ""
            }
        } else {
            paste0(
                "(",
                paste0(name, "=", wrap, value, wrap, collapse = " OR "),
                ")")
        }
    }
    app <- function(append, where) {
        append_where(where, append)
    }

    where <- app(w("strategy", strategy),
        app(w("configuration", configuration),
        app(w("data", data),
        app(w("guardConfiguration", guardConfiguration),
        app(w("battery", battery),
        app(w("packetSize", packetSize, wrap = ""),
        app(w("probability", probability, wrap = ""),
        app(w("timeStep", timeStep),
        app(w("success", success),
        app(w("fail", fail),
        app(w("seed", seed, wrap = ""),
        app(w("topTen", topTen, wrap = ""),
        where))))))))))))
    where <- append_where(where, and)
    where
}

extract_where <- function(where, name, quoted = TRUE) {
    pattern <- "[a-zA-Z0-9\\[\\]\\.\\+\\s]*"
    if (quoted) {
        pattern <- quoted(pattern)
    }
    location <- str_locate(where, paste0(name, "=", pattern))
    start_offset <- str_length(name) + 1
    end_offset <- 0
    if (quoted) {
        start_offset <- start_offset + 1
        end_offset <- end_offset - 1
    }
    value <- substr(where, location[1,1] + start_offset, location[1,2] + end_offset)
    value
}

create_db_connection <- function(database_name = "results.sqlite") {
    con <- dbConnect(SQLite(), database_name)
    some_res <- dbExecute(con, "PRAGMA case_sensitive_like=ON;")
    con
}