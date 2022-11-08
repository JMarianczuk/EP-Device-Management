

first_greater_index <- function(arr, element) {
    result <- 0
    for (i in 1:length(arr)) {
        if (arr[i] > element) {
            result <- i
            break
        }
    }
    result
}

vector_without <- function(vector, element) {
    result <- c()
    for (value in vector) {
        if (value != element) {
            result <- append(result, value)
        }
    }
    result
}