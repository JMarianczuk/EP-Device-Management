library(stringr)

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

quoted_single <- function(text, quote_character = "\"") {
    if (!str_starts(text, quote_character)) {
        text <- paste0(quote_character, text)
    }
    if (!str_ends(text, quote_character)) {
        text <- paste0(text, quote_character)
    }
    text
}

quoted <- function(text, quote_character = "\"") {
    sapply(text, FUN = function(t) quoted_single(t, quote_character))
}

str_contains <- function(text, pattern) {
    location <- str_locate(text, pattern)
    res <- !is.na(location[1])
    res
}

pad_left <- function(text, character = " ", length = NULL, data = NULL) {
    text_length <- str_length(text)
    desired_length <- if (!is.null(length)) length else str_length(data)
    pad_length_or_zero <- sapply(desired_length - text_length, FUN = function(x) { max(x, 0) })
    paste0(
        str_dup(
            character,
            pad_length_or_zero),
        text)
}

pad_right <- function(text, character = " ", length = NULL, data = NULL) {
    text_length <- str_length(text)
    desired_length <- if (!is.null(length)) length else str_length(data)
    if (text_length >= desired_length) {
        text
    } else {
        paste0(text, str_dup(character, desired_length - text_length))
    }
}