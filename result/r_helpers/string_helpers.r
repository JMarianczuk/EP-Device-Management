source("r_helpers/array_helpers.r")

normalize <- function(text) {
    normalized <- gsub(":", "-", text)
    normalized
}

probability_num_value <- function(probability) {
    only_number <- substring(probability, first = 1, last = nchar(probability) - 2)
    numeric <- as.numeric(only_number)
    numeric
}

format_percent <- function(number) {
    formatted <- format(round(number * 100, 1), nsmall = 1)
    paste0(formatted, "%")
}

paste_if <- function(..., sep = " ", collapse = NULL, recycle0 = FALSE) {
    as_vector <- c(...)
    filtered <- vector_without(as_vector, "")
    paste0(filtered, collapse = sep)
}