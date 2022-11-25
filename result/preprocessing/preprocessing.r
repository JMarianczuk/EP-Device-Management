library(RSQLite)

create_preprocessed_table_table <- function(con, table_name) {
    query <- paste(
        "CREATE TABLE IF NOT EXISTS",
        table_name,
        "(",
            '"TableName"',
            "TEXT NOT NULL,",
            "PRIMARY KEY",
            "(",
                '"TableName"',
            ")",
        ")")
    res <- dbExecute(con, query)
}

insert_preprocessed_table_name <- function(con, table_name) {
    create_preprocessed_table_table(con, "PreprocessedTables")
    query <- paste(
        "INSERT OR IGNORE INTO",
        "PreprocessedTables",
        "VALUES",
        "(",
        paste0('"', table_name, '"'),
        ")"
    )
    res <- dbExecute(con, query)
}

insert_preprocessed_view_name <- function(con, view_name) {
    create_preprocessed_table_table(con, "PreprocessedViews")
    query <- paste(
        "INSERT OR IGNORE INTO",
        "PreprocessedViews",
        "VALUES",
        "(",
        paste0('"', view_name, '"'),
        ")")
    res <- dbExecute(con, query)
}

drop_preprocessed_tables <- function(con, filter = "") {
    where <- get_where(and = filter)
    cat(paste0("Dropping preprocessed ", where, "\n"))

    preprocessed_table_names <- dbGetQuery(con, paste("SELECT * FROM PreprocessedTables", where))
    for (table_name in preprocessed_table_names[,1]) {
        query <- paste("DROP TABLE IF EXISTS", table_name, ";")
        res <- dbExecute(con, query)
    }
    res <- dbExecute(con, paste("DELETE FROM PreprocessedTables", where))

    preprocessed_view_names <- dbGetQuery(con, paste("SELECT * FROM PreprocessedViews", where))
    for (view_name in preprocessed_view_names[,1]) {
        query <- paste("DROP VIEW IF EXISTS", view_name, ";")
        res <- dbExecute(con, query)
    }
    res <- dbExecute(con, paste("DELETE FROM PreprocessedViews", where))
}

reset_top_ten <- function(con) {
    table_names <- dbGetQuery(con, paste("SELECT * FROM PreprocessedTables", get_where(and = "TableName like 'successful_counts_%'")))
    for (table_name in table_names[,1]) {
        dbExecute(con, paste(
            "update",
            table_name,
            "set topTen=0, ordering=0"))
    }
}