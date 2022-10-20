

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