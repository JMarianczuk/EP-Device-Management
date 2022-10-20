
normalize <- function(text) {
    normalized <- gsub(":", "-", text)
    normalized
}

probability_num_value <- function(probability) {
    only_number <- substring(probability, first = 1, last = nchar(probability) - 2)
    numeric <- as.numeric(only_number)
    numeric
}