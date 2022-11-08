library(RSQLite)

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

get_where <- function(
    where = "",
    strategy = "",
    configuration = "",
    data = "",
    battery = "",
    packetSize = "",
    probability = "",
    timeStep = "",
    success = "",
    fail = "",
    seed = "",
    topTen = "",
    and = "") {
    w <- function(name, value, wrap = '"') {
        if (value != "") {
            paste(name, "=", wrap, value, wrap, sep = "")
        } else {
            ""
        }
    }
    app <- function(append, where) {
        append_where(where, append)
    }

    where <- app(w("strategy", strategy),
        app(w("configuration", configuration),
        app(w("data", data),
        app(w("battery", battery),
        app(w("packetSize", packetSize, wrap = ""),
        app(w("probability", probability, wrap = ""),
        app(w("timeStep", timeStep),
        app(w("success", success),
        app(w("fail", fail),
        app(w("seed", seed, wrap = ""),
        app(w("topTen", topTen, wrap = ""),
        where)))))))))))
    where <- append_where(where, and)
    where
}

create_db_connection <- function() {
    con <- dbConnect(SQLite(), "results.sqlite")
    some_res <- dbExecute(con, "PRAGMA case_sensitive_like=ON;")
    con
}